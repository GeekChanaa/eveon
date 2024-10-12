using OCPP.Core.Server;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Core
{
  public interface IMessageProcessor
  {
      Task ProcessMessage(OCPPMessage message, ChargePointStatus chargePointStatus, HttpContext context, string ocppMessage);
  }
}