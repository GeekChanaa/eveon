using System.ComponentModel.DataAnnotations;
using System;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UserDashboardEditInformationsDto
    {
        [StringLength(64)]
        public string? FirstName { get; set; }
        [StringLength(64)]
        public string? LastName { get; set; }
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Enter a valid email address.")]
        [StringLength(254)]
        public string? Email { get; set; }
        [RegularExpression(@"^\+?[0-9][0-9 ()\-]{5,19}$", ErrorMessage = "Enter a valid phone number.")]
        public string? Phone { get; set; }
        [Range(0, int.MaxValue)]
        public int RoleID { get; set; }
        public int? PartnerID { get; set; }
        public int? ElectricVehicleModelID { get; set; }
        public DateTime? Birthday { get; set; }
        [StringLength(20)]
        public string? Gender { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneNumberVerified { get; set; }
        public DateTime? SuspendedAt { get; set; }
        [StringLength(500)]
        public string? SuspensionReason { get; set; }

    }
}