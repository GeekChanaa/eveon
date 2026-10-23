using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Models.Billing
{
    public enum InvoiceKind
    {
        Invoice,
        CreditNote
    }

    /// <summary>
    /// A legal invoice or credit note, issued once and never changed. <see cref="Number"/> (EV-2026-000123 for
    /// invoices, AV-2026-000001 for credit notes) comes from a gap-free sequence per series and year, assigned in
    /// the same transaction as this row, so a failed issue leaves no hole. <see cref="SourceReference"/> names the
    /// billed event (session:12, session:12:idle, order:5, refund:3) and is unique: issuing twice returns the
    /// first document. The PDF rendered at issue time is kept in <see cref="InvoiceFile"/>.
    /// </summary>
    public class Invoice
    {
        public int ID { get; set; }
        public InvoiceKind Kind { get; set; }
        [MaxLength(32)]
        public string Number { get; set; } = "";
        /// <summary>Series prefix (EV, AV) the number was taken from.</summary>
        [MaxLength(8)]
        public string Series { get; set; } = "";
        /// <summary>Business-time-zone year of issue: the numbering restarts at 1 every year.</summary>
        public int Year { get; set; }
        public int Sequence { get; set; }
        [MaxLength(64)]
        public string SourceReference { get; set; } = "";
        public DateTime IssuedAt { get; set; }

        public int? ChargingSessionID { get; set; }
        public int? OrderID { get; set; }
        public int? RefundID { get; set; }
        /// <summary>For a credit note: the invoice it corrects.</summary>
        public int? CreditedInvoiceID { get; set; }

        public int? UserID { get; set; }
        public int? CardID { get; set; }
        [MaxLength(200)]
        public string CustomerName { get; set; } = "";
        [MaxLength(256)]
        public string? CustomerEmail { get; set; }
        /// <summary>Language of the PDF (fr, en, ar).</summary>
        [MaxLength(8)]
        public string Language { get; set; } = "fr";
        [MaxLength(3)]
        public string Currency { get; set; } = "MAD";

        [Precision(5, 4)]
        public decimal VatRate { get; set; }
        [Precision(18, 2)]
        public decimal AmountExclVat { get; set; }
        [Precision(18, 2)]
        public decimal VatAmount { get; set; }
        [Precision(18, 2)]
        public decimal AmountInclVat { get; set; }
        [MaxLength(256)]
        public string? Description { get; set; }

        public InvoiceFile? File { get; set; }
    }

    /// <summary>The PDF of an issued invoice, as rendered at issue time (kept apart so lists never load it).</summary>
    public class InvoiceFile
    {
        public int InvoiceID { get; set; }
        public Invoice? Invoice { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
        [MaxLength(64)]
        public string ContentType { get; set; } = "application/pdf";
        [MaxLength(128)]
        public string FileName { get; set; } = "";
    }

    /// <summary>
    /// Last number used in a series for a year. <see cref="LastNumber"/> is the concurrency token: two issuers
    /// taking the same number make one of them retry, and the increment commits with the invoice.
    /// </summary>
    public class InvoiceSequence
    {
        [MaxLength(8)]
        public string Series { get; set; } = "";
        public int Year { get; set; }
        [ConcurrencyCheck]
        public int LastNumber { get; set; }
    }
}
