using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>
    /// First-connection provisioning. Like every OCPP controller, it requires OperateChargePoints
    /// (see DashboardAccessFilter).
    /// </summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class ProvisioningController : Controller
    {
        private readonly ProvisioningService _provisioningService;

        public ProvisioningController(ProvisioningService provisioningService)
        {
            _provisioningService = provisioningService;
        }

        [HttpGet("Pending")]
        public async Task<ActionResult<List<PendingProvisioningChargePointDto>>> Pending() =>
            Ok(await _provisioningService.GetPending());

        [HttpPost("Discover/{chargePointID}")]
        public Task<IActionResult> Discover(string chargePointID) =>
            WhileOnline(async () => Ok(await _provisioningService.Discover(chargePointID)));

        [HttpPost("Apply/{chargePointID}")]
        public Task<IActionResult> Apply(string chargePointID, ApplyProvisioningDto request) =>
            WhileOnline(async () => Ok(await _provisioningService.Apply(chargePointID, request, CurrentUserId())));

        [HttpPost("Skip/{chargePointID}")]
        public Task<IActionResult> Skip(string chargePointID) =>
            WhileOnline(async () => Ok(await _provisioningService.Skip(chargePointID, CurrentUserId())));

        private async Task<IActionResult> WhileOnline(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (WebSocketNotFoundException)
            {
                return Conflict(new { error = "The charge point is not connected." });
            }
        }

        private int? CurrentUserId() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }
}
