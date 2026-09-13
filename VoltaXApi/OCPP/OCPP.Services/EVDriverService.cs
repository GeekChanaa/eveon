using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Data;
using VoltaXApi.Services;
using VoltaXApi.Exceptions;

namespace VoltaXApi.OCPP.Services
{
    public class EVDriverService : IEVDriverService
    {
        private readonly OCPPMessageProcessor _messageProcessor;
        private readonly OCPPMessageFactory _messageFactory;
        private readonly ICardRepository _cardRepository;
        private readonly ILogger<EVDriverService> _logger;
        private readonly IConnectorStatusService _connectorStatusService;

        public EVDriverService(
            OCPPMessageProcessor messageProcessor, 
            ICardRepository cardRepository, 
            IConnectorStatusService connectorStatusService,
            ILogger<EVDriverService> logger)
        {
            _messageProcessor = messageProcessor;
            _cardRepository = cardRepository;
            _messageFactory = new OCPPMessageFactory();
            _logger = logger;
            _connectorStatusService = connectorStatusService;
        }

        public async Task RequestStartTransactionMobile(string chargePointID, RequestStartTransactionRequest request, int userID)
        {
            _logger.LogInformation("Starting RequestStartTransactionMobile for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("RequestStartTransactionMobile", request);
            if (request.IdToken == null)
            {
                var cardTokenInfo = await _cardRepository.GetCardTokenInfoByUserID(userID);
                request.IdToken = new IdTokenType { IdToken = cardTokenInfo.CardNumber, Type = IdTokenEnumType.Central };
            }
            // check wether there's a connector in the chargepoint that is connected
            if(!(await _connectorStatusService.IsEVCableConnected(chargePointID)))
            {
                throw new NoEVConnectedException("No EV is currently connected to the charge point. Please connect your EV before requesting charge.");
            }
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed RequestStartTransactionMobile for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task RequestStopTransactionMobile(string chargePointID, RequestStopTransactionRequest request)
        {
            _logger.LogInformation("Starting RequestStopTransactionMobile for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("RequestStopTransaction", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed RequestStopTransactionMobile for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request)
        {
            _logger.LogInformation("Starting RequestStartTransaction for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("RequestStartTransaction", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed RequestStartTransaction for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request)
        {
            _logger.LogInformation("Starting RequestStopTransaction for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("RequestStopTransaction", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed RequestStopTransaction for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task CancelReservation(string chargePointID, CancelReservationRequest request)
        {
            _logger.LogInformation("Starting CancelReservation for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("CancelReservation", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed CancelReservation for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task ReserveNow(string chargePointID, ReserveNowRequest request)
        {
            _logger.LogInformation("Starting ReserveNow for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("ReserveNow", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed ReserveNow for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task UnlockConnector(string chargePointID, UnlockConnectorRequest request)
        {
            _logger.LogInformation("Starting UnlockConnector for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("UnlockConnector", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed UnlockConnector for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task ClearCache(string chargePointID, ClearCacheRequest request)
        {
            _logger.LogInformation("Starting ClearCache for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("ClearCache", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed ClearCache for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task SendLocalList(string chargePointID, SendLocalListRequest request)
        {
            _logger.LogInformation("Starting SendLocalList for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("SendLocalList", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed SendLocalList for ChargePoint: {ChargePointID}", chargePointID);
        }

        public async Task GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request)
        {
            _logger.LogInformation("Starting GetLocalListVersion for ChargePoint: {ChargePointID}", chargePointID);
            var msg = _messageFactory.CreateMessage("GetLocalListVersion", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
            _logger.LogInformation("Completed GetLocalListVersion for ChargePoint: {ChargePointID}", chargePointID);
        }
    }
}
