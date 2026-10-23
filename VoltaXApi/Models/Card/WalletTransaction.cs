using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Models
{
    public enum WalletTransactionType
    {
        TopUp,
        SessionCharge,
        IdleFee,
        Refund,
        Adjustment
    }

    /// <summary>
    /// One immutable movement of a charging card balance (the card is the wallet). Credits are positive, debits
    /// negative; for every card the movements add up to <see cref="Card.Balance"/>. <see cref="Reference"/> is
    /// unique: posting the same business event twice (a retried OCPP message, a replayed payment callback)
    /// returns the first movement instead of moving money again.
    /// </summary>
    public class WalletTransaction
    {
        public int ID { get; set; }
        public int CardID { get; set; }
        public Card? Card { get; set; }
        public WalletTransactionType Type { get; set; }
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        [Precision(18, 2)]
        public decimal BalanceAfter { get; set; }
        public int? ChargingSessionID { get; set; }
        public int? OrderID { get; set; }
        [MaxLength(100)]
        public string Reference { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }
        public int? CreatedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
