

using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
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
        private readonly IChargePointUptimeRepository _chargePointUTRepository;
        public WebSocketHandler(
          WebSocketManagerService webSocketManagerService,
          ILoggerFactory logFactory,
          IConfiguration config,
          OCPPMessageProcessor msgProcessor,
          IHubContext<ChargerHub> hubContext,
          IChargePointUptimeRepository chargePointUptimeRepository)
        {
            _webSocketManagerService = webSocketManagerService;
            _config = config;
            _logFactory = logFactory;
            _fileWriter = new FileWriter();
            _msgProcessor = msgProcessor;
            _hubContext = hubContext;
            _chargePointUTRepository = chargePointUptimeRepository;
        }

        public async Task AcceptWebSocketAsync(HttpContext context, string subProtocol, ChargePointStatus chargePointStatus)
        {
            using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol))
            {
                Console.WriteLine($"WebSocket connection with charge point '{chargePointStatus.Id}'");
                await this._chargePointUTRepository.StartOnline(chargePointStatus.Id);
                chargePointStatus.WebSocket = webSocket;
                _webSocketManagerService.AddWebSocket(chargePointStatus.Id, webSocket);
                await ReceiveOcppMessageAsync(chargePointStatus, context);
            }
        }

        private async Task ReceiveOcppMessageAsync(ChargePointStatus chargePointStatus, HttpContext context)
        {
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
                        
                        await _hubContext.Clients.Group(chargePointStatus.Id).SendAsync("SentMessage", JsonConvert.SerializeObject(ocppMessage));

                        var msgIn = await ValidatingOCPPMessage(chargePointStatus, ocppMessage, context);

                        await _msgProcessor.ProcessMessage(msgIn, chargePointStatus, context, ocppMessage);

                    }
                }
                else
                {
                    Console.WriteLine("OCPPMiddleware.Receive20 => Receive: unexpected result: CloseStatus={0} / MessageType={1}", result?.CloseStatus, result?.MessageType);
                    await _chargePointUTRepository.StopNormal(chargePointStatus.Id);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            Console.WriteLine("OCPPMiddleware.Receive20 => Websocket closed: State={0} / CloseStatus={1}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            await _chargePointUTRepository.StopNormal(chargePointStatus.Id);
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

        private async  Task<OCPPMessage>? ValidatingOCPPMessage(ChargePointStatus chargePointStatus,string ocppMessage, HttpContext context)
        {
            Match match = Regex.Match(ocppMessage, MessageRegExp);
            if (match != null && match.Groups != null && match.Groups.Count >= 3)
            {
                string messageTypeId = match.Groups[1].Value;
                string uniqueId = match.Groups[2].Value;
                string action = match.Groups[3].Value;
                string jsonPayload = match.Groups[4].Value;
                var msg =  new OCPPMessage(messageTypeId, uniqueId, action, jsonPayload, ocppMessage);
                return msg;
            }
            else
            {
                Console.WriteLine("OCPPMiddleware.Receive20 => Error in RegEx-Matching: Msg={0})", ocppMessage);
                return null;
            }
        }
    }

}