using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Data;
using VoltaXApi.Services;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Exceptions;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.OCPP.Ocpp16;

namespace VoltaXApi.OCPP.Services
{
    public class EVDriverService : IEVDriverService
    {
        private readonly IOcppCommandSender _commandSender;
        private readonly ICardRepository _cardRepository;
        private readonly ILogger<EVDriverService> _logger;
        private readonly GlobalConfigurations _globalConfigurations;
        private readonly IConnectorStatusService _connectorStatusService;
        private readonly VoltaXApiDbContext _context;
        private readonly Ocpp16CommandService _ocpp16;
        private readonly ReservationService _reservations;

        public EVDriverService(
            IOcppCommandSender commandSender,
            ICardRepository cardRepository,
            IConnectorStatusService connectorStatusService,
            ILogger<EVDriverService> logger,
            GlobalConfigurations globalConfigurations,
            VoltaXApiDbContext context,
            Ocpp16CommandService ocpp16,
            ReservationService reservations)
        {
            _context = context;
            _ocpp16 = ocpp16;
            _reservations = reservations;
            _commandSender = commandSender;
            _cardRepository = cardRepository;
            _logger = logger;
            _connectorStatusService = connectorStatusService;
            _globalConfigurations = globalConfigurations;
        }

        public async Task<RequestStartTransactionResponse> RequestStartTransactionMobile(string chargePointID, RequestStartTransactionRequest request, int userID, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RequestStartTransactionMobile for ChargePoint: {ChargePointID}", chargePointID);
            await new EmailVerificationGuard(_context).EnsureVerifiedAsync(userID);

            // The IdToken is always resolved server-side from the caller's own active card.
            var now = DateTime.UtcNow;
            var userCards = _context.Cards.AsNoTracking().Where(c => c.UserID == userID && !c.IsDeleted);
            var activeCards = userCards.Where(c => c.Status == CardStatusEnum.Active && c.Blocked != true && c.ExpirationDate > now);
            var requestedToken = request.IdToken?.IdToken;
            Card? card;
            if (!string.IsNullOrEmpty(requestedToken))
            {
                card = await activeCards.FirstOrDefaultAsync(c => c.CardNumber == requestedToken);
                if (card == null)
                {
                    _logger.LogWarning("RequestStartTransactionMobile denied: user {UserID} supplied an IdToken that is not one of their active cards", userID);
                    throw new ChargingOwnershipException("The requested card cannot be used by this account.");
                }
            }
            else
            {
                card = await activeCards.OrderBy(c => c.ID).FirstOrDefaultAsync();
                if (card == null)
                {
                    if (await userCards.AnyAsync())
                        throw new ChargingOwnershipException("Your recharge card is not active (blocked, expired or inactive).");
                    throw new CardNotFoundException("No recharge card was found for the authenticated user.");
                }
            }

            if (card.Balance < _globalConfigurations.DefaultMinimumRequiredBalance)
            {
                throw new PriceExceedsCardBalance(
                    card.Balance,
                    _globalConfigurations.DefaultMinimumRequiredBalance);
            }

            request.IdToken = new IdTokenType { IdToken = card.CardNumber, Type = IdTokenEnumType.Central };

            // check wether there's a connector in the chargepoint that is connected
            if(!(await _connectorStatusService.IsEVCableConnected(chargePointID)))
            {
                throw new NoEVConnectedException("No EV is currently connected to the charge point. Please connect your EV before requesting charge.");
            }

            var response = await SendStart(chargePointID, request, cancellationToken);
            _logger.LogInformation("Completed RequestStartTransactionMobile for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<RequestStopTransactionResponse> RequestStopTransactionMobile(string chargePointID, RequestStopTransactionRequest request, int userID, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RequestStopTransactionMobile for ChargePoint: {ChargePointID}", chargePointID);

            // Same answer for "does not exist" and "belongs to someone else" so other users' sessions are not revealed.
            var owned = await _context.Transactions.AsNoTracking().AnyAsync(t =>
                t.Uid == request.TransactionId && !t.IsDeleted &&
                t.Connector != null && t.Connector.ChargePoint.ChargePointId == chargePointID &&
                (t.ChargingSession!.UserID == userID || t.StartCard!.UserID == userID));
            if (!owned)
            {
                _logger.LogWarning("RequestStopTransactionMobile denied: transaction not found for user {UserID} on {ChargePointID}", userID, chargePointID);
                throw new TransactionNotFoundException("Transaction not found.");
            }

            var response = await SendStop(chargePointID, request, cancellationToken);
            _logger.LogInformation("Completed RequestStopTransactionMobile for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<RequestStartTransactionResponse> RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RequestStartTransaction for ChargePoint: {ChargePointID}", chargePointID);
            var response = await SendStart(chargePointID, request, cancellationToken);
            _logger.LogInformation("Completed RequestStartTransaction for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<RequestStopTransactionResponse> RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RequestStopTransaction for ChargePoint: {ChargePointID}", chargePointID);
            var response = await SendStop(chargePointID, request, cancellationToken);
            _logger.LogInformation("Completed RequestStopTransaction for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<CancelReservationResponse> CancelReservation(string chargePointID, CancelReservationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting CancelReservation for ChargePoint: {ChargePointID}", chargePointID);
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.CancelReservation(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<CancelReservationRequest, CancelReservationResponse>(chargePointID, "CancelReservation", request, cancellationToken: cancellationToken);
            if (response.Status == CancelReservationStatusEnumType.Accepted)
                await _reservations.UpdateActiveAsync(chargePointID, request.ReservationId, ReservationStatusEnum.Cancelled, cancellationToken: cancellationToken);
            _logger.LogInformation("Completed CancelReservation for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<ReserveNowResponse> ReserveNow(string chargePointID, ReserveNowRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting ReserveNow for ChargePoint: {ChargePointID}", chargePointID);
            Reservation reservation;
            try
            {
                reservation = await _reservations.CreateAsync(chargePointID, request.Id, request.EvseId, request.IdToken?.IdToken ?? "",
                    request.ExpiryDateTime.Kind == DateTimeKind.Local ? request.ExpiryDateTime.ToUniversalTime() : request.ExpiryDateTime, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "ReserveNow for {ChargePointID} not sent", chargePointID);
                return new ReserveNowResponse { Status = ReserveNowStatusEnumType.Rejected, StatusInfo = new StatusInfoType { ReasonCode = "InvalidReservation", AdditionalInfo = ex.Message } };
            }
            request.Id = reservation.ReservationId;

            ReserveNowResponse response;
            try
            {
                response = _ocpp16.IsOcpp16(chargePointID)
                    ? await _ocpp16.ReserveNow(chargePointID, request, cancellationToken)
                    : await _commandSender.SendRequestAsync<ReserveNowRequest, ReserveNowResponse>(chargePointID, "ReserveNow", request, cancellationToken: cancellationToken);
            }
            catch
            {
                // Not sent or not answered: the charger holds no reservation we know of.
                await _reservations.SetStatusAsync(reservation.ID, ReservationStatusEnum.Rejected, CancellationToken.None);
                throw;
            }
            if (response.Status != ReserveNowStatusEnumType.Accepted)
                await _reservations.SetStatusAsync(reservation.ID, ReservationStatusEnum.Rejected, cancellationToken);
            _logger.LogInformation("Completed ReserveNow for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<UnlockConnectorResponse> UnlockConnector(string chargePointID, UnlockConnectorRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting UnlockConnector for ChargePoint: {ChargePointID}", chargePointID);
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.UnlockConnector(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<UnlockConnectorRequest, UnlockConnectorResponse>(chargePointID, "UnlockConnector", request, cancellationToken: cancellationToken);
            _logger.LogInformation("Completed UnlockConnector for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<ClearCacheResponse> ClearCache(string chargePointID, ClearCacheRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting ClearCache for ChargePoint: {ChargePointID}", chargePointID);
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.ClearCache(chargePointID, cancellationToken)
                : await _commandSender.SendRequestAsync<ClearCacheRequest, ClearCacheResponse>(chargePointID, "ClearCache", request, cancellationToken: cancellationToken);
            _logger.LogInformation("Completed ClearCache for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<SendLocalListResponse> SendLocalList(string chargePointID, SendLocalListRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting SendLocalList for ChargePoint: {ChargePointID}", chargePointID);
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.SendLocalList(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<SendLocalListRequest, SendLocalListResponse>(chargePointID, "SendLocalList", request, cancellationToken: cancellationToken);
            _logger.LogInformation("Completed SendLocalList for ChargePoint: {ChargePointID}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<GetLocalListVersionResponse> GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting GetLocalListVersion for ChargePoint: {ChargePointID}", chargePointID);
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.GetLocalListVersion(chargePointID, cancellationToken)
                : await _commandSender.SendRequestAsync<GetLocalListVersionRequest, GetLocalListVersionResponse>(chargePointID, "GetLocalListVersion", request, cancellationToken: cancellationToken);
            _logger.LogInformation("Completed GetLocalListVersion for ChargePoint: {ChargePointID}: version {VersionNumber}", chargePointID, response.VersionNumber);
            return response;
        }

        private Task<RequestStartTransactionResponse> SendStart(string chargePointID, RequestStartTransactionRequest request, CancellationToken cancellationToken) =>
            _ocpp16.IsOcpp16(chargePointID)
                ? _ocpp16.RemoteStartTransaction(chargePointID, request, cancellationToken)
                : _commandSender.SendRequestAsync<RequestStartTransactionRequest, RequestStartTransactionResponse>(chargePointID, "RequestStartTransaction", request, cancellationToken: cancellationToken);

        private Task<RequestStopTransactionResponse> SendStop(string chargePointID, RequestStopTransactionRequest request, CancellationToken cancellationToken) =>
            _ocpp16.IsOcpp16(chargePointID)
                ? _ocpp16.RemoteStopTransaction(chargePointID, request, cancellationToken)
                : _commandSender.SendRequestAsync<RequestStopTransactionRequest, RequestStopTransactionResponse>(chargePointID, "RequestStopTransaction", request, cancellationToken: cancellationToken);
    }
}
