using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.Services;
using VoltaXApi.Services.CustomerPortal;
using VoltaXApi.Services.PartnerPortal;

namespace VoltaXApi.Controllers.PartnerPortal
{
    /// <summary>
    /// The partner portal: the signed-in partner's own stations, charge points, prices, sessions, statistics and
    /// statements. Every query is scoped to the caller's partner; another partner's ids answer 404.
    /// </summary>
    [ApiController]
    [Route("api/partner-portal")]
    [PartnerPortal]
    public sealed class PartnerPortalController : ControllerBase
    {
        private readonly PartnerPortalService _portal;

        public PartnerPortalController(PartnerPortalService portal)
        {
            _portal = portal;
        }

        private int PartnerId => this.PartnerId();

        [HttpGet("profile")]
        public async Task<IActionResult> Profile(CancellationToken ct) =>
            await _portal.ProfileAsync(PartnerId, ct) is { } profile ? Ok(profile) : NotFound();

        /// <summary>Replaces the partner's logo (PNG, JPEG or WebP, checked by content), shown in the portal.</summary>
        [HttpPost("profile/logo")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadLogo([FromServices] IPartnerService partners, CancellationToken ct)
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count != 1)
                return BadRequest(new { error = "Upload exactly one image." });
            await partners.UploadPartnerLogo(Request.Form.Files[0], PartnerId);
            return Ok(await _portal.ProfileAsync(PartnerId, ct));
        }

        [HttpGet("overview")]
        public async Task<IActionResult> Overview(CancellationToken ct) => Ok(await _portal.OverviewAsync(PartnerId, ct));

        [HttpGet("stations")]
        public async Task<IActionResult> Stations(CancellationToken ct) => Ok(await _portal.StationsAsync(PartnerId, ct));

        [HttpGet("charge-points")]
        public async Task<IActionResult> ChargePoints([FromQuery] int? stationId, CancellationToken ct) =>
            Ok(await _portal.ChargePointsAsync(PartnerId, stationId, ct));

        [HttpGet("charge-points/{id:int}")]
        public async Task<IActionResult> ChargePoint(int id, CancellationToken ct) =>
            await _portal.ChargePointAsync(PartnerId, id, ct) is { } point ? Ok(point) : NotFound();

        /// <summary>Sets the prices of one of the partner's connectors (VAT included). Started sessions keep their prices.</summary>
        [HttpPut("connectors/{id:int}/pricing")]
        public async Task<IActionResult> UpdatePricing(int id, [FromBody] UpdateConnectorPricingRequest request, CancellationToken ct) =>
            await _portal.UpdatePricingAsync(PartnerId, id, request, ct) is { } connector ? Ok(connector) : NotFound();

        [HttpGet("sessions")]
        public async Task<IActionResult> Sessions([FromQuery] PartnerSessionFilter filter, CancellationToken ct) =>
            Ok(await _portal.SessionsAsync(PartnerId, filter, ct));

        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics(CancellationToken ct) => Ok(await _portal.StatisticsAsync(PartnerId, ct));

        /// <summary>The period's sessions as a CSV file or a printable PDF revenue statement.</summary>
        [HttpGet("statement")]
        public async Task<IActionResult> Statement([FromQuery] PartnerSessionFilter filter, [FromQuery] string format,
            [FromServices] SessionStatementExporter exporter, [FromServices] GlobalConfigurations pricing, CancellationToken ct)
        {
            var rows = await _portal.StatementAsync(PartnerId, filter, ct);
            var name = $"eveon-partner-statement-{DateTime.UtcNow:yyyyMMdd}";
            var partnerName = (await _portal.ProfileAsync(PartnerId, ct))?.Name ?? "Partner";
            return format?.ToLowerInvariant() switch
            {
                "csv" => File(exporter.Csv(rows), "text/csv; charset=utf-8", name + ".csv"),
                "pdf" => File(exporter.Pdf(partnerName, filter.From, filter.To, rows, pricing.Vat), "application/pdf", name + ".pdf"),
                _ => BadRequest(new { error = "format must be csv or pdf" })
            };
        }
    }
}
