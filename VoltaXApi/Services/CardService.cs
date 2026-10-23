using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Services
{
    public class CardService : ICardService
    {
      private readonly ICardRepository _cardRepository;
      private readonly IConnectorRepository _connectorRepository;
      private readonly IMailService _mailService;
      private readonly ISystemReportService _systemReportService;
      private readonly IWalletService _walletService;

      public CardService(
        ICardRepository cardRepository,
        IConnectorRepository connectorRepository,
        IMailService mailService,
        ISystemReportService systemReportService,
        IWalletService walletService
      ) 
      {
        _cardRepository = cardRepository;
        _connectorRepository = connectorRepository;
        _mailService = mailService;
        _systemReportService = systemReportService;
        _walletService = walletService;
      }

      public async Task<AuthorizationStatusEnumType> ValidateCard(string idTag)
        {
            if (string.IsNullOrWhiteSpace(idTag))
            {
                return AuthorizationStatusEnumType.Accepted;
            }
            var card = await _cardRepository.GetCardByNumber(idTag);

            if (card == null)
                return AuthorizationStatusEnumType.Unknown;

            if (card.Blocked.HasValue && card.Blocked.Value)
                return AuthorizationStatusEnumType.Blocked;

            if (card.ExpirationDate < DateTime.UtcNow)
                return AuthorizationStatusEnumType.Expired;

            return AuthorizationStatusEnumType.Accepted;
        }

        public Task<bool> SubstractAmountFromCardByMinutes(int cardTagID, double minutesCharged, int connectorID, int chargingSessionID) =>
            Debit(cardTagID, connectorID, connector => CostCalculator.Instance.FinalCost(connector, minutesCharged),
                WalletTransactionType.SessionCharge, $"session:{chargingSessionID}:charge", chargingSessionID, "Charging session");

        public Task<bool> SubstractAmountFromCardByIdleMinutes(int cardTagID, double idleMinutes, int connectorID, int chargingSessionID) =>
            Debit(cardTagID, connectorID, connector => CostCalculator.Instance.IdleCost(connector, idleMinutes),
                WalletTransactionType.IdleFee, $"session:{chargingSessionID}:idle", chargingSessionID, "Idle fee");

        /// <summary>Debits the card with the connector-priced amount; false (and a system report) when the balance goes negative.</summary>
        private async Task<bool> Debit(int cardID, int connectorID, Func<Connector, decimal> price, WalletTransactionType type,
            string reference, int chargingSessionID, string description)
        {
            Connector connector = await _connectorRepository.GetByIdAsync(connectorID)
                ?? throw new NotFoundException($"There is no connector with the id {connectorID}");

            var amount = price(connector);
            if (amount <= 0)
                return true;
            var movement = await _walletService.PostAsync(cardID,
                new WalletPosting(-amount, type, reference, description, ChargingSessionID: chargingSessionID));

            if (movement.BalanceAfter < 0)
            {
                Card card = await _cardRepository.GetByIdAsync(cardID)
                    ?? throw new CardNotFoundException($"There is no card with the id {cardID}");
                await HandleCardNegativeBalance(card, connectorID);
                return false;
            }
            return true;
        }

        public async Task<bool> AddAmountToCard(int cardID, decimal amount, int orderID)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "A recharge amount must be positive");
            await _walletService.PostAsync(cardID, new WalletPosting(amount, WalletTransactionType.TopUp, $"order:{orderID}:topup",
                "Wallet top-up", OrderID: orderID));
            return true;
        }

        public async Task HandleCardNegativeBalance(Card card, int connectorID)
        {
            SystemReport report = new()
            {
                IsEmail = true,
                IsNotification = true,
                CardID = card.ID,
                ConnectorID = connectorID,
                IssueDescription = "Negative balance of " + card.Balance + " for the card : #" + card.ID,
                Status = ReportStatusEnum.Pending,
                Criticality = ReportCriticality.High
            };
            await _systemReportService.HandleReport(report);
        }


    }

    
}