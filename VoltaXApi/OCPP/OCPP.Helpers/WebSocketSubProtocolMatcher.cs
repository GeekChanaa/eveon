using System.Net.WebSockets;

namespace VoltaXApi.OCPP.Helpers
{
  public class WebSocketSubProtocolMatcher
  {
      private static readonly string[] SupportedProtocols = { "ocpp2.0.1", "ocpp1.6" };

      public string? GetMatchingSubProtocol(HttpContext context)
      {
          foreach (var protocol in SupportedProtocols)
          {
              if (context.WebSockets.WebSocketRequestedProtocols.Contains(protocol))
              {
                  return protocol;
              }
          }
          return null;
      }
  }

}
