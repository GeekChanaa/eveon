using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Services.Billing;

namespace VoltaXApi.Controllers
{
    /// <summary>
    /// The register of issued invoices and credit notes (permission ViewRechargeOrders): search, stored PDFs and
    /// the CSV journal for accounting. Documents are never edited; a correction is a credit note.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class InvoiceController : ControllerBase
    {
        private readonly InvoiceQueries _invoices;

        public InvoiceController(InvoiceQueries invoices)
        {
            _invoices = invoices;
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] InvoiceFilter filter, CancellationToken ct) =>
            Ok(await _invoices.SearchAsync(filter, null, ct));

        [HttpGet("{id:int}/Pdf")]
        public async Task<IActionResult> Pdf(int id, CancellationToken ct) =>
            await _invoices.FileAsync(id, null, ct) is { } file ? File(file.Content, file.ContentType, file.FileName) : NotFound();

        [HttpGet("Journal")]
        public async Task<IActionResult> Journal([FromQuery] InvoiceFilter filter, CancellationToken ct) =>
            File(await _invoices.JournalCsvAsync(filter, ct), "text/csv; charset=utf-8", $"eveon-invoices-{DateTime.UtcNow:yyyyMMdd}.csv");

        [HttpGet("Session/{chargingSessionID:int}")]
        public async Task<IActionResult> ForSession(int chargingSessionID, CancellationToken ct) =>
            Ok(await _invoices.ForSessionAsync(chargingSessionID, ct));

        [HttpGet("Order/{orderID:int}")]
        public async Task<IActionResult> ForOrder(int orderID, CancellationToken ct) =>
            Ok(await _invoices.ForOrderAsync(orderID, ct));
    }
}
