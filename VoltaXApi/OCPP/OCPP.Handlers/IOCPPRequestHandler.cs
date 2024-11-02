using OCPP.Core.Server;
using VoltaXApi.OCPP.Models;


namespace VoltaXApi.OCPP.Handlers
{
  public interface IOCPPRequestHandler
  {
    public Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus);
  }
}