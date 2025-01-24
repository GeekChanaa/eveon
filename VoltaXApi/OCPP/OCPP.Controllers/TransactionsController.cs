using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    [ApiController]
    public class TransactionsController : Controller
    {
        private readonly IOCPPTransactionsService _transactionsService;

        public TransactionsController(IOCPPTransactionsService transactionsService)
        {
            _transactionsService = transactionsService;
        }

        [HttpPost("ClearChargingProfile/{chargePointID}")]
        public async Task<IActionResult> ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request)
        {
            await _transactionsService.ClearChargingProfile(chargePointID, request);
            return Ok(new { Message = "ClearChargingProfile request sent successfully." });
        }

        [HttpPost("GetTransactionStatus/{chargePointID}")]
        public async Task<IActionResult> GetTransactionStatus(string chargePointID, GetTransactionStatusRequest request)
        {
            await _transactionsService.GetTransactionStatusRequest(chargePointID, request);
            return Ok(new { Message = "GetTransactionStatus request sent successfully." });
        }

    }
}
