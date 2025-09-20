using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OCPP.Core.Server;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Core
{
    public class OCPPMessageProcessor
    {
        private readonly IConfiguration _config;
        private readonly ControllerOCPP20 _controller20;
        private readonly RequestQueueManagerService _requestQueueManagerService;
        private readonly WebSocketManagerService _wsManagerService;
        private readonly IHubContext<ChargerHub> _hubContext;
        private readonly OCPPRequestHandler _reqHandler;
        private readonly ILogger<OCPPMessageProcessor> _logger;

        public OCPPMessageProcessor(
            IConfiguration config,
            RequestQueueManagerService requestQueueManagerService,
            WebSocketManagerService wsManagerService,
            IHubContext<ChargerHub> hubContext,
            OCPPRequestHandler requestHandler,
            ILogger<OCPPMessageProcessor> logger)
        {
            _config = config;
            _logger = logger;
            _requestQueueManagerService = requestQueueManagerService;
            _wsManagerService = wsManagerService;
            _hubContext = hubContext;
            _reqHandler = requestHandler;
        }

        public async Task ProcessMessage(
            OCPPMessage message,
            ChargePointStatus chargePointStatus,
            HttpContext context,
            string ocppMessage)
        {
            switch (message.MessageType)
            {
                case "2":
                    OCPPMessage msgOut = await _reqHandler.ProcessRequest(message, chargePointStatus);
                    await SendMessage(msgOut, chargePointStatus.Id);
                    break;

                case "3":
                case "4":
                    if (_requestQueueManagerService.ContainsKey(message.UniqueId))
                    {
                        await _controller20.ProcessAnswer(message, _requestQueueManagerService.GetMessage(message.UniqueId));
                        _requestQueueManagerService.RemoveMessage(message.UniqueId);
                    }
                    else
                    {
                        Console.WriteLine(" HttpContext from caller not found / Msg: {0}", ocppMessage);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown message type: {MessageType}", message.MessageType);
                    break;
            }
        }

        public async Task SendMessage(OCPPMessage message, string chargePointID)
        {
            object ocppArrayMessage;

            if (message.MessageType == "4")
            {
                ocppArrayMessage = new object[]
                {
                    JRaw.Parse(message.MessageType),
                    message.UniqueId,
                    string.IsNullOrWhiteSpace(message.JsonPayload) ? null : JRaw.Parse(message.JsonPayload)
                };
            }
            else
            {
                ocppArrayMessage = new object[]
                {
                    JRaw.Parse(message.MessageType),
                    message.UniqueId,
                    message.Action,
                    string.IsNullOrWhiteSpace(message.JsonPayload) ? null : JRaw.Parse(message.JsonPayload)
                };
            }




            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                NullValueHandling = NullValueHandling.Ignore
            };

            string serializedMessage = JsonConvert.SerializeObject(ocppArrayMessage, settings);

            _logger.LogInformation("Message Sent to the Charger : ");
            _logger.LogInformation(serializedMessage);
            await _hubContext.Clients.Group(chargePointID).SendAsync("ReceiveMessage", serializedMessage);

            byte[] binaryMessage = Encoding.UTF8.GetBytes(serializedMessage);

            WebSocket webSocket = this._wsManagerService.GetWebSocket(chargePointID);
            if (webSocket == null)
            {
                throw new WebSocketNotFoundException("WebSocket not found for the provided ChargePointID.");
            }

            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}