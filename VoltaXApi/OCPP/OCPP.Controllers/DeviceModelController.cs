using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>
    /// Reads and edits a charge point's OCPP 2.0.1 device model (GetBaseReport, GetVariables,
    /// SetVariables). Like every OCPP controller, it requires OperateChargePoints (see DashboardAccessFilter).
    /// </summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class DeviceModelController : Controller
    {
        private readonly ProvisioningService _provisioningService;

        public DeviceModelController(ProvisioningService provisioningService)
        {
            _provisioningService = provisioningService;
        }

        [HttpGet("Overview")]
        public async Task<ActionResult<List<ChargePointConfigurationStatusDto>>> Overview() =>
            Ok(await _provisioningService.GetOverview());

        [HttpGet("Status/{chargePointID}")]
        public async Task<ActionResult<ChargePointConfigurationStatusDto>> Status(string chargePointID) =>
            Ok(await _provisioningService.GetStatus(chargePointID));

        [HttpGet("Variables/{chargePointID}")]
        public async Task<ActionResult<List<DeviceModelVariableDto>>> Variables(string chargePointID) =>
            Ok(await _provisioningService.GetDeviceModel(chargePointID));

        [HttpPost("Report/{chargePointID}")]
        public Task<IActionResult> Report(string chargePointID, DeviceModelReportRequestDto request) =>
            WhileOnline(async () =>
            {
                if (!Enum.TryParse<ReportBaseEnumType>(request.ReportBase, true, out var reportBase))
                    return BadRequest(new { error = "ReportBase must be FullInventory, ConfigurationInventory or SummaryInventory." });
                return Ok(await _provisioningService.RequestReport(chargePointID, reportBase));
            });

        [HttpPost("Get/{chargePointID}")]
        public Task<IActionResult> Get(string chargePointID, DeviceModelVariablesRequestDto request) =>
            WhileOnline(async () => Ok(await _provisioningService.ReadVariables(chargePointID, request.Variables)));

        [HttpPost("Set/{chargePointID}")]
        public Task<IActionResult> Set(string chargePointID, DeviceModelVariablesRequestDto request) =>
            WhileOnline(async () => Ok(await _provisioningService.SendVariables(chargePointID, request.Variables)));

        [HttpPost("Reboot/{chargePointID}")]
        public Task<IActionResult> Reboot(string chargePointID, DeviceModelRebootRequestDto request) =>
            WhileOnline(async () => Ok(new { status = await _provisioningService.Reboot(chargePointID, request.Immediate) }));

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
            catch (TimeoutException ex)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, new { error = ex.Message });
            }
            catch (OcppCallErrorException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
        }
    }
}
