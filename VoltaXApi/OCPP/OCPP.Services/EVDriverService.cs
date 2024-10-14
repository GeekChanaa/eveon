

using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{


    public class EVDriverService : IEVDriverService
    {
        private readonly OCPPMessageProcessor _messageProcessor;

        public EVDriverService(OCPPMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
        }

        // POST /ocpp/evdriver/requestStartTransaction
        public async Task RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "RequestStartTransaction",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/requestStopTransaction
        public async Task RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "RequestStopTransaction",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/cancelReservation
        public async Task CancelReservation(string chargePointID, CancelReservationRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "CancelReservation",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/reserveNow
        public async Task ReserveNow(string chargePointID, ReserveNowRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "ReserveNow",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/unlockConnector
        public async Task UnlockConnector(string chargePointID, UnlockConnectorRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "UnlockConnector",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/clearCache
        public async Task ClearCache(string chargePointID, ClearCacheRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "ClearCache",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/sendLocalList
        public async Task SendLocalList(string chargePointID, SendLocalListRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "SendLocalList",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }

        // POST /ocpp/evdriver/getLocalListVersion
        public async Task GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request)
        {
            OCPPMessage msg = new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = "GetLocalListVersion",
                JsonPayload = JsonConvert.SerializeObject(request)
            };
            await _messageProcessor.SendMessage(msg, chargePointID);
        }
    }


    //GET   /data/evdriver/systemConfig
    //PUT   /data/evdriver/systemConfig
}