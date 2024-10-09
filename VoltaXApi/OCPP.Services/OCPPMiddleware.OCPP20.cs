using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using VoltaXApi.Messages_OCPP20;
using VoltaXApi.OCPP.Core;
using VoltaXApi.Services;

namespace OCPP.Core.Server
{
    public partial class OCPPMiddleware
    {
        private async Task Receive20(ChargePointStatus chargePointStatus, HttpContext context)
        {
            var _msgProcessor = new OCPPMessageProcessor(this._logFactory, _configuration,chargePointStatus);
            ControllerOCPP20 controller20 = new ControllerOCPP20(_configuration, _logFactory, chargePointStatus);

            byte[] buffer = new byte[1024 * 4];
            MemoryStream memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result != null && result.MessageType != WebSocketMessageType.Close)
                {
                    Console.WriteLine("OCPPMiddleware.Receive20 => Receiving segment: {0} bytes (EndOfMessage={1} / MsgType={2})", result.Count, result.EndOfMessage, result.MessageType);
                    memStream.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        byte[] bMessage = memStream.ToArray();
                        memStream = new MemoryStream(buffer.Length);

                        DumpMessage(bMessage, "incoming");

                        string ocppMessage = Encoding.UTF8.GetString(bMessage);
                        Match match = Regex.Match(ocppMessage, MessageRegExp);
                        if (match != null && match.Groups != null && match.Groups.Count >= 3)
                        {
                            string messageTypeId = match.Groups[1].Value;
                            string uniqueId = match.Groups[2].Value;
                            string action = match.Groups[3].Value;
                            string jsonPaylod = match.Groups[4].Value;
                            Console.WriteLine("OCPPMiddleware.Receive20 => OCPP-Message: Type={0} / ID={1} / Action={2})", messageTypeId, uniqueId, action);

                            OCPPMessage msgIn = new OCPPMessage(messageTypeId, uniqueId, action, jsonPaylod);
                            await _msgProcessor.ProcessMessage(msgIn, chargePointStatus, context, _requestQueue, ocppMessage);
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
            ChargePointStatus dummy;
            _chargePointStatusDict.Remove(chargePointStatus.Id, out dummy);
        }

        private async Task Reset20(ChargePointStatus chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logFactory.CreateLogger("OCPPMiddleware.OCPP20");
            ControllerOCPP20 controller20 = new ControllerOCPP20(_configuration, _logFactory, chargePointStatus);

            ResetRequest resetRequest = new ResetRequest();
            resetRequest.Type = ResetEnumType.OnIdle;
            resetRequest.CustomData = new CustomDataType();
            resetRequest.CustomData.VendorId = ControllerOCPP20.VendorId;

            string jsonResetRequest = JsonConvert.SerializeObject(resetRequest);

            OCPPMessage msgOut = new OCPPMessage();
            msgOut.MessageType = "2";
            msgOut.Action = "Reset";
            msgOut.UniqueId = Guid.NewGuid().ToString("N");
            msgOut.JsonPayload = jsonResetRequest;
            msgOut.TaskCompletionSource = new TaskCompletionSource<string>();

            // store HttpContext with MsgId for later answer processing (=> send anwer to API caller)
            _requestQueue.Add(msgOut.UniqueId, msgOut);

            // Send OCPP message with optional logging/dump
            await SendOcpp20Message(msgOut, logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            string apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task UnlockConnector20(ChargePointStatus chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logFactory.CreateLogger("OCPPMiddleware.OCPP20");
            ControllerOCPP20 controller20 = new ControllerOCPP20(_configuration, _logFactory, chargePointStatus);

            UnlockConnectorRequest unlockConnectorRequest = new UnlockConnectorRequest();
            unlockConnectorRequest.EvseId = 0;
            unlockConnectorRequest.CustomData = new CustomDataType();
            unlockConnectorRequest.CustomData.VendorId = ControllerOCPP20.VendorId;

            string jsonResetRequest = JsonConvert.SerializeObject(unlockConnectorRequest);

            OCPPMessage msgOut = new OCPPMessage();
            msgOut.MessageType = "2";
            msgOut.Action = "UnlockConnector";
            msgOut.UniqueId = Guid.NewGuid().ToString("N");
            msgOut.JsonPayload = jsonResetRequest;
            msgOut.TaskCompletionSource = new TaskCompletionSource<string>();

            _requestQueue.Add(msgOut.UniqueId, msgOut);

            await SendOcpp20Message(msgOut, logger, chargePointStatus.WebSocket);

            string apiResult = await msgOut.TaskCompletionSource.Task;

            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task SendOcpp20Message(OCPPMessage msg, ILogger logger, WebSocket webSocket)
        {
            string? ocppTextMessage;

            if (string.IsNullOrEmpty(msg.ErrorCode))
            {
                if (msg.MessageType == "2")
                {
                    ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",{3}]", msg.MessageType, msg.UniqueId, msg.Action, msg.JsonPayload);
                }
                else
                {
                    ocppTextMessage = string.Format("[{0},\"{1}\",{2}]", msg.MessageType, msg.UniqueId, msg.JsonPayload);
                }
            }
            else
            {
                ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",\"{3}\",{4}]", msg.MessageType, msg.UniqueId, msg.ErrorCode, msg.ErrorDescription, "{}");
            }
            Console.WriteLine("OCPPMiddleware.OCPP20 => SendOcppMessage: {0}", ocppTextMessage);

            if (string.IsNullOrEmpty(ocppTextMessage))
            {
                // invalid message
                ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",\"{3}\",{4}]", "4", string.Empty, VoltaXApi.Messages_OCPP20.ErrorCodes.ProtocolError, string.Empty, "{}");
            }

            string dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                // Write outgoing message into dump directory
                string path = Path.Combine(dumpDir, string.Format("{0}_ocpp20-out.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")));
                try
                {
                    File.WriteAllText(path, ocppTextMessage);
                }
                catch (Exception exp)
                {
                    Console.WriteLine( "OCPPMiddleware.SendOcpp20Message=> Error dumping message to path: '{0}'", path);
                }
            }

            byte[] binaryMessage = UTF8Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void DumpMessage(byte[] message, string direction)
        {
            string dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_{direction}.txt";
                _fileWriter.WriteMessageToFile(dumpDir, fileName, message);
            }
        }
    }
}
