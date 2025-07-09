using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

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
        public string? Car { get; set; }
        public DateTime? Birthday { get; set; }
        private string? _phoneNumber;
        public string? Phone
        {
            get => _phoneNumber;
            set => _phoneNumber = NormalizePhoneNumber(value);
        }

        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            // Remove spaces and non-digit characters except "+"
            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            // Remove leading 0 or +212 or 212
            if (phone.StartsWith("+212"))
                phone = phone.Substring(4);
            else if (phone.StartsWith("212"))
                phone = phone.Substring(3);
            else if (phone.StartsWith("0"))
                phone = phone.Substring(1);

            return $"+212{phone}";
        }
    
        public byte[] PasswordHash { get; set; }
        public  byte[] PasswordSalt { get; set; }
        [NotMapped]
        public string? Password { get; set; }
        public int? PartnerID { get; set; }
        public int? ImageID { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public bool IsPhoneNumberVerified { get; set; } = false;
        public string? PhoneVerificationToken { get; set; }
        public int RoleID { get; set; }
        public Role Role { get; set; }
        public string? ResetPasswordToken  { get; set; }
        public IEnumerable<Order>? Orders { get; set; }
        public ICollection<DebitCard>? DebitCards { get; set; }
        public ICollection<Card>? Cards { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? SuspendedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Image? Image { get; set; }
        [NotMapped]
        public string FullName { get { return FirstName+ " " + LastName;} }
        public Partner? Partner { get; set; }
    }
}