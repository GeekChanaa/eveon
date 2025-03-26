using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UserDashboardCreateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? Car { get; set; }
        public DateTime? Birthday { get; set; }
        public string Phone { get; set; }
        public string? Password { get; set; }
        public int? PartnerID { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneNumberVerified { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
    }
}