using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  /// <summary>
  /// EV driver commands. Each waits for the charger's answer and returns it; see IOcppCommandSender for the exceptions
  /// (not connected, timeout, CALLERROR).
  /// </summary>
  public interface IEVDriverService
  {
      Task<RequestStartTransactionResponse> RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request, CancellationToken cancellationToken = default);
      Task<RequestStartTransactionResponse> RequestStartTransactionMobile(string chargePointID, RequestStartTransactionRequest request, int userID, CancellationToken cancellationToken = default);
      Task<RequestStopTransactionResponse> RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request, CancellationToken cancellationToken = default);
      Task<RequestStopTransactionResponse> RequestStopTransactionMobile(string chargePointID, RequestStopTransactionRequest request, int userID, CancellationToken cancellationToken = default);
      Task<CancelReservationResponse> CancelReservation(string chargePointID, CancelReservationRequest request, CancellationToken cancellationToken = default);
      Task<ReserveNowResponse> ReserveNow(string chargePointID, ReserveNowRequest request, CancellationToken cancellationToken = default);
      Task<UnlockConnectorResponse> UnlockConnector(string chargePointID, UnlockConnectorRequest request, CancellationToken cancellationToken = default);
      Task<ClearCacheResponse> ClearCache(string chargePointID, ClearCacheRequest request, CancellationToken cancellationToken = default);
      Task<SendLocalListResponse> SendLocalList(string chargePointID, SendLocalListRequest request, CancellationToken cancellationToken = default);
      Task<GetLocalListVersionResponse> GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request, CancellationToken cancellationToken = default);
  }
}
