using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    [ApiController]
    public class ReportingController : Controller
    {
        private readonly IReportingService _reportingService;

        public ReportingController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        [HttpPost("GetBaseReport/{chargePointID}")]
        public async Task<IActionResult> GetBaseReport(string chargePointID, GetBaseReportRequest request)
        {
            await _reportingService.GetBaseReport(chargePointID, request);
            return Ok(new { Message = "GetBaseReport request sent successfully." });
        }

        [HttpPost("GetReport/{chargePointID}")]
        public async Task<IActionResult> GetReport(string chargePointID, GetReportRequest request)
        {
            await _reportingService.GetReport(chargePointID, request);
            return Ok(new { Message = "GetReport request sent successfully." });
        }

        [HttpPost("GetMonitoringReport/{chargePointID}")]
        public async Task<IActionResult> GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request)
        {
            await _reportingService.GetMonitoringReport(chargePointID, request);
            return Ok(new { Message = "GetMonitoringReport request sent successfully." });
        }

        [HttpPost("GetLog/{chargePointID}")]
        public async Task<IActionResult> GetLog(string chargePointID, GetLogRequest request)
        {
            await _reportingService.GetLog(chargePointID, request);
            return Ok(new { Message = "GetLog request sent successfully." });
        }

        [HttpPost("CustomerInformation/{chargePointID}")]
        public async Task<IActionResult> CustomerInformation(string chargePointID, CustomerInformationRequest request)
        {
            await _reportingService.CustomerInformation(chargePointID, request);
            return Ok(new { Message = "CustomerInformation request sent successfully." });
        }
    }
}
