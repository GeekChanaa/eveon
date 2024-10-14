using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.Data;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Services;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Services;

using System.Text;
using System.Text.RegularExpressions;
using VoltaXApi.OCPP.Messages;


namespace OCPP.Core.Server
{
    public partial class OCPPMiddleware
    {
        private static string MessageRegExp = "^\\[\\s*(\\d)\\s*,\\s*\"([^\"]*)\"\\s*,(?:\\s*\"(\\w*)\"\\s*,)?\\s*(.*)\\s*\\]$";

        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _logFactory;
        private readonly IConfiguration _configuration;
        private readonly FileWriter _fileWriter;

        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly RequestQueueManagerService _requestQueueManagerService;
        private readonly ChargePointStatusManagerService _chargePointStatusManagerService;
        private IWebSocketRequestsHandler _wsRequestsHandler;

        public OCPPMiddleware(RequestDelegate next,
            ILoggerFactory logFactory,
            IConfiguration configuration,
            WebSocketManagerService webSocketManagerService,
            ChargePointStatusManagerService chargePointStatusManagerService,
            RequestQueueManagerService requestQueueManagerService)
        {
            _next = next;
            _logFactory = logFactory;
            _configuration = configuration;
            _webSocketManagerService = webSocketManagerService;
            _fileWriter = new FileWriter();
            _chargePointStatusManagerService = chargePointStatusManagerService;
            _requestQueueManagerService = requestQueueManagerService;
        }

        public async Task Invoke(HttpContext context)
        {
            this._wsRequestsHandler = context.RequestServices.GetRequiredService<IWebSocketRequestsHandler>();
            if (context.WebSockets.IsWebSocketRequest)
                await this._wsRequestsHandler.Handle(context);
            else
                await _next(context);
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
            _requestQueueManagerService.AddMessage(msgOut.UniqueId, msgOut);

            // Send OCPP message with optional logging/dump
            await SendOcpp20Message(msgOut, chargePointStatus.WebSocket);

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

            await SendOcpp20Message(msgOut, chargePointStatus.WebSocket);

            string apiResult = await msgOut.TaskCompletionSource.Task;

            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task SendOcpp20Message(OCPPMessage msg, WebSocket webSocket)
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
                ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",\"{3}\",{4}]", "4", string.Empty, ErrorCodes.ProtocolError, string.Empty, "{}");
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
                    Console.WriteLine("OCPPMiddleware.SendOcpp20Message=> Error dumping message to path: '{0}'", path);
                }
            }

            byte[] binaryMessage = UTF8Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }

    public static class OCPPMiddlewareExtensions
    {
        public static IApplicationBuilder UseOCPPMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OCPPMiddleware>();
        }
    }


}
