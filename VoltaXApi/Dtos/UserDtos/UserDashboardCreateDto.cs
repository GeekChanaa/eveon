using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using VoltaXApi.Models;
using VoltaXApi.Attributes;

namespace VoltaXApi.Dtos
{
    public class UserDashboardCreateDto
    {
        [Required, StringLength(64)]
        public string FirstName { get; set; }
        [Required, StringLength(64)]
        public string LastName { get; set; }
        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; }
        [StringLength(20)]
        public string? Gender { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        public DateTime? Birthday { get; set; }
        [Required]
        [RegularExpression(@"^\+?[0-9][0-9 ()\-]{5,19}$", ErrorMessage = "Enter a valid phone number.")]
        public string Phone { get; set; }
        [StrongPassword]
        public string Password { get; set; }
        public int? PartnerID { get; set; }
        public bool IsEmailVerified { get; set; }
        public int? ElectricVehicleModelID { get; set; }
        public bool IsPhoneNumberVerified { get; set; }
        [Range(1, int.MaxValue)]
        public int RoleID { get; set; }
    }
}