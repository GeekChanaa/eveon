using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.Data;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Services;
using VoltaXApi.OCPP.Core;

namespace OCPP.Core.Server
{
    public partial class OCPPMiddleware
    {
        // Supported OCPP protocols (in order)
        private const string Protocol_OCPP16 = "ocpp1.6";
        private const string Protocol_OCPP20 = "ocpp2.0.1";
        private static readonly string[] SupportedProtocols = { Protocol_OCPP20, Protocol_OCPP16 /*, "ocpp1.5" */};
        private static string MessageRegExp = "^\\[\\s*(\\d)\\s*,\\s*\"([^\"]*)\"\\s*,(?:\\s*\"(\\w*)\"\\s*,)?\\s*(.*)\\s*\\]$";

        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _logFactory;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly FileWriter _fileWriter;
        private readonly AuthenticationService _authService;

        // Dictionary with status objects for each charge point
        private static Dictionary<string, ChargePointStatus> _chargePointStatusDict = new Dictionary<string, ChargePointStatus>();

        // Dictionary for processing asynchronous API calls
        private Dictionary<string, OCPPMessage> _requestQueue = new Dictionary<string, OCPPMessage>();
        private readonly WebSocketManagerService _webSocketManagerService;
        private IChargePointRepository _chargePointRepo;

        public OCPPMiddleware(RequestDelegate next, 
            ILoggerFactory logFactory, 
            IConfiguration configuration,
            WebSocketManagerService webSocketManagerService)
        {
            _next = next;
            _logFactory = logFactory;
            _configuration = configuration;
            _webSocketManagerService = webSocketManagerService;
            _logger = logFactory.CreateLogger("OCPPMiddleware");
            _authService = new AuthenticationService();
            _fileWriter = new FileWriter();
        }

        public async Task Invoke(HttpContext context)
        {
            this._chargePointRepo = context.RequestServices.GetRequiredService<IChargePointRepository>();
            if (context.WebSockets.IsWebSocketRequest)
                await HandleWebSocketRequests(context);
            else
                await _next(context);
        }

        public async Task HandleWebSocketRequests(HttpContext context)
        {
            Console.WriteLine("OCPPMiddleware => Websocket request: Path='{0}'", context.Request.Path);
            
            ChargePointStatus? chargePointStatus = null;
            if (context.Request.Path.StartsWithSegments("/OCPP"))
            {
                string chargepointIdentifier;
                string[] parts = context.Request.Path.Value.Split('/');
                chargepointIdentifier = parts[parts.Length - 1];


                Console.WriteLine("OCPPMiddleware => Connection request with chargepoint identifier = '{0}'", chargepointIdentifier);

                // Known chargepoint?
                if (!string.IsNullOrWhiteSpace(chargepointIdentifier))
                {
                    ChargePoint? chargePoint = await _chargePointRepo.GetChargePointByChargePointIDAsync(chargepointIdentifier);
                    if (chargePoint != null)
                    {
                        Console.WriteLine("OCPPMiddleware => SUCCESS: Found chargepoint with identifier={0}", chargePoint.ChargePointId);

                        _authService.AuthenticateChargePoint(context, chargePoint);
                        chargePointStatus = new ChargePointStatus(chargePoint);
                    }
                    else
                    {
                        Console.WriteLine("OCPPMiddleware => FAILURE: Found no chargepoint with identifier={0}", chargepointIdentifier);
                    }
                }

                if (chargePointStatus != null)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        // Match supported sub protocols
                        string subProtocol = null;
                        foreach (string supportedProtocol in SupportedProtocols)
                        {
                            if (context.WebSockets.WebSocketRequestedProtocols.Contains(supportedProtocol))
                            {
                                Console.WriteLine(supportedProtocol);
                                subProtocol = supportedProtocol;
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(subProtocol))
                        {

                            // Not matching protocol! => failure
                            string protocols = string.Empty;
                            foreach (string p in context.WebSockets.WebSocketRequestedProtocols)
                            {
                                if (string.IsNullOrEmpty(protocols)) protocols += ",";
                                protocols += p;
                            }
                            Console.WriteLine("OCPPMiddleware => No supported sub-protocol in '{0}' from charge station '{1}'", protocols, chargepointIdentifier);
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                        else
                        {
                            chargePointStatus.Protocol = subProtocol;

                            bool statusSuccess = false;
                            try
                            {
                                Console.WriteLine("OCPPMiddleware => Store/Update status object");

                                lock (_chargePointStatusDict)
                                {
                                    if (_chargePointStatusDict.ContainsKey(chargepointIdentifier.ToString()))
                                    {
                                        if (_chargePointStatusDict[chargepointIdentifier.ToString()].WebSocket.State != WebSocketState.Open)
                                        {
                                            _chargePointStatusDict.Remove(chargepointIdentifier.ToString());
                                        }
                                    }

                                    _chargePointStatusDict.Add(chargepointIdentifier.ToString(), chargePointStatus);
                                    statusSuccess = true;
                                }
                            }
                            catch (Exception exp)
                            {
                                Console.WriteLine( "OCPPMiddleware => Error storing status object in dictionary => refuse connection");
                                Console.WriteLine("Exception : ");
                                Console.WriteLine(exp.Message);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }

                            if (statusSuccess)
                            {
                                Console.WriteLine("OCPPMiddleware => Waiting for message...");
                                
                                using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol))
                                {
                                    Console.WriteLine("OCPPMiddleware => WebSocket connection with charge point '{0}'", chargepointIdentifier);
                                    chargePointStatus.WebSocket = webSocket;
                                    _webSocketManagerService.AddWebSocket(chargepointIdentifier.ToString(), webSocket);

                                    if (subProtocol == Protocol_OCPP20)
                                    {
                                        await Receive20(chargePointStatus, context);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("OCPPMiddleware => Non-Websocket request");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    Console.WriteLine("OCPPMiddleware => no chargepoint: http 412");
                    context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
                }
            }
            else if (context.Request.Path.StartsWithSegments("/API"))
            {
                Console.WriteLine("this is a ws api request");
                await HandleWebsocketApiRequests(context);
            }
            else
            {
                Console.WriteLine("OCPPMiddleware => Bad path request");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
        public async Task<bool> SendMessageToChargePointAsync(string chargePointId, string message)
        {
            if (_chargePointStatusDict.TryGetValue(chargePointId, out ChargePointStatus chargePointStatus))
            {
                var webSocket = chargePointStatus.WebSocket;
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
                        var buffer = new ArraySegment<byte>(messageBytes);

                        await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                        Console.WriteLine($"Message sent to charge point {chargePointId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to charge point {chargePointId}: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"WebSocket for charge point {chargePointId} is not open.");
                }
            }
            else
            {
                Console.WriteLine($"Charge point {chargePointId} not found.");
            }

            return false;
        }

        private async Task HandleWebsocketApiRequests(HttpContext context)
        {
            string apiKeyConfig = _configuration.GetValue<string>("ApiKey");
            if (!string.IsNullOrWhiteSpace(apiKeyConfig))
            {
                string apiKeyCaller = context.Request.Headers["X-API-Key"].FirstOrDefault();
                if (apiKeyConfig == apiKeyCaller)
                {
                    Console.WriteLine("OCPPMiddleware => Success: X-API-Key matches");
                }
                else
                {
                    Console.WriteLine("OCPPMiddleware => Failure: Wrong X-API-Key! Caller='{0}'", apiKeyCaller);
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }
            }
            else
            {
                Console.WriteLine("OCPPMiddleware => No X-API-Key configured!");
            }

            string[] urlParts = context.Request.Path.Value.Split('/');

            if (urlParts.Length >= 3)
            {
                string cmd = urlParts[2];
                string urlChargePointId = (urlParts.Length >= 4) ? urlParts[3] : null;
                Console.WriteLine("OCPPMiddleware => cmd='{0}' / id='{1}' / FullPath='{2}')", cmd, urlChargePointId, context.Request.Path.Value);

                if (cmd == "Status")
                {
                    try
                    {
                        Console.WriteLine("this is in here");
                        List<ChargePointStatus> statusList = new List<ChargePointStatus>();
                        foreach (ChargePointStatus status in _chargePointStatusDict.Values)
                        {
                            statusList.Add(status);
                        }
                        string jsonStatus = JsonConvert.SerializeObject(statusList);
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(jsonStatus);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine( "OCPPMiddleware => Error: {0}", exp.Message);
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
                else if (cmd == "Reset")
                {
                    if (!string.IsNullOrEmpty(urlChargePointId))
                    {
                        try
                        {
                            ChargePointStatus status = null;
                            if (_chargePointStatusDict.TryGetValue(urlChargePointId, out status))
                            {
                                if (status.Protocol == Protocol_OCPP20)
                                {
                                    await Reset20(status, context);
                                }
                            }
                            else
                            {
                                // Chargepoint offline
                                Console.WriteLine("OCPPMiddleware SoftReset => Chargepoint offline: {0}", urlChargePointId);
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            }
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("OCPPMiddleware SoftReset => Error: {0}", exp.Message);
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                    }
                    else
                    {
                        Console.WriteLine("OCPPMiddleware SoftReset => Missing chargepoint ID");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else if (cmd == "UnlockConnector")
                {
                    if (!string.IsNullOrEmpty(urlChargePointId))
                    {
                        try
                        {
                            ChargePointStatus status = null;
                            if (_chargePointStatusDict.TryGetValue(urlChargePointId, out status))
                            {
                                if (status.Protocol == Protocol_OCPP20)
                                {
                                    await UnlockConnector20(status, context);
                                }
                            }
                            else
                            {
                                // Chargepoint offline
                                Console.WriteLine("OCPPMiddleware UnlockConnector => Chargepoint offline: {0}", urlChargePointId);
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            }
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("OCPPMiddleware UnlockConnector => Error: {0}", exp.Message);
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                    }
                    else
                    {
                        Console.WriteLine("OCPPMiddleware UnlockConnector => Missing chargepoint ID");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    // Unknown action/function
                    Console.WriteLine("OCPPMiddleware => action/function: {0}", cmd);
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
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
