using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Core
{
  public interface IOCPPTransactionsService
  {
    Task<GetTransactionStatusResponse> GetTransactionStatus(string chargePointID, GetTransactionStatusRequest request, CancellationToken cancellationToken = default);
  }
}
