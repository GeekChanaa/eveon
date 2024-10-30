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
    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ControllerOCPP20 _controller20;
    private readonly RequestQueueManagerService _requestQueueManagerService;
    private readonly WebSocketManagerService _wsManagerService;
    private readonly IHubContext<ChargerHub> _hubContext;
    private readonly IOCPPRequestHandler _reqHandler;

    public OCPPMessageProcessor(
        ILoggerFactory loggerFactory, 
        IConfiguration config,
        RequestQueueManagerService requestQueueManagerService,
        WebSocketManagerService wsManagerService,
        IHubContext<ChargerHub> hubContext)
    {
        _config = config;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger(typeof(OCPPMessageProcessor));
        _requestQueueManagerService = requestQueueManagerService;
        _wsManagerService = wsManagerService;
        _hubContext = hubContext;
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
                OCPPMessage msgOut = _reqHandler.ProcessRequest(message);
                await SendMessage(msgOut, chargePointStatus.Id);
                break;

            case "3":
            case "4":
                    if (_requestQueueManagerService.ContainsKey(message.UniqueId))
                    {
                        _controller20.ProcessAnswer(message, _requestQueueManagerService.GetMessage(message.UniqueId));
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
        var ocppArrayMessage = new object[]
        {
            JRaw.Parse(message.MessageType),
            message.UniqueId,   
            message.Action,     
            JRaw.Parse(message.JsonPayload)   
        };

        if(message.MessageType == "3")
        {
            ocppArrayMessage = new object[]
            {
                JRaw.Parse(message.MessageType),
                message.UniqueId,   
                JRaw.Parse(message.JsonPayload)   
            };
        }

        Console.WriteLine("SENDING MESSAGE :  ");
        Console.WriteLine("message.UniqueId : " + message.UniqueId);
        Console.WriteLine("message.Action : " + message.Action);

        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        string serializedMessage = JsonConvert.SerializeObject(ocppArrayMessage,settings);
        Console.WriteLine("SENDING A MESSAGE THROUGH SIGNALR");
        Console.WriteLine(chargePointID);
        await _hubContext.Clients.Group(chargePointID).SendAsync("ReceiveMessage", serializedMessage);

        

        byte[] binaryMessage = Encoding.UTF8.GetBytes(serializedMessage);

        WebSocket webSocket = this._wsManagerService.GetWebSocket(chargePointID);
        if(webSocket == null){
            throw new WebSocketNotFoundException("WebSocket not found for the provided ChargePointID."); 
        }

        await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage), WebSocketMessageType.Text, true, CancellationToken.None);
    }
  }
}