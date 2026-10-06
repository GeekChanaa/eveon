using System.Text;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Pki;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>
    /// Certificate management and security profiles. Commands wait for the charger's answer (see <see cref="OcppCommandResult"/>).
    /// Reads follow the OCPP controllers' access (OperateChargePoints); every action that changes charger or PKI state is admin-only.
    /// </summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class SecurityController : Controller
    {
        private readonly ISecurityService _securityService;
        private readonly IChargerCertificateAuthority _ca;

        public SecurityController(ISecurityService service, IChargerCertificateAuthority ca)
        {
            _securityService = service;
            _ca = ca;
        }

        [HttpPost("InstallCertificate/{chargePointID}")]
        public Task<IActionResult> InstallCertificate(string chargePointID, InstallCertificateRequest request, CancellationToken cancellationToken) =>
            AdminCommand("InstallCertificate", () => _securityService.InstallCertificate(chargePointID, request, cancellationToken));

        [HttpGet("Certificates/{chargePointID}")]
        public async Task<IActionResult> GetCertificates(string chargePointID, CancellationToken cancellationToken)
        {
            var result = await _securityService.GetCertificates(chargePointID, cancellationToken);
            return result == null ? NotFound(new { message = "Unknown charge point." }) : Ok(result);
        }

        [HttpPost("InstallRootCertificate/{chargePointID}")]
        public Task<IActionResult> InstallRootCertificate(string chargePointID, InstallRootCertificateDto request, CancellationToken cancellationToken) =>
            AdminCommand("InstallCertificate", () => _securityService.InstallRootCertificate(chargePointID, request, cancellationToken));

        [HttpPost("DeleteCertificate/{chargePointID}")]
        public Task<IActionResult> DeleteCertificate(string chargePointID, CertificateHashDataType request, CancellationToken cancellationToken) =>
            AdminCommand("DeleteCertificate", () => _securityService.DeleteCertificate(chargePointID, request, cancellationToken));

        [HttpPost("GetInstalledCertificateIds/{chargePointID}")]
        public Task<IActionResult> GetInstalledCertificateIds(string chargePointID, GetInstalledCertificateIdsDto? request, CancellationToken cancellationToken) =>
            AdminCommand("GetInstalledCertificateIds", () => _securityService.GetInstalledCertificateIds(chargePointID, request?.CertificateTypes, cancellationToken));

        [HttpPost("TriggerCertificateRenewal/{chargePointID}")]
        public Task<IActionResult> TriggerCertificateRenewal(string chargePointID, CancellationToken cancellationToken) =>
            AdminCommand("TriggerMessage", () => _securityService.TriggerCertificateRenewal(chargePointID, cancellationToken));

        [HttpPost("SetSecurityProfile/{chargePointID}")]
        public async Task<IActionResult> SetSecurityProfile(string chargePointID, SetSecurityProfileDto request, CancellationToken cancellationToken)
        {
            if (!IsAdmin) return Forbid();
            try
            {
                return Ok(await _securityService.SetSecurityProfile(chargePointID, request, cancellationToken));
            }
            catch (PkiRequestException ex)
            {
                return BadRequest(new { message = ex.Message, error = ex.Message });
            }
        }

        [HttpPost("RevokeCertificate/{chargePointID}/{certificateID:int}")]
        public async Task<IActionResult> RevokeCertificate(string chargePointID, int certificateID, RevokeChargerCertificateDto? request, CancellationToken cancellationToken)
        {
            if (!IsAdmin) return Forbid();
            var result = await _securityService.RevokeCertificate(chargePointID, certificateID, request?.Reason, cancellationToken);
            return result == null ? NotFound(new { message = "Unknown certificate." }) : Ok(result);
        }

        [HttpGet("Ca")]
        public async Task<IActionResult> GetCa(CancellationToken cancellationToken) => Ok(await _ca.DescribeAsync(cancellationToken));

        /// <summary>The charger CA certificate (public part only), PEM.</summary>
        [HttpGet("Ca/Certificate")]
        public async Task<IActionResult> DownloadCaCertificate(CancellationToken cancellationToken)
        {
            var ca = await _ca.GetCaAsync(cancellationToken);
            if (ca == null) return NotFound(new { message = "No charger CA is configured." });
            return File(Encoding.ASCII.GetBytes(ChargerPkiCrypto.ToPem(ca)), "application/x-pem-file", "eveon-charger-ca.pem");
        }

        private bool IsAdmin => HttpContext.Items[typeof(AccessSnapshot)] is AccessSnapshot { IsAdmin: true };

        private async Task<IActionResult> AdminCommand<TResponse>(string action, Func<Task<TResponse>> send)
        {
            if (!IsAdmin) return Forbid();
            try
            {
                return await OcppCommandResult.Run(action, send);
            }
            catch (PkiRequestException ex)
            {
                return BadRequest(new { message = ex.Message, error = ex.Message });
            }
        }
    }
}
