
namespace VoltaXApi.OCPP.Exceptions
{
  public class WebSocketNotFoundException : Exception
  {
      public WebSocketNotFoundException(string message) : base(message)
      {
      }
  }
}