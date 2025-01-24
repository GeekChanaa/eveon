using OCPP.Core.Server;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Core
{
  public interface IOCPPTransactionsService
  {
      Task GetTransactionStatusRequest(string chargePointID, GetTransactionStatusRequest request);
      Task ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request);
  }
}