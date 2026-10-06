using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    // Append-only: deliberately not an IEntity, so it has no soft delete and no UpdatedAt.
    public class AuditLog
    {
        public long ID { get; set; }
        public DateTime OccurredAt { get; set; }
        public int? UserID { get; set; }
        [MaxLength(256)]
        public string? UserEmail { get; set; }
        [MaxLength(64)]
        public string? Role { get; set; }
        [MaxLength(64)]
        public string Action { get; set; } = null!;
        [MaxLength(128)]
        public string? EntityType { get; set; }
        [MaxLength(64)]
        public string? EntityID { get; set; }
        public string? ChangesJson { get; set; }
        [MaxLength(64)]
        public string? IpAddress { get; set; }
        [MaxLength(64)]
        public string? CorrelationID { get; set; }
    }
}
