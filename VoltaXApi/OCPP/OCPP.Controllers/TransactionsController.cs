using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    [ApiController]
    public class OcppTransactionsController : Controller
    {
        private readonly IOCPPTransactionsService _smartChargingService;

        public OcppTransactionsController(IOCPPTransactionsService smartChargingService)
        {
            _smartChargingService = smartChargingService;
        }

        [HttpPost("ClearChargingProfile/{chargePointID}")]
        public async Task<IActionResult> ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request)
        {
            await _smartChargingService.ClearChargingProfile(chargePointID, request);
            return Ok(new { Message = "ClearChargingProfile request sent successfully." });
        }

    }
}
