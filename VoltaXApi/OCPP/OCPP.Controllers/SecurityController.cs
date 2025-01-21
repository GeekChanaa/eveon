using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    [ApiController]
    public class SecurityController : Controller
    {
        private readonly ISecurityService _securityService;

        public SecurityController(ISecurityService reportingService)
        {
            _securityService = reportingService;
        }

        [HttpPost("InstallCertificate/{chargePointID}")]
        public async Task<IActionResult> InstallCertificate(string chargePointID, InstallCertificateRequest request)
        {
            await _securityService.InstallCertificate(chargePointID, request);
            return Ok(new {Message = "InstallCertificate request sent successfully." });
        }
    }
}
