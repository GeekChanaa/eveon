using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using OCPP.Core.Server;

namespace VoltaXApi.OCPP.Core
{
  public class OCPPMessageProcessor : IMessageProcessor
  {
    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ControllerOCPP20 _controller20;

    public OCPPMessageProcessor(
        ILoggerFactory loggerFactory, 
        IConfiguration config, 
        ChargePointStatus chargePointStatus)
    {
        _config = config;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger(typeof(OCPPMessageProcessor));
        this._controller20 = new ControllerOCPP20(this._config,this._loggerFactory, chargePointStatus);
    }

    public async Task ProcessMessage(
        OCPPMessage message, 
        ChargePointStatus chargePointStatus, 
        HttpContext context,
        Dictionary<string, OCPPMessage> requestQueue,
        string ocppMessage)
    {
        switch (message.MessageType)
        {
            case "2":
                OCPPMessage msgOut = _controller20.ProcessRequest(message);
                await SendMessage(msgOut, chargePointStatus.WebSocket);
                break;

            case "3":
            case "4":
                    if (requestQueue.ContainsKey(message.UniqueId))
                    {
                        _controller20.ProcessAnswer(message, requestQueue[message.UniqueId]);
                        requestQueue.Remove(message.UniqueId);
                    }
                    else
                    {
                        Console.WriteLine("OCPPMiddleware.Receive20 => HttpContext from caller not found / Msg: {0}", ocppMessage);
                    }
                break;

            default:
                _logger.LogWarning("Unknown message type: {MessageType}", message.MessageType);
                break;
        }
    }

    private async Task SendMessage(OCPPMessage message, WebSocket webSocket)
{
    // Deserialize JsonPayload into an object if it's a stringified JSON
    var jsonPayloadObject = JsonConvert.DeserializeObject(message.JsonPayload);

    // Construct the OCPP message array
    var ocppArrayMessage = new object[]
    {
        message.MessageType,  // MessageType (e.g., 2, 3, or 4)
        message.UniqueId,     // Unique ID (string)
        message.Action,       // Action (for MessageType 2 only, otherwise null)
        jsonPayloadObject     // Payload (actual payload or error details)
    };

    // Serialize the array to JSON
    string serializedMessage = JsonConvert.SerializeObject(ocppArrayMessage);

    // Convert the serialized message to bytes and send over WebSocket
    byte[] binaryMessage = Encoding.UTF8.GetBytes(serializedMessage);
    Console.WriteLine("this is the binary message");
    Console.WriteLine(serializedMessage);

    await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage), WebSocketMessageType.Text, true, CancellationToken.None);
}
  }
}