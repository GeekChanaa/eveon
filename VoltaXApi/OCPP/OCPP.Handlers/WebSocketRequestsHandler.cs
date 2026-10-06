
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
    private IChargePointRepository _chargePointRepo;
    private readonly AuthenticationService _authService;
    private readonly WebSocketSubProtocolMatcher _webSocketSubProtocolMatcher;
    private readonly WebSocketHandler _webSocketHandler;
    private readonly ILogger<WebSocketRequestsHandler> _logger;
    private readonly bool _allowUnauthenticatedChargers;

    public WebSocketRequestsHandler(
      IChargePointRepository chargePointRepo,
      WebSocketSubProtocolMatcher webSocketSubProtocolMatcher,
      WebSocketHandler webSocketHandler,
      ILogger<WebSocketRequestsHandler> logger,
      IConfiguration configuration
    )
    {
      _authService = new AuthenticationService();
      _chargePointRepo = chargePointRepo;
      _webSocketSubProtocolMatcher = webSocketSubProtocolMatcher;
      _webSocketHandler = webSocketHandler;
      _logger = logger;
      _allowUnauthenticatedChargers = configuration.GetValue("Ocpp:AllowUnauthenticatedChargers", false);
    }

    public async Task Handle(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/OCPP"))
        {
          _logger.LogWarning("WebSocket request outside /OCPP rejected: {Path}", context.Request.Path);
          context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
          return;
        }
    
        string chargepointIdentifier = context.Request.Path.Value!.Split('/').Last();
        ChargePoint? chargePoint = await _chargePointRepo.GetChargePointByChargePointIDAsync(chargepointIdentifier);
        if (chargePoint == null)
        {
            _logger.LogWarning("OCPP connection rejected: unknown charge point {ChargePointId}", chargepointIdentifier);
            RejectUnauthorized(context);
            return;
        }

        if (!await IsAuthenticated(context, chargePoint, chargepointIdentifier))
        {
            RejectUnauthorized(context);
            return;
        }

        // Only versions with registered handlers are offered (see OcppInboundHandlerRegistry).
        string? subProtocol = _webSocketSubProtocolMatcher.GetMatchingSubProtocol(context);
        if (string.IsNullOrEmpty(subProtocol))
        {
            _logger.LogWarning("OCPP connection rejected for {ChargePointId}: none of the requested sub-protocols [{Requested}] is supported [{Supported}]",
                chargepointIdentifier, string.Join(", ", context.WebSockets.WebSocketRequestedProtocols), string.Join(", ", _webSocketSubProtocolMatcher.SupportedProtocols));
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        var chargePointStatus = new ChargePointStatus(chargePoint) { Protocol = subProtocol };
        await _webSocketHandler.AcceptWebSocketAsync(context, subProtocol, chargePointStatus);
    }

    private async Task<bool> IsAuthenticated(HttpContext context, ChargePoint chargePoint, string identity)
    {
        var result = _authService.AuthenticateChargePoint(context, chargePoint, identity, out var upgradedHash);
        if (result == ChargePointAuthResult.Success)
        {
            if (upgradedHash != null)
            {
                try
                {
                    chargePoint.Password = upgradedHash;
                    await _chargePointRepo.Update(chargePoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not upgrade legacy password hash for charge point {ChargePointId}", identity);
                }
            }
            return true;
        }

        if (result == ChargePointAuthResult.NoCredentialsConfigured && _allowUnauthenticatedChargers)
        {
            _logger.LogWarning("SECURITY: charge point {ChargePointId} has no password or certificate and was accepted WITHOUT authentication because Ocpp:AllowUnauthenticatedChargers is enabled. Set a password for this charger.", identity);
            return true;
        }

        _logger.LogWarning("OCPP connection rejected for charge point {ChargePointId} from {RemoteIp}: {Reason}",
            identity, context.Connection.RemoteIpAddress, result);
        return false;
    }

    private static void RejectUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"OCPP\", charset=\"UTF-8\"";
    }
  }
}