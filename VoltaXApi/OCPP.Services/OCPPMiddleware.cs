using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Server
{
    public partial class OCPPMiddleware
    {
        // Supported OCPP protocols (in order)
        private const string Protocol_OCPP16 = "ocpp1.6";
        private const string Protocol_OCPP20 = "ocpp2.0.1";
        private static readonly string[] SupportedProtocols = { Protocol_OCPP20, Protocol_OCPP16 /*, "ocpp1.5" */};

        // RegExp for splitting ocpp message parts
        // ^\[\s*(\d)\s*,\s*\"([^"]*)\"\s*,(?:\s*\"(\w*)\"\s*,)?\s*(.*)\s*\]$
        // Third block is optional, because responses don't have an action
        private static string MessageRegExp = "^\\[\\s*(\\d)\\s*,\\s*\"([^\"]*)\"\\s*,(?:\\s*\"(\\w*)\"\\s*,)?\\s*(.*)\\s*\\]$";

        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _logFactory;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        // Dictionary with status objects for each charge point
        private static Dictionary<string, ChargePointStatus> _chargePointStatusDict = new Dictionary<string, ChargePointStatus>();

        // Dictionary for processing asynchronous API calls
        private Dictionary<string, OCPPMessage> _requestQueue = new Dictionary<string, OCPPMessage>();

        public OCPPMiddleware(RequestDelegate next, ILoggerFactory logFactory, IConfiguration configuration)
        {
            _next = next;
            _logFactory = logFactory;
            _configuration = configuration;

            _logger = logFactory.CreateLogger("OCPPMiddleware");
        }

        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine("OCPPMiddleware => Websocket request: Path='{0}'", context.Request.Path);

            ChargePointStatus? chargePointStatus = null;

            if (context.WebSockets.IsWebSocketRequest)
            {
                if (context.Request.Path.StartsWithSegments("/OCPP"))
                {
                    string chargepointIdentifier;
                    string[] parts = context.Request.Path.Value.Split('/');
                    chargepointIdentifier = parts[parts.Length - 1];

                    Console.WriteLine("OCPPMiddleware => Connection request with chargepoint identifier = '{0}'", chargepointIdentifier);

                    // Known chargepoint?
                    if (!string.IsNullOrWhiteSpace(chargepointIdentifier.ToString()))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                        optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
                        using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                        {
                            ChargePoint? chargePoint = dbContext.ChargePoints.Where(u => u.ChargePointId == chargepointIdentifier).FirstOrDefault();
                            if (chargePoint != null)
                            {
                                Console.WriteLine("OCPPMiddleware => SUCCESS: Found chargepoint with identifier={0}", chargePoint.ChargePointId);

                                // Check optional chargepoint authentication
                                if (!string.IsNullOrWhiteSpace(chargePoint.Username))
                                {
                                    // Chargepoint MUST send basic authentication header

                                    bool basicAuthSuccess = false;
                                    string authHeader = context.Request.Headers["Authorization"];
                                    Console.WriteLine("this is the authHeader : " + authHeader);
                                    if (!string.IsNullOrEmpty(authHeader))
                                    {
                                        string[] cred = System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(authHeader.Substring(6))).Split(':');
                                        if (cred.Length == 2 && chargePoint.Username == cred[0] && chargePoint.Password == cred[1])
                                        {
                                            // Authentication match => OK
                                            Console.WriteLine("OCPPMiddleware => SUCCESS: Basic authentication for chargepoint '{0}' match", chargePoint.ChargePointId);
                                            basicAuthSuccess = true;
                                        }
                                        else
                                        {
                                            // Authentication does NOT match => Failure
                                            Console.WriteLine("OCPPMiddleware => FAILURE: Basic authentication for chargepoint '{0}' does NOT match", chargePoint.ChargePointId);
                                        }
                                    }
                                    if (basicAuthSuccess == false)
                                    {
                                        context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"OCPP.Core\"");
                                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                        return;
                                    }

                                }
                                else if (!string.IsNullOrWhiteSpace(chargePoint.ClientCertThumb))
                                {
                                    // Chargepoint MUST send basic authentication header
                                    bool certAuthSuccess = false;
                                    X509Certificate2 clientCert = context.Connection.ClientCertificate;
                                    if (clientCert != null)
                                    {
                                        if (clientCert.Thumbprint.Equals(chargePoint.ClientCertThumb, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            // Authentication match => OK
                                            Console.WriteLine("OCPPMiddleware => SUCCESS: Certificate authentication for chargepoint '{0}' match", chargePoint.ChargePointId);
                                            certAuthSuccess = true;
                                        }
                                        else
                                        {
                                            // Authentication does NOT match => Failure
                                            Console.WriteLine("OCPPMiddleware => FAILURE: Certificate authentication for chargepoint '{0}' does NOT match", chargePoint.ChargePointId);
                                        }
                                    }
                                    if (certAuthSuccess == false)
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                        return;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("OCPPMiddleware => No authentication for chargepoint '{0}' configured", chargePoint.ChargePointId);
                                }

                                // Store chargepoint data
                                chargePointStatus = new ChargePointStatus(chargePoint);
                            }
                            else
                            {
                                Console.WriteLine("OCPPMiddleware => FAILURE: Found no chargepoint with identifier={0}", chargepointIdentifier);
                            }
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
                                        // Check if this chargepoint already/still hat a status object
                                        if (_chargePointStatusDict.ContainsKey(chargepointIdentifier.ToString()))
                                        {
                                            // exists => check status
                                            if (_chargePointStatusDict[chargepointIdentifier.ToString()].WebSocket.State != WebSocketState.Open)
                                            {
                                                // Closed or aborted => remove
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
                                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                }

                                if (statusSuccess)
                                {
                                    // Handle socket communication
                                    Console.WriteLine("OCPPMiddleware => Waiting for message...");

                                    using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol))
                                    {
                                        Console.WriteLine("OCPPMiddleware => WebSocket connection with charge point '{0}'", chargepointIdentifier);
                                        chargePointStatus.WebSocket = webSocket;

                                        if (subProtocol == Protocol_OCPP20)
                                        {
                                            // OCPP V2.0
                                            await Receive20(chargePointStatus, context);
                                        }
                                        // else
                                        // {
                                        //     // OCPP V1.6
                                        //     await Receive16(chargePointStatus, context);
                                        // }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // no websocket request => failure
                            Console.WriteLine("OCPPMiddleware => Non-Websocket request");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                    else
                    {
                        // unknown chargepoint
                        Console.WriteLine("OCPPMiddleware => no chargepoint: http 412");
                        context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/API"))
                {
                    // Check authentication (X-API-Key)
                    string apiKeyConfig = _configuration.GetValue<string>("ApiKey");
                    if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                    {
                        // ApiKey specified => check request
                        string apiKeyCaller = context.Request.Headers["X-API-Key"].FirstOrDefault();
                        if (apiKeyConfig == apiKeyCaller)
                        {
                            // API-Key matches
                            Console.WriteLine("OCPPMiddleware => Success: X-API-Key matches");
                        }
                        else
                        {
                            // API-Key does NOT matches => authentication failure!!!
                            Console.WriteLine("OCPPMiddleware => Failure: Wrong X-API-Key! Caller='{0}'", apiKeyCaller);
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            return;
                        }
                    }
                    else
                    {
                        // No API-Key configured => no authenticatiuon
                        Console.WriteLine("OCPPMiddleware => No X-API-Key configured!");
                    }

                    // format: /API/<command>[/chargepointId]
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
                                        // Send message to chargepoint
                                        if (status.Protocol == Protocol_OCPP20)
                                        {
                                            // OCPP V2.0
                                            await Reset20(status, context);
                                        }
                                        // else
                                        // {
                                        //     // OCPP V1.6
                                        //     await Reset16(status, context);
                                        // }
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
                                        // Send message to chargepoint
                                        if (status.Protocol == Protocol_OCPP20)
                                        {
                                            // OCPP V2.0
                                            await UnlockConnector20(status, context);
                                        }
                                        // else
                                        // {
                                        //     // OCPP V1.6
                                        //     await UnlockConnector16(status, context);
                                        // }
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
                else if (context.Request.Path.StartsWithSegments("/"))
                {
                    try
                    {
                        bool showIndexInfo = _configuration.GetValue<bool>("ShowIndexInfo");
                        if (showIndexInfo)
                        {
                            Console.WriteLine("OCPPMiddleware => Index status page");

                            context.Response.ContentType = "text/plain";
                            await context.Response.WriteAsync(string.Format("Running...\r\n\r\n{0} chargepoints connected", _chargePointStatusDict.Values.Count));
                        }
                        else
                        {
                            Console.WriteLine("OCPPMiddleware => Root path with deactivated index page");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("OCPPMiddleware => Error: {0}", exp.Message);
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
                else
                {
                    Console.WriteLine("OCPPMiddleware => Bad path request");
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
            else
            {
                // If it's not a WebSocket connection, just call the next middleware in the pipeline.
                await _next(context);
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
