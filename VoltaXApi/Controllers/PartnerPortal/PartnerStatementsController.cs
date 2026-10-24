using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoltaXApi.Authorization;
using VoltaXApi.Models;
using VoltaXApi.Services.Billing;

namespace VoltaXApi.Controllers.PartnerPortal
{
    /// <summary>
    /// The signed-in partner's monthly statements (settlements): list, detail with every session, CSV and PDF.
    /// Scoped to the caller's partner; another partner's statement answers 404.
    /// </summary>
    [ApiController]
    [Route("api/partner-portal/statements")]
    [PartnerPortal]
    public sealed class PartnerStatementsController : ControllerBase
    {
        private readonly PartnerSettlementService _settlements;

        public PartnerStatementsController(PartnerSettlementService settlements)
        {
            _settlements = settlements;
        }

        private int PartnerId => this.PartnerId();

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? year, [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default) =>
            Ok(await _settlements.SearchAsync(new SettlementFilter { PartnerId = PartnerId, Year = year, Page = page, PageSize = pageSize }, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id, CancellationToken ct) =>
            await _settlements.DetailAsync(id, PartnerId, ct) is { } detail ? Ok(detail) : NotFound();

        [HttpGet("{id:int}/export")]
        public async Task<IActionResult> Export(int id, [FromQuery] string format, [FromQuery] string? language,
            [FromServices] SettlementDocuments documents, [FromServices] IOptionsMonitor<CompanyInformations> company, CancellationToken ct)
        {
            if (format is not ("csv" or "pdf"))
                return BadRequest(new { error = "format must be csv or pdf" });
            var detail = await _settlements.DetailAsync(id, PartnerId, ct);
            if (detail == null)
                return NotFound();
            var name = SettlementDocuments.FileName(detail.Settlement);
            return format == "csv"
                ? File(documents.Csv(detail), "text/csv; charset=utf-8", name + ".csv")
                : File(documents.Pdf(detail, company.CurrentValue, language ?? BillingTexts.French), "application/pdf", name + ".pdf");
        }
    }
}
