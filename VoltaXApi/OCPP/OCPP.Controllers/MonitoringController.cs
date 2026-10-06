using Microsoft.AspNetCore.Mvc;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>Every action waits for the charger's answer (see <see cref="OcppCommandResult"/>).</summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class MonitoringController : Controller
    {
        private readonly IMonitoringService _monitoringService;

        public MonitoringController(IMonitoringService service)
        {
            _monitoringService = service;
        }

        [HttpPost("SetVariableMonitoring/{chargePointID}")]
        public Task<IActionResult> SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetVariableMonitoring", () => _monitoringService.SetVariableMonitoring(chargePointID, request, cancellationToken));

        [HttpPost("ClearVariableMonitoring/{chargePointID}")]
        public Task<IActionResult> ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ClearVariableMonitoring", () => _monitoringService.ClearVariableMonitoring(chargePointID, request, cancellationToken));

        [HttpPost("SetMonitoringLevel/{chargePointID}")]
        public Task<IActionResult> SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetMonitoringLevel", () => _monitoringService.SetMonitoringLevel(chargePointID, request, cancellationToken));

        [HttpPost("SetMonitoringBase/{chargePointID}")]
        public Task<IActionResult> SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetMonitoringBase", () => _monitoringService.SetMonitoringBase(chargePointID, request, cancellationToken));

        [HttpPost("SetVariables/{chargePointID}")]
        public Task<IActionResult> SetVariables(string chargePointID, SetVariablesRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetVariables", () => _monitoringService.SetVariables(chargePointID, request, cancellationToken));

        [HttpPost("GetVariables/{chargePointID}")]
        public Task<IActionResult> GetVariables(string chargePointID, GetVariablesRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetVariables", () => _monitoringService.GetVariables(chargePointID, request, cancellationToken));
    }
}
