using Microsoft.AspNetCore.Mvc;
using VoltaxApi.Dtos;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.Services;
using VoltaXApi.Services.CustomerPortal;

namespace VoltaXApi.Controllers.SelfService
{
    /// <summary>
    /// The customer portal: the caller's own profile, cards, charging sessions, wallet movements and top-ups,
    /// read only (paying and saving cards go through api/payments). Every query is scoped to the caller.
    /// </summary>
    [ApiController]
    [Route("api/my")]
    [SelfService]
    public sealed class MyController : ControllerBase
    {
        private readonly CustomerPortalService _portal;

        public MyController(CustomerPortalService portal)
        {
            _portal = portal;
        }

        private int Me => this.User().ID;

        [HttpGet("overview")]
        public async Task<IActionResult> Overview(CancellationToken ct) =>
            await _portal.OverviewAsync(Me, ct) is { } overview ? Ok(overview) : NotFound();

        [HttpGet("profile")]
        public async Task<IActionResult> Profile(CancellationToken ct) =>
            await _portal.ProfileAsync(Me, ct) is { } profile ? Ok(profile) : NotFound();

        [HttpGet("cards")]
        public async Task<IActionResult> Cards(CancellationToken ct) => Ok(await _portal.CardsAsync(Me, ct));

        [HttpGet("sessions")]
        public async Task<IActionResult> Sessions([FromQuery] MySessionFilter filter, CancellationToken ct) =>
            Ok(await _portal.SessionsAsync(Me, filter, ct));

        [HttpGet("sessions/active")]
        public async Task<IActionResult> ActiveSession(CancellationToken ct) =>
            await _portal.ActiveSessionAsync(Me, ct) is { } active ? Ok(active) : NoContent();

        [HttpGet("sessions/{id:int}")]
        public async Task<IActionResult> Session(int id, CancellationToken ct) =>
            await _portal.SessionAsync(Me, id, ct) is { } session ? Ok(session) : NotFound();

        /// <summary>The legal invoice of one ended session (PDF, stored at issue).</summary>
        [HttpGet("sessions/{id:int}/invoice")]
        public async Task<IActionResult> SessionInvoice(int id, [FromServices] VoltaXApi.Services.Billing.IInvoiceService invoices, CancellationToken ct)
        {
            if (!await _portal.OwnsSessionAsync(Me, id, ct))
                return NotFound();
            var invoice = await invoices.IssueForSessionAsync(id, ct);
            var file = invoice == null ? null : await invoices.GetFileAsync(invoice.ID, ct);
            return file == null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>The filtered sessions as a CSV file (spreadsheets) or a PDF statement (printing).</summary>
        [HttpGet("sessions/export")]
        public async Task<IActionResult> Export([FromQuery] MySessionFilter filter, [FromQuery] string format,
            [FromServices] SessionStatementExporter exporter, [FromServices] GlobalConfigurations pricing, CancellationToken ct)
        {
            var sessions = await _portal.ExportAsync(Me, filter, ct);
            var name = $"eveon-sessions-{DateTime.UtcNow:yyyyMMdd}";
            return format?.ToLowerInvariant() switch
            {
                "csv" => File(exporter.Csv(sessions), "text/csv; charset=utf-8", name + ".csv"),
                "pdf" => File(exporter.Pdf($"{this.User().FirstName} {this.User().LastName}".Trim(), filter.From, filter.To, sessions, pricing.Vat),
                    "application/pdf", name + ".pdf"),
                _ => BadRequest(new { error = "format must be csv or pdf" })
            };
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> Wallet([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await _portal.WalletAsync(Me, page, pageSize, ct));

        [HttpGet("orders")]
        public async Task<IActionResult> Orders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await _portal.OrdersAsync(Me, page, pageSize, ct));

        /// <summary>The legal invoice of a paid (possibly since refunded) top-up (PDF, stored at issue).</summary>
        [HttpGet("orders/{id:int}/invoice")]
        public async Task<IActionResult> OrderInvoice(int id, [FromServices] VoltaXApi.Services.Billing.IInvoiceService invoices,
            [FromServices] VoltaXApi.Services.Billing.InvoiceQueries mine, CancellationToken ct)
        {
            if (!await mine.OwnsOrderAsync(Me, id, ct))
                return NotFound();
            var invoice = await invoices.IssueForOrderAsync(id, ct);
            var file = invoice == null ? null : await invoices.GetFileAsync(invoice.ID, ct);
            return file == null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
        }
    }
}
