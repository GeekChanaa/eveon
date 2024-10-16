

using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class WebSocketHandler
    {
        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly IConfiguration _config;
        private readonly ILoggerFactory _logFactory;
        private static string MessageRegExp = "^\\[\\s*(\\d)\\s*,\\s*\"([^\"]*)\"\\s*,(?:\\s*\"(\\w*)\"\\s*,)?\\s*(.*)\\s*\\]$";
        private readonly FileWriter _fileWriter;
        private readonly OCPPMessageProcessor _msgProcessor;
        private readonly IHubContext<ChargerHub> _hubContext;
        public WebSocketHandler(
          WebSocketManagerService webSocketManagerService,
          ILoggerFactory logFactory,
          IConfiguration config,
          OCPPMessageProcessor msgProcessor,
          IHubContext<ChargerHub> hubContext)
        {
            _webSocketManagerService = webSocketManagerService;
            _config = config;
            _logFactory = logFactory;
            _fileWriter = new FileWriter();
            _msgProcessor = msgProcessor;
            _hubContext = hubContext;
        }

        public async Task AcceptWebSocketAsync(HttpContext context, string subProtocol, ChargePointStatus chargePointStatus)
        {
            using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol))
            {
                Console.WriteLine($"WebSocket connection with charge point '{chargePointStatus.Id}'");
                chargePointStatus.WebSocket = webSocket;
                _webSocketManagerService.AddWebSocket(chargePointStatus.Id, webSocket);
                await ReceiveOcppMessageAsync(chargePointStatus, context);
            }
        }

        private async Task ReceiveOcppMessageAsync(ChargePointStatus chargePointStatus, HttpContext context)
        {
            ControllerOCPP20 controller20 = new ControllerOCPP20(_config, _logFactory, chargePointStatus);

            byte[] buffer = new byte[1024 * 4];
            MemoryStream memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result != null && result.MessageType != WebSocketMessageType.Close)
                {
                    Console.WriteLine("Receiving segment: {0} bytes (EndOfMessage={1} / MsgType={2})", result.Count, result.EndOfMessage, result.MessageType);
                    memStream.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        byte[] bMessage = memStream.ToArray();
                        memStream = new MemoryStream(buffer.Length);

                        DumpMessage(bMessage, "incoming");

                        string ocppMessage = Encoding.UTF8.GetString(bMessage);
                        Console.WriteLine("Sending the Message for the chargepoint : " + chargePointStatus.Id);
                        await _hubContext.Clients.Group(chargePointStatus.Id).SendAsync("SentMessage", JsonConvert.SerializeObject(ocppMessage));

                        Match match = Regex.Match(ocppMessage, MessageRegExp);
                        if (match != null && match.Groups != null && match.Groups.Count >= 3)
                        {
                            string messageTypeId = match.Groups[1].Value;
                            string uniqueId = match.Groups[2].Value;
                            string action = match.Groups[3].Value;
                            string jsonPaylod = match.Groups[4].Value;
                            Console.WriteLine("OCPPMiddleware.Receive20 => OCPP-Message: Type={0} / ID={1} / Action={2})", messageTypeId, uniqueId, action);

                            OCPPMessage msgIn = new OCPPMessage(messageTypeId, uniqueId, action, jsonPaylod);
                            await _msgProcessor.ProcessMessage(msgIn, chargePointStatus, context, ocppMessage);
                        }
                        else
                        {
                            Console.WriteLine("OCPPMiddleware.Receive20 => Error in RegEx-Matching: Msg={0})", ocppMessage);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("OCPPMiddleware.Receive20 => Receive: unexpected result: CloseStatus={0} / MessageType={1}", result?.CloseStatus, result?.MessageType);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            Console.WriteLine("OCPPMiddleware.Receive20 => Websocket closed: State={0} / CloseStatus={1}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            _webSocketManagerService.RemoveWebSocket(chargePointStatus.Id);
        }

        private void DumpMessage(byte[] message, string direction)
        {
            string dumpDir = _config.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_{direction}.txt";
                _fileWriter.WriteMessageToFile(dumpDir, fileName, message);
            }
        }
    }

}