using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Models.Billing
{
    public enum SettlementStatus
    {
        /// <summary>Computed, not paid yet: it can be regenerated.</summary>
        Pending,
        /// <summary>Paid out to the partner (date and reference set by an operator): frozen.</summary>
        Paid
    }

    /// <summary>
    /// What EVEON owes a partner for one month: the sessions billed on the partner's stations (and refunds of
    /// them), minus EVEON's commission and the VAT on it. The totals are the sums of the <see cref="Lines"/>; a
    /// session or refund is in at most one settlement (unique line source key).
    /// </summary>
    public class PartnerSettlement
    {
        public int ID { get; set; }
        public int PartnerID { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        /// <summary>UTC instants of the business month's boundaries.</summary>
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public int SessionCount { get; set; }
        [Precision(18, 3)]
        public decimal EnergyKwh { get; set; }
        /// <summary>Billed to drivers, VAT included.</summary>
        [Precision(18, 2)]
        public decimal GrossAmount { get; set; }
        [Precision(18, 2)]
        public decimal NetExclVat { get; set; }
        [Precision(18, 2)]
        public decimal RevenueVat { get; set; }
        [Precision(18, 2)]
        public decimal CommissionExclVat { get; set; }
        [Precision(18, 2)]
        public decimal CommissionVat { get; set; }
        /// <summary>Gross − commission − VAT on the commission: what is paid to the partner.</summary>
        [Precision(18, 2)]
        public decimal NetPayout { get; set; }

        /// <summary>Terms applied (copied from the partner when computed).</summary>
        [Precision(5, 4)]
        public decimal CommissionRate { get; set; }
        [Precision(18, 4)]
        public decimal CommissionPerKwh { get; set; }
        /// <summary>VAT rate applied to the commission.</summary>
        [Precision(5, 4)]
        public decimal VatRate { get; set; }

        public SettlementStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        [MaxLength(128)]
        public string? PaymentReference { get; set; }
        public int? PaidByUserID { get; set; }
        public DateTime GeneratedAt { get; set; }
        /// <summary>The operator who generated it on demand; null for the monthly job.</summary>
        public int? GeneratedByUserID { get; set; }

        public List<PartnerSettlementLine> Lines { get; set; } = new();
    }

    /// <summary>One session (or refund of a session, negative) of a settlement, with its share of each total.</summary>
    public class PartnerSettlementLine
    {
        public int ID { get; set; }
        public int SettlementID { get; set; }
        /// <summary>session:{id} or refund:{id}; unique, so nothing is settled twice.</summary>
        [MaxLength(64)]
        public string SourceKey { get; set; } = "";
        public int? ChargingSessionID { get; set; }
        public int? RefundID { get; set; }
        /// <summary>Session start, or refund time.</summary>
        public DateTime Date { get; set; }
        [MaxLength(128)]
        public string? Station { get; set; }
        [MaxLength(64)]
        public string? ChargePoint { get; set; }
        [Precision(18, 3)]
        public decimal Kwh { get; set; }
        [Precision(18, 2)]
        public decimal Gross { get; set; }
        [Precision(18, 2)]
        public decimal NetExclVat { get; set; }
        [Precision(18, 2)]
        public decimal Vat { get; set; }
        [Precision(18, 2)]
        public decimal CommissionExclVat { get; set; }
        [Precision(18, 2)]
        public decimal CommissionVat { get; set; }
        [Precision(18, 2)]
        public decimal NetPayout { get; set; }
    }
}
