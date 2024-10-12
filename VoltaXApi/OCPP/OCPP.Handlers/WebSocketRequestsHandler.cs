
using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.Data;
using System.Net;
using System.Net.WebSockets;
using OCPP.Core.Server;
using VoltaXApi.Services;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class WebSocketRequestsHandler : IWebSocketRequestsHandler
  {
    private const string Protocol_OCPP16 = "ocpp1.6";
    private const string Protocol_OCPP20 = "ocpp2.0.1";
    private static readonly string[] SupportedProtocols = { Protocol_OCPP20, Protocol_OCPP16 };
    private IChargePointRepository _chargePointRepo;
    private readonly AuthenticationService _authService;
    private readonly WebSocketManagerService _webSocketManagerService;
    private readonly ChargePointStatusManagerService _chargePointStatusManagerService;
    private readonly WebSocketSubProtocolMatcher _webSocketSubProtocolMatcher;
    private readonly WebSocketHandler _webSocketHandler;
    private readonly RequestQueueManagerService _requestQueueManagerService;


    public WebSocketRequestsHandler(
      IChargePointRepository chargePointRepo,
      WebSocketManagerService webSocketManagerService,
      ChargePointStatusManagerService chargePointStatusManagerService,
      WebSocketSubProtocolMatcher webSocketSubProtocolMatcher,
      WebSocketHandler webSocketHandler,
      RequestQueueManagerService requestQueueManagerService
    )
    {
      _authService = new AuthenticationService();
      _chargePointRepo = chargePointRepo;
      _webSocketManagerService = webSocketManagerService;
      _chargePointStatusManagerService = chargePointStatusManagerService;
      _webSocketSubProtocolMatcher = webSocketSubProtocolMatcher;
      _webSocketHandler = webSocketHandler;
      _requestQueueManagerService = requestQueueManagerService;
    }

    public async Task Handle(HttpContext context)
    {
        Console.WriteLine("OCPPMiddleware => Websocket request: Path='{0}'", context.Request.Path);
        
        ChargePointStatus? chargePointStatus = null;

        if (!context.Request.Path.StartsWithSegments("/OCPP"))
        {
            Console.WriteLine("Invalid Path.");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }
    
        string chargepointIdentifier = context.Request.Path.Value.Split('/').Last();
        ChargePoint? chargePoint = await _chargePointRepo.GetChargePointByChargePointIDAsync(chargepointIdentifier);
        if (chargePoint == null)
        {
            Console.WriteLine($"No ChargePoint found with identifier {chargepointIdentifier}");
            context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
            return;
        }
 
        Console.WriteLine("Found chargepoint with identifier={0}", chargePoint.ChargePointId);
        _authService.AuthenticateChargePoint(context, chargePoint);

        chargePointStatus = new ChargePointStatus(chargePoint);
        if (chargePointStatus != null)
        {
            // Match supported sub protocols
            string subProtocol = _webSocketSubProtocolMatcher.GetMatchingSubProtocol(context);
            if (string.IsNullOrEmpty(subProtocol))
            {
                Console.WriteLine("OCPPMiddleware => No supported sub-protocol from charge station '{0}'", chargepointIdentifier);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else
            {
                chargePointStatus.Protocol = subProtocol;

               _chargePointStatusManagerService.UpdateChargePointStatus(chargepointIdentifier, chargePointStatus);
                
                Console.WriteLine("OCPPMiddleware => Waiting for message...");
                await _webSocketHandler.AcceptWebSocketAsync(context, subProtocol, chargePointStatus);
                
            }
        }
        else
        {
            Console.WriteLine("OCPPMiddleware => no chargepoint: http 412");
            context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
        }
    }


  }
}