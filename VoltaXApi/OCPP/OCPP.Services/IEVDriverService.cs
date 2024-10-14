

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface IEVDriverService
  {
      Task RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request);
      Task RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request);
      Task CancelReservation(string chargePointID, CancelReservationRequest request);
      Task ReserveNow(string chargePointID, ReserveNowRequest request);
      Task UnlockConnector(string chargePointID, UnlockConnectorRequest request);
      Task ClearCache(string chargePointID, ClearCacheRequest request);
      Task SendLocalList(string chargePointID, SendLocalListRequest request);
      Task GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request);

  }
}