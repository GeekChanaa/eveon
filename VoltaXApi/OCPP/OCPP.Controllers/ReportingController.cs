using Microsoft.AspNetCore.Mvc;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>Every action waits for the charger's answer (see <see cref="OcppCommandResult"/>).</summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class ReportingController : Controller
    {
        private readonly IReportingService _reportingService;

        public ReportingController(IReportingService service)
        {
            _reportingService = service;
        }

        [HttpPost("GetReport/{chargePointID}")]
        public Task<IActionResult> GetReport(string chargePointID, GetReportRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetReport", () => _reportingService.GetReport(chargePointID, request, cancellationToken));

        [HttpPost("GetMonitoringReport/{chargePointID}")]
        public Task<IActionResult> GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetMonitoringReport", () => _reportingService.GetMonitoringReport(chargePointID, request, cancellationToken));

        [HttpPost("GetLog/{chargePointID}")]
        public Task<IActionResult> GetLog(string chargePointID, GetLogRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetLog", () => _reportingService.GetLog(chargePointID, request, cancellationToken));

        [HttpPost("CustomerInformation/{chargePointID}")]
        public Task<IActionResult> CustomerInformation(string chargePointID, CustomerInformationRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("CustomerInformation", () => _reportingService.CustomerInformation(chargePointID, request, cancellationToken));

        [HttpPost("GetBaseReport/{chargePointID}")]
        public Task<IActionResult> GetBaseReport(string chargePointID, GetBaseReportRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetBaseReport", () => _reportingService.GetBaseReport(chargePointID, request, cancellationToken),
                new Dictionary<string, object?> { ["requestID"] = request.RequestId });
    }
}
