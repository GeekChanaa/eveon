using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.Services.Billing;

namespace VoltaXApi.Controllers.SelfService
{
    /// <summary>The caller's own invoices and credit notes (newest first) and their stored PDFs.</summary>
    [ApiController]
    [Route("api/my/invoices")]
    [SelfService]
    public sealed class MyInvoicesController : ControllerBase
    {
        private readonly InvoiceQueries _invoices;

        public MyInvoicesController(InvoiceQueries invoices)
        {
            _invoices = invoices;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] InvoiceFilter filter, CancellationToken ct) =>
            Ok(await _invoices.SearchAsync(filter, this.User().ID, ct));

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> Pdf(int id, CancellationToken ct) =>
            await _invoices.FileAsync(id, this.User().ID, ct) is { } file ? File(file.Content, file.ContentType, file.FileName) : NotFound();
    }
}
