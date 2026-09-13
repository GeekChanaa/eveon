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
        public string? ResetPasswordCode { get; set; }
        public DateTime? ResetPasswordCodeExpiresAt { get; set; }
        public int RoleID { get; set; }
        public Role Role { get; set; }
        public string? ResetPasswordToken  { get; set; }
        public IEnumerable<Order>? Orders { get; set; }
        public ICollection<DebitCard>? DebitCards { get; set; }
        public ICollection<Card>? Cards { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? SuspendedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Image? Image { get; set; }
        public ElectricVehicleModel? ElectricVehicleModel { get; set; }
        [NotMapped]
        public string FullName { get { return FirstName+ " " + LastName;} }
        public Partner? Partner { get; set; }
    }
}