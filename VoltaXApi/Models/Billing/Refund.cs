using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Models.Billing
{
    /// <summary>What is refunded: a paid top-up (money back to the customer) or a billed charging session.</summary>
    public enum RefundTarget
    {
        Order,
        Session
    }

    /// <summary>Where the money goes back.</summary>
    public enum RefundMethod
    {
        /// <summary>To the payment method of a top-up, through the payment provider; the wallet is debited.</summary>
        Gateway,
        /// <summary>To the EVEON wallet (session refunds).</summary>
        Wallet,
        /// <summary>Outside the platform (cash, bank transfer, the provider's back office); the wallet is debited.</summary>
        External
    }

    public enum RefundStatus
    {
        Pending,
        Succeeded,
        Failed
    }

    /// <summary>
    /// A refund decided by an operator. <see cref="IdempotencyKey"/> is unique: the same request sent twice
    /// refunds once. Pending and succeeded refunds of an order or session never add up to more than was paid.
    /// The wallet movement is <c>refund:{IdempotencyKey}</c>; a succeeded refund gets a credit note.
    /// </summary>
    public class Refund
    {
        public int ID { get; set; }
        public RefundTarget Target { get; set; }
        public int? OrderID { get; set; }
        public int? ChargingSessionID { get; set; }
        public int CardID { get; set; }
        public int? UserID { get; set; }
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        [MaxLength(256)]
        public string Reason { get; set; } = "";
        public RefundMethod Method { get; set; }
        public RefundStatus Status { get; set; }
        [MaxLength(32)]
        public string? Provider { get; set; }
        [MaxLength(128)]
        public string? ProviderReference { get; set; }
        [MaxLength(32)]
        public string? ProviderStatus { get; set; }
        [MaxLength(256)]
        public string? FailureReason { get; set; }
        [MaxLength(64)]
        public string IdempotencyKey { get; set; } = "";
        public int RequestedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CreditNoteID { get; set; }
    }
}
