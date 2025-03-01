using System;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UserDashboardDisplayInformationsDto
    {
        public int ID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public DateTime Birthday { get; set; }
        public string? Gender { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsPhoneNumberVerified { get; set; } = false;
        public DateTime SuspendedAt { get; set; }

    }
}