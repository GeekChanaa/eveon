using VoltaXApi.OCPP.Models;


namespace VoltaXApi.OCPP.Handlers
{
  public interface IWebSocketRequestsHandler
  {
    Task Handle(HttpContext context);
  }
}