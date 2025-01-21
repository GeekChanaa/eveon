using OCPP.Core.Server;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Core
{
  public interface ISecurityService
  {
      Task InstallCertificate(string chargePointID, InstallCertificateRequest request);
  }
}