using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Models
{
    /// <summary>
    /// A wallet top-up. Online top-ups are created Pending and only become Completed (and credit the card) when
    /// the payment provider confirms the payment in a verified callback; manual ones are credited by an operator.
    /// </summary>
    public class Order : IEntity
    {
        public int ID { get; set; }
        [NotUpdatable]
        public int CardID { get; set; }
        [NotUpdatable]
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        [NotUpdatable]
        public RechargeOrderStatus Status { get; set; }
        public DateTime RechargeDate { get; set; }
        /// <summary>Payment provider (Sandbox, Cmi, Stripe) or Manual for an operator credit.</summary>
        [NotUpdatable, MaxLength(32)]
        public string? Provider { get; set; }
        /// <summary>The provider's id of the payment (checkout session, transaction id).</summary>
        [NotUpdatable, MaxLength(128)]
        public string? ProviderReference { get; set; }
        [NotUpdatable]
        public DateTime? PaidAt { get; set; }
        [NotUpdatable, MaxLength(256)]
        public string? FailureReason { get; set; }
        /// <summary>The operator who credited a manual top-up.</summary>
        [NotUpdatable]
        public int? CreatedByUserID { get; set; }
        [MaxLength(256)]
        public string? Note { get; set; }
        public Card? Card { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
