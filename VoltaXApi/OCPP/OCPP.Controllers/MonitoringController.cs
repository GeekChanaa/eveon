using Microsoft.AspNetCore.Mvc;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using System.Threading.Tasks;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    [ApiController]
    public class MonitoringController : Controller
    {
        private readonly IMonitoringService _monitoringService;

        public MonitoringController(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        [HttpPost("SetVariableMonitoring/{chargePointID}")]
        public async Task<IActionResult> SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request)
        {
            await _monitoringService.SetVariableMonitoring(chargePointID, request);
            return Ok(new { Message = "SetVariableMonitoring request sent successfully." });
        }

        [HttpPost("ClearVariableMonitoring/{chargePointID}")]
        public async Task<IActionResult> ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request)
        {
            await _monitoringService.ClearVariableMonitoring(chargePointID, request);
            return Ok(new { Message = "ClearVariableMonitoring request sent successfully." });
        }

        [HttpPost("SetMonitoringLevel/{chargePointID}")]
        public async Task<IActionResult> SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request)
        {
            await _monitoringService.SetMonitoringLevel(chargePointID, request);
            return Ok(new { Message = "SetMonitoringLevel request sent successfully." });
        }

        [HttpPost("SetMonitoringBase/{chargePointID}")]
        public async Task<IActionResult> SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request)
        {
            await _monitoringService.SetMonitoringBase(chargePointID, request);
            return Ok(new { Message = "SetMonitoringBase request sent successfully." });
        }

        [HttpPost("SetVariables/{chargePointID}")]
        public async Task<IActionResult> SetVariables(string chargePointID, SetVariablesRequest request)
        {
            await _monitoringService.SetVariables(chargePointID, request);
            return Ok(new { Message = "SetVariables request sent successfully." });
        }

        [HttpPost("GetVariables/{chargePointID}")]
        public async Task<IActionResult> GetVariables(string chargePointID, GetVariablesRequest request)
        {
            await _monitoringService.GetVariables(chargePointID, request);
            return Ok(new { Message = "GetVariables request sent successfully." });
        }
    }
}
