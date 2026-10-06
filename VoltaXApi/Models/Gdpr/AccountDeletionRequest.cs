using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public enum AccountDeletionStatusEnum
    {
        Pending,
        Cancelled,
        Completed
    }

    public class AccountDeletionRequest : IEntity
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public User? User { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime ScheduledFor { get; set; }
        public AccountDeletionStatusEnum Status { get; set; } = AccountDeletionStatusEnum.Pending;
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int? CancelledByUserID { get; set; }
        // Charging card balance at request time; ops refund it before or after anonymization.
        public double WalletBalance { get; set; }
        public bool RefundRequired { get; set; }
        // Cards blocked by the request, restored if it is cancelled.
        [MaxLength(1024)]
        public string? BlockedCardIDs { get; set; }
        [MaxLength(64)]
        public string? RequestedFromIp { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
