

using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class UserListDto
{
    public int ID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string? Gender { get; set; }
    public string? City { get; set; }
    public string? Car { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Phone { get; set; }
    public string? PartnerName { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneNumberVerified { get; set; } = false;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string SuspendedAt { get; set; }
    public string? ImageUrl { get; set; }
    
    public string FullName { get { return FirstName+ " " + LastName;} }
}