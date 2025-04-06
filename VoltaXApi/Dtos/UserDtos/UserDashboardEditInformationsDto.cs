using System;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UserDashboardEditInformationsDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Gender { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneNumberVerified { get; set; }
        public DateTime? SuspendedAt { get; set; }

    }
}