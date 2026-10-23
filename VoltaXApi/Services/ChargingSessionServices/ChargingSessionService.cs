using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Factories;

namespace VoltaXApi.Services
{
    public class ChargingSessionService : IChargingSessionService
    {
        private readonly IChargingSessionRepository _chargingSessionRepository;
        private readonly ICardService _cardService;
        private readonly GlobalConfigurations _globalConfigurations;
        private readonly IMailService _mailService;
        private readonly IMailRequestFactory _mailRequestFactory;
        private readonly IUserRepository _userRepository;
        private readonly IWalletService _walletService;
        private readonly ICostCalculator _costCalculator;
        private readonly VoltaXApiDbContext _context;
        private readonly ILogger<ChargingSessionService> _logger;

        public ChargingSessionService(
          IChargingSessionRepository chargingSessionRepository,
          GlobalConfigurations globalConfigurations,
          ICardService cardService,
          IMailService mailService,
          IMailRequestFactory mailRequestFactory,
          IUserRepository userRepository,
          IWalletService walletService,
          ICostCalculator costCalculator,
          VoltaXApiDbContext context,
          ILogger<ChargingSessionService> logger
        )
        {
            _chargingSessionRepository = chargingSessionRepository;
            _cardService = cardService;
            _globalConfigurations = globalConfigurations;
            _mailService = mailService;
            _mailRequestFactory = mailRequestFactory;
            _userRepository = userRepository;
            _walletService = walletService;
            _costCalculator = costCalculator;
            _context = context;
            _logger = logger;
        }

        public async Task<ChargingSession> StartChargingSession(Connector connector, Card card, DateTime startDate)
        {
            return await _chargingSessionRepository.CreateChargingSessionForTransaction(connector, card, startDate);
        }

        public async Task<ChargingSession> UpdateChargingSession(int chargingSessionID, double minutesCharged, double chargedKwhs)
        {
            ChargingSession chargingSession = await _chargingSessionRepository.GetByIdAsync(chargingSessionID);
            chargingSession.ChargedMinutes = minutesCharged;
            chargingSession.ChargedKwhs = chargedKwhs;
            await _chargingSessionRepository.Update(chargingSession);
            return chargingSession;
        }

        public async Task<ChargingSession> EndChargingSession(int chargingSessionID, double minutesCharged, double chargedKwhs, DateTime endDate)
        {
            ChargingSession chargingSession = await _context.ChargingSessions
                .Include(s => s.Connector)
                .FirstAsync(s => s.ID == chargingSessionID);
            var connectorStatus = await _context.ConnectorStatuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ConnectorID == chargingSession.ConnectorID);

            var alreadyBilled = chargingSession.IsBilled;
            chargingSession.ChargedMinutes = minutesCharged;
            chargingSession.ChargedKwhs = chargedKwhs;
            chargingSession.EndDate = endDate;
            chargingSession.ChargingSessionStatus = ChargingSessionStatusEnum.CompletedOccupied;
            if (connectorStatus?.LastStatus == ConnectorStatusEnumType.Available)
            {
                // The car already left: no idle time.
                chargingSession.IdleMinutes = 0;
                chargingSession.EndIdleDate = endDate;
                chargingSession.ChargingSessionStatus = ChargingSessionStatusEnum.Completed;
            }

            if (alreadyBilled)
            {
                await _context.SaveChangesAsync();
                return chargingSession;
            }

            var tariff = Tariff.Of(chargingSession, chargingSession.Connector, _globalConfigurations.Vat);
            var charge = _costCalculator.Charge(tariff, chargedKwhs, minutesCharged);
            tariff.ApplyTo(chargingSession);
            chargingSession.EnergyAmount = charge.Energy;
            chargingSession.TimeAmount = charge.Time;
            chargingSession.FlatFeeAmount = charge.FlatFee;
            chargingSession.TotalAmount = charge.Total + (chargingSession.IdleAmount ?? 0);
            chargingSession.BilledAt = DateTime.UtcNow;

            await _walletService.SaveWithRetryAsync(async () =>
            {
                var card = await _context.Cards.FirstOrDefaultAsync(c => c.ID == chargingSession.CardID);
                chargingSession.BalanceBefore = card?.Balance;
                if (card != null && charge.Total > 0)
                    _walletService.Stage(card, new WalletPosting(-charge.Total, WalletTransactionType.SessionCharge,
                        $"session:{chargingSession.ID}:charge", "Charging session", ChargingSessionID: chargingSession.ID));
                chargingSession.BalanceAfter = card?.Balance;
            });

            if (chargingSession.BalanceAfter < 0)
                await ReportNegativeBalance(chargingSession);

            return chargingSession;
        }

        public async Task SendSessionQuoteAsync(int chargingSessionID)
        {
            try
            {
                int? userID = await _context.ChargingSessions.Where(s => s.ID == chargingSessionID).Select(s => s.UserID).FirstAsync();
                if (userID == null)
                    return; // a card that belongs to nobody (fleet / unassigned card)
                string userEmail = await _userRepository.GetUserEmailByID(userID.Value);
                if (string.IsNullOrWhiteSpace(userEmail))
                    return;
                MailRequest chargingSessionMailRequest = _mailRequestFactory.CreateChargingSessionQuoteMail(userEmail);
                ChargingSessionForMailDto chargingSessionForMail = await _chargingSessionRepository.GetChargingSessionForMail(chargingSessionID);
                await _mailService.SendChargingSessionQuoteMailRequest(chargingSessionMailRequest, chargingSessionForMail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The quote of charging session {ChargingSessionId} could not be emailed", chargingSessionID);
            }
        }

        public async Task<ChargingSession?> HandleIdleMinutes(int connectorID, DateTimeOffset? statusTime)
        {
            ChargingSession? chargingSession = await _context.ChargingSessions
                .Include(s => s.Connector)
                .Where(c => c.ConnectorID == connectorID && c.EndDate != null)
                .OrderByDescending(c => c.StartDate)
                .ThenByDescending(c => c.ID)
                .FirstOrDefaultAsync();
            if (chargingSession == null || chargingSession.IdleMinutes != null)
                return chargingSession;

            double idleMinutes = Math.Max(0, (statusTime - chargingSession.EndDate)?.TotalMinutes ?? 0);
            chargingSession.IdleMinutes = idleMinutes;
            chargingSession.EndIdleDate = statusTime?.UtcDateTime;
            chargingSession.ChargingSessionStatus = ChargingSessionStatusEnum.Completed;

            var tariff = Tariff.Of(chargingSession, chargingSession.Connector, _globalConfigurations.Vat);
            var fee = _costCalculator.IdleFee(tariff, idleMinutes, _globalConfigurations.GracePeriod);
            chargingSession.IdleAmount = fee;
            if (chargingSession.IsBilled)
                chargingSession.TotalAmount = chargingSession.ChargingPriceWithVAT + fee;

            await _walletService.SaveWithRetryAsync(async () =>
            {
                if (fee <= 0)
                    return;
                var card = await _context.Cards.FirstOrDefaultAsync(c => c.ID == chargingSession.CardID);
                if (card == null)
                    return;
                _walletService.Stage(card, new WalletPosting(-fee, WalletTransactionType.IdleFee,
                    $"session:{chargingSession.ID}:idle", "Idle fee", ChargingSessionID: chargingSession.ID));
                chargingSession.BalanceAfter = card.Balance;
            });

            if (chargingSession.BalanceAfter < 0)
                await ReportNegativeBalance(chargingSession);
            return chargingSession;
        }

        private async Task ReportNegativeBalance(ChargingSession chargingSession)
        {
            var card = await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.ID == chargingSession.CardID);
            if (card != null)
                await _cardService.HandleCardNegativeBalance(card, chargingSession.ConnectorID);
        }
    }
}
