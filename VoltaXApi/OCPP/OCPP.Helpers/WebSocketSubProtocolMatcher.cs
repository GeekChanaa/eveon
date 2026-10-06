using VoltaXApi.OCPP.Factories;

namespace VoltaXApi.OCPP.Helpers
{
  /// <summary>
  /// Picks the OCPP sub-protocol for a handshake. Only versions with registered inbound handlers are offered
  /// (see <see cref="OcppInboundHandlerRegistry"/>), most preferred first.
  /// </summary>
  public class WebSocketSubProtocolMatcher
  {
      private readonly OcppInboundHandlerRegistry _registry;

      public WebSocketSubProtocolMatcher(OcppInboundHandlerRegistry registry)
      {
          _registry = registry;
      }

      public IReadOnlyList<string> SupportedProtocols => _registry.SupportedProtocols;

      public string? GetMatchingSubProtocol(HttpContext context) =>
          GetMatchingSubProtocol(context.WebSockets.WebSocketRequestedProtocols);

      public string? GetMatchingSubProtocol(IEnumerable<string> requestedProtocols)
      {
          var requested = requestedProtocols.ToList();
          foreach (var protocol in _registry.SupportedProtocols)
          {
              var match = requested.FirstOrDefault(r => string.Equals(r, protocol, StringComparison.OrdinalIgnoreCase));
              if (match != null) return protocol;
          }
          return null;
      }
  }
}
