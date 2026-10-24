using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Services.Billing
{
    public sealed record InvoiceDto(int Id, InvoiceKind Kind, string Number, DateTime IssuedAt, string CustomerName, decimal AmountExclVat,
        decimal VatAmount, decimal AmountInclVat, decimal VatRate, string Currency, string Language, int? ChargingSessionId, int? OrderId,
        int? RefundId, string? CreditedInvoiceNumber, string? Description);

    public sealed class InvoiceFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public InvoiceKind? Kind { get; set; }
        /// <summary>Number, customer name or e-mail (contains).</summary>
        public string? Search { get; set; }
        public int? ChargingSessionId { get; set; }
        public int? OrderId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    /// <summary>Reads issued invoices and credit notes: the customer's own documents and the operator's register.</summary>
    public sealed class InvoiceQueries
    {
        private const int MaxPageSize = 100;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;

        public InvoiceQueries(VoltaXApiDbContext db, IBusinessClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public Task<bool> OwnsOrderAsync(int userId, int orderId, CancellationToken ct = default) =>
            _db.Orders.AsNoTracking().AnyAsync(o => o.ID == orderId && o.Card!.UserID == userId, ct);

        public async Task<PagedResult<InvoiceDto>> SearchAsync(InvoiceFilter filter, int? userId = null, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
            var query = Filtered(filter, userId);
            var total = await query.CountAsync(ct);
            var rows = await Project(query.OrderByDescending(i => i.IssuedAt).ThenByDescending(i => i.ID).Skip((page - 1) * pageSize).Take(pageSize))
                .ToListAsync(ct);
            return new PagedResult<InvoiceDto>(rows, page, pageSize, total);
        }

        /// <summary>The PDF of one document, only when it belongs to <paramref name="userId"/> (null: any document).</summary>
        public Task<InvoiceFile?> FileAsync(int invoiceId, int? userId = null, CancellationToken ct = default) =>
            _db.InvoiceFiles.AsNoTracking()
                .Where(f => f.InvoiceID == invoiceId && (userId == null || f.Invoice!.UserID == userId))
                .FirstOrDefaultAsync(ct);

        public Task<List<InvoiceDto>> ForSessionAsync(int sessionId, CancellationToken ct = default) =>
            Project(_db.Invoices.AsNoTracking().Where(i => i.ChargingSessionID == sessionId).OrderBy(i => i.ID)).ToListAsync(ct);

        public Task<List<InvoiceDto>> ForOrderAsync(int orderId, CancellationToken ct = default) =>
            Project(_db.Invoices.AsNoTracking().Where(i => i.OrderID == orderId).OrderBy(i => i.ID)).ToListAsync(ct);

        /// <summary>
        /// The invoice journal for accounting: one row per document, credit notes negative, semicolon separated,
        /// UTF-8 with BOM (opens as is in French / Arabic spreadsheets).
        /// </summary>
        public async Task<byte[]> JournalCsvAsync(InvoiceFilter filter, CancellationToken ct = default)
        {
            var rows = await Project(Filtered(filter, null).OrderBy(i => i.Series).ThenBy(i => i.Year).ThenBy(i => i.Sequence)).Take(50_000).ToListAsync(ct);
            var csv = new StringBuilder();
            csv.AppendLine("Number;Type;Issued;Customer;Excl. VAT (MAD);VAT rate;VAT (MAD);Incl. VAT (MAD);Credited invoice;Session;Order");
            foreach (var r in rows)
            {
                var sign = r.Kind == InvoiceKind.CreditNote ? -1 : 1;
                csv.AppendLine(string.Join(";",
                    r.Number, r.Kind == InvoiceKind.CreditNote ? "Credit note" : "Invoice",
                    _clock.ToLocal(r.IssuedAt).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    CsvField.Of(r.CustomerName),
                    (sign * r.AmountExclVat).ToString("0.00", CultureInfo.InvariantCulture),
                    r.VatRate.ToString("0.####", CultureInfo.InvariantCulture),
                    (sign * r.VatAmount).ToString("0.00", CultureInfo.InvariantCulture),
                    (sign * r.AmountInclVat).ToString("0.00", CultureInfo.InvariantCulture),
                    r.CreditedInvoiceNumber ?? "",
                    r.ChargingSessionId?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.OrderId?.ToString(CultureInfo.InvariantCulture) ?? ""));
            }
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        }

        private IQueryable<Invoice> Filtered(InvoiceFilter filter, int? userId)
        {
            var query = _db.Invoices.AsNoTracking();
            if (userId.HasValue)
                query = query.Where(i => i.UserID == userId);
            if (filter.From.HasValue)
            {
                var from = _clock.StartOfDayUtc(filter.From.Value.Date);
                query = query.Where(i => i.IssuedAt >= from);
            }
            if (filter.To.HasValue)
            {
                var to = _clock.StartOfDayUtc(filter.To.Value.Date.AddDays(1));
                query = query.Where(i => i.IssuedAt < to);
            }
            if (filter.Kind.HasValue)
                query = query.Where(i => i.Kind == filter.Kind);
            if (filter.ChargingSessionId.HasValue)
                query = query.Where(i => i.ChargingSessionID == filter.ChargingSessionId);
            if (filter.OrderId.HasValue)
                query = query.Where(i => i.OrderID == filter.OrderId);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(i => i.Number.Contains(term) || i.CustomerName.Contains(term) || (i.CustomerEmail != null && i.CustomerEmail.Contains(term)));
            }
            return query;
        }

        private IQueryable<InvoiceDto> Project(IQueryable<Invoice> query) =>
            query.Select(i => new InvoiceDto(i.ID, i.Kind, i.Number, i.IssuedAt, i.CustomerName, i.AmountExclVat, i.VatAmount, i.AmountInclVat,
                i.VatRate, i.Currency, i.Language, i.ChargingSessionID, i.OrderID, i.RefundID,
                _db.Invoices.Where(c => c.ID == i.CreditedInvoiceID).Select(c => c.Number).FirstOrDefault(), i.Description));
    }
}
