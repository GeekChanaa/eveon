using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
    public class EVDriverService : IEVDriverService
    {
        private readonly OCPPMessageProcessor _messageProcessor;
        private readonly OCPPMessageFactory _messageFactory;

        public EVDriverService(OCPPMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
            _messageFactory = new OCPPMessageFactory();
        }

        // POST /ocpp/evdriver/requestStartTransaction
        public async Task RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request)
        {
            var msg = _messageFactory.CreateMessage("RequestStartTransaction", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/requestStopTransaction
        public async Task RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request)
        {
            var msg = _messageFactory.CreateMessage("RequestStopTransaction", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/cancelReservation
        public async Task CancelReservation(string chargePointID, CancelReservationRequest request)
        {
            var msg = _messageFactory.CreateMessage("CancelReservation", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/reserveNow
        public async Task ReserveNow(string chargePointID, ReserveNowRequest request)
        {
            var msg = _messageFactory.CreateMessage("ReserveNow", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/unlockConnector
        public async Task UnlockConnector(string chargePointID, UnlockConnectorRequest request)
        {
            var msg = _messageFactory.CreateMessage("UnlockConnector", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/clearCache
        public async Task ClearCache(string chargePointID, ClearCacheRequest request)
        {
            var msg = _messageFactory.CreateMessage("ClearCache", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/sendLocalList
        public async Task SendLocalList(string chargePointID, SendLocalListRequest request)
        {
            var msg = _messageFactory.CreateMessage("SendLocalList", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/getLocalListVersion
        public async Task GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request)
        {
            var msg = _messageFactory.CreateMessage("GetLocalListVersion", request);
            await _messageProcessor.SendMessage(msg, chargePointID);
        }
    }
}