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
        public DateTime Birthday { get; set; }
        public string? Phone { get; set; }
    
        // Salting the hashed password is one of the most secured ways to secure passwords
        public  byte[] PasswordHash { get; set; }
        public  byte[] PasswordSalt { get; set; }
        [NotMapped]
        public string? Password { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }

        public bool IsPhoneNumberVerified { get; set; } = false;
        public string? PhoneVerificationToken { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public string? ResetPasswordToken  { get; set; }
        public IEnumerable<Order>? Orders { get; set; }
        public ICollection<DebitCard>? DebitCards { get; set; }
        public ICollection<Card>? Cards { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
    }
}