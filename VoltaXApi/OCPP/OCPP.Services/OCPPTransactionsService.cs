using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Transaction commands. Every command waits for the charger's answer (see <see cref="IOcppCommandSender"/>).</summary>
    public class OCPPTransactionsService : IOCPPTransactionsService
    {
        private readonly IOcppCommandSender _commandSender;
        private readonly ILogger<OCPPTransactionsService> _logger;

        public OCPPTransactionsService(IOcppCommandSender commandSender, ILogger<OCPPTransactionsService> logger)
        {
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task<GetTransactionStatusResponse> GetTransactionStatus(string chargePointID, GetTransactionStatusRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<GetTransactionStatusRequest, GetTransactionStatusResponse>(chargePointID, "GetTransactionStatus", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetTransactionStatus answered by {ChargePointId}", chargePointID);
            return response;
        }
    }
}
