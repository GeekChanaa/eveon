using OCPP.Core.Server;

namespace VoltaXApi.OCPP.Core
{
  public interface IMessageProcessor
  {
      Task ProcessMessage(OCPPMessage message, ChargePointStatus chargePointStatus, HttpContext context,Dictionary<string, OCPPMessage> requestQueue, string ocppMessage);
  }
}