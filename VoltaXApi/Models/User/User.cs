using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using VoltaXApi.Helpers;

namespace VoltaXApi.Models
{
    public class User  : IEntity
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public int? ElectricVehicleModelID { get; set; }
        public DateTime? Birthday { get; set; }
        private string? _phoneNumber;
        public string? Phone
        {
            get => _phoneNumber;
            set => _phoneNumber = PhoneHelper.Normalize(value);
        }


        public byte[]? PasswordHash { get; set; }
        public  byte[]? PasswordSalt { get; set; }
        [NotMapped]
        public string? Password { get; set; }

        // External (OAuth) identity
        [MaxLength(256)]
        public string? GoogleId { get; set; }
        [MaxLength(32)]
        public AuthProviderEnum AuthProvider { get; set; } = AuthProviderEnum.Local;
        [MaxLength(512)]
        public string? ExternalPictureUrl { get; set; }
        [NotMapped]
        public bool HasPassword => PasswordHash != null && PasswordSalt != null;
        public int? PartnerID { get; set; }
        public int? ImageID { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public bool IsPhoneNumberVerified { get; set; } = false;
        public string? PhoneVerificationToken { get; set; }
        // Keyed digest of the 6 digit mobile reset code, never the code itself
        public string? ResetPasswordCode { get; set; }
        public DateTime? ResetPasswordCodeExpiresAt { get; set; }
        public int ResetPasswordCodeAttempts { get; set; }
        public int RoleID { get; set; }
        public Role Role { get; set; }
        // SHA-256 of the single use reset token, never the token itself
        public string? ResetPasswordToken  { get; set; }
        public DateTime? ResetPasswordTokenExpiresAt { get; set; }

        // TOTP two factor authentication (admin and partner accounts)
        public bool TwoFactorEnabled { get; set; }
        // Base32 secret protected with ASP.NET Data Protection; set (pending) before it is confirmed
        [MaxLength(512)]
        public string? TwoFactorSecret { get; set; }
        // Semicolon separated SHA-256 hashes of the unused recovery codes
        [MaxLength(1024)]
        public string? TwoFactorRecoveryCodes { get; set; }
        // Last accepted TOTP time step, so a code cannot be replayed
        public long? TwoFactorLastUsedStep { get; set; }
        public IEnumerable<Order>? Orders { get; set; }
        public ICollection<DebitCard>? DebitCards { get; set; }
        public ICollection<Card>? Cards { get; set; }
        public bool IsDeleted { get; set; } = false;
        // Suspension end date: the account is suspended while SuspendedAt is in the future
        // (the dashboard edits it as "suspension end date"). Use IsSuspended / NotSuspendedAt everywhere.
        public DateTime? SuspendedAt { get; set; }
        // A pending or completed GDPR deletion also blocks the account, even if an admin edits
        // SuspendedAt by hand; only AccountDeletionService.Cancel lifts it.
        public bool IsSuspended(DateTime utcNow) =>
            SuspendedAt != null && SuspendedAt > utcNow || DeletionRequestedAt != null || DeletedAt != null;
        [NotMapped]
        public bool IsCurrentlySuspended => IsSuspended(DateTime.UtcNow);
        public static System.Linq.Expressions.Expression<Func<User, bool>> NotSuspendedAt(DateTime utcNow) =>
            u => (u.SuspendedAt == null || u.SuspendedAt <= utcNow) && u.DeletionRequestedAt == null && u.DeletedAt == null;
        public DateTime CreatedAt { get; set; }
        public string? SuspensionReason { get; set; }
        // GDPR: set when the user asks for deletion (login is disabled from then on),
        // and DeletedAt once the grace period ran out and the PII was anonymized.
        public DateTime? DeletionRequestedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
        [MaxLength(32)]
        public string? TermsVersion { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Image? Image { get; set; }
        public ElectricVehicleModel? ElectricVehicleModel { get; set; }
        [NotMapped]
        public string FullName { get { return FirstName+ " " + LastName;} }
        public Partner? Partner { get; set; }
    }
}