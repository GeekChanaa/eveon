using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    [ApiController]
    public class SmartChargingController : Controller
    {
        private readonly ISmartChargingService _smartChargingService;

        public SmartChargingController(ISmartChargingService smartChargingService)
        {
            _smartChargingService = smartChargingService;
        }

        [HttpPost("ClearChargingProfile/{chargePointID}")]
        public async Task<IActionResult> ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request)
        {
            await _smartChargingService.ClearChargingProfile(chargePointID, request);
            return Ok(new { Message = "ClearChargingProfile request sent successfully." });
        }

        [HttpPost("GetChargingProfiles/{chargePointID}")]
        public async Task<IActionResult> GetChargingProfiles(string chargePointID, GetChargingProfilesRequest request)
        {
            await _smartChargingService.GetChargingProfiles(chargePointID, request);
            return Ok(new { Message = "GetChargingProfiles request sent successfully." });
        }

        [HttpPost("SetChargingProfile/{chargePointID}")]
        public async Task<IActionResult> SetChargingProfile(string chargePointID, SetChargingProfileRequest request)
        {
            await _smartChargingService.SetChargingProfile(chargePointID, request);
            return Ok(new { Message = "SetChargingProfile request sent successfully." });
        }

        [HttpPost("ClearedChargingLimit/{chargePointID}")]
        public async Task<IActionResult> ClearedChargingLimit(string chargePointID, ClearedChargingLimitRequest request)
        {
            await _smartChargingService.ClearedChargingLimit(chargePointID, request);
            return Ok(new { Message = "ClearedChargingLimit request sent successfully." });
        }

        [HttpPost("GetCompositeSchedule/{chargePointID}")]
        public async Task<IActionResult> GetCompositeSchedule(string chargePointID, GetCompositeScheduleRequest request)
        {
            await _smartChargingService.GetCompositeSchedule(chargePointID, request);
            return Ok(new { Message = "GetCompositeSchedule request sent successfully." });
        }
    }
}
