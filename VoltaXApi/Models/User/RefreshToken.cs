using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    /// <summary>
    /// One issued refresh token. The raw value never reaches the database: only a
    /// SHA-256 hash of it is stored, so a dump of this table cannot be replayed
    /// against the API.
    ///
    /// Tokens are rotated on every use — the row being spent is marked revoked and
    /// points at its successor through <see cref="ReplacedByTokenHash"/>. That chain
    /// is what lets <c>RefreshTokenService</c> detect a replay of an already spent
    /// token and cut the whole family off.
    /// </summary>
    public class RefreshToken : IEntity
    {
        public int ID { get; set; }

        /// <summary>SHA-256 of the raw token, hex encoded.</summary>
        [MaxLength(128)]
        public string TokenHash { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }

        public DateTime ExpiresAt { get; set; }

        /// <summary>Set the moment the token is spent, revoked or replaced.</summary>
        public DateTime? RevokedAt { get; set; }

        [MaxLength(128)]
        public string? ReplacedByTokenHash { get; set; }

        /// <summary>Why the token stopped being usable — "rotated", "logout", "reuse", "password-change".</summary>
        [MaxLength(64)]
        public string? RevokedReason { get; set; }

        [MaxLength(64)]
        public string? CreatedByIp { get; set; }

        [MaxLength(256)]
        public string? UserAgent { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
