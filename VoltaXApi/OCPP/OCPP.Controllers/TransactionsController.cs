using Microsoft.AspNetCore.Mvc;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>Every action waits for the charger's answer (see <see cref="OcppCommandResult"/>).</summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class TransactionsController : Controller
    {
        private readonly IOCPPTransactionsService _transactionsService;

        public TransactionsController(IOCPPTransactionsService service)
        {
            _transactionsService = service;
        }

        [HttpPost("GetTransactionStatus/{chargePointID}")]
        public Task<IActionResult> GetTransactionStatus(string chargePointID, GetTransactionStatusRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetTransactionStatus", () => _transactionsService.GetTransactionStatus(chargePointID, request, cancellationToken));
    }
}
