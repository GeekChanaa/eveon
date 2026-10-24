using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoltaXApi.Models;
using VoltaXApi.Services.Billing;

namespace VoltaXApi.Controllers
{
    /// <summary>
    /// Partner commission terms and monthly settlements for operators: reading needs ViewPartners; changing terms,
    /// generating settlements and recording payouts need EditPartners.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class SettlementController : ControllerBase
    {
        private readonly PartnerSettlementService _settlements;

        public SettlementController(PartnerSettlementService settlements)
        {
            _settlements = settlements;
        }

        private int? Me => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        [HttpGet("Commissions")]
        public async Task<IActionResult> GetCommissions(CancellationToken ct) => Ok(await _settlements.CommissionsAsync(ct));

        [HttpPut("Commissions/{partnerID:int}")]
        public async Task<IActionResult> UpdateCommission(int partnerID, [FromBody] UpdateCommissionRequest request, CancellationToken ct) =>
            await _settlements.UpdateCommissionAsync(partnerID, request, ct) is { } terms ? Ok(terms) : NotFound();

        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] SettlementFilter filter, CancellationToken ct) => Ok(await _settlements.SearchAsync(filter, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSettlement(int id, CancellationToken ct) =>
            await _settlements.DetailAsync(id, null, ct) is { } detail ? Ok(detail) : NotFound();

        /// <summary>Computes the month for one partner or all of them (pending settlements are recomputed, paid ones kept).</summary>
        [HttpPost("Generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateSettlementsRequest request, CancellationToken ct)
        {
            var created = request.PartnerId.HasValue
                ? new[] { await _settlements.GenerateAsync(request.PartnerId.Value, request.Year, request.Month, Me, ct) }.Where(s => s != null).ToList()
                : await _settlements.GenerateAllAsync(request.Year, request.Month, Me, ct: ct);
            var result = new List<SettlementDto>();
            foreach (var settlement in created)
                if (await _settlements.GetAsync(settlement!.ID, null, ct) is { } dto)
                    result.Add(dto);
            return Ok(result);
        }

        [HttpPost("{id:int}/Paid")]
        public async Task<IActionResult> MarkPaid(int id, [FromBody] MarkSettlementPaidRequest request, CancellationToken ct)
        {
            try
            {
                return await _settlements.MarkPaidAsync(id, request, Me ?? 0, ct) is { } dto ? Ok(dto) : NotFound();
            }
            catch (SettlementConflictException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpGet("{id:int}/Export")]
        public async Task<IActionResult> Export(int id, [FromQuery] string format, [FromQuery] string? language,
            [FromServices] SettlementDocuments documents, [FromServices] IOptionsMonitor<CompanyInformations> company, CancellationToken ct)
        {
            if (format is not ("csv" or "pdf"))
                return BadRequest(new { error = "format must be csv or pdf" });
            var detail = await _settlements.DetailAsync(id, null, ct);
            if (detail == null)
                return NotFound();
            var name = SettlementDocuments.FileName(detail.Settlement);
            return format == "csv"
                ? File(documents.Csv(detail), "text/csv; charset=utf-8", name + ".csv")
                : File(documents.Pdf(detail, company.CurrentValue, language ?? BillingTexts.French), "application/pdf", name + ".pdf");
        }
    }
}
