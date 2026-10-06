using System.ComponentModel.DataAnnotations;

using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class CreatePartnerDto
{
    [Required, StringLength(150)]
    public string Name { get; set; }
    [StringLength(2000)]
    public string Description { get; set; }
    [EnumDataType(typeof(PartnerTypeEnum))]
    public PartnerTypeEnum Type { get; set; }
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; }
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Enter a valid email address.")]
    [StringLength(254)]
    public string? Email2 { get; set; }
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Enter a valid email address.")]
    [StringLength(254)]
    public string? Email3 { get; set; }
    [Required]
    [RegularExpression(@"^\+?[0-9][0-9 ()\-]{5,19}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; }
    [RegularExpression(@"^\+?[0-9][0-9 ()\-]{5,19}$", ErrorMessage = "Enter a valid phone number.")]
    public string? Phone2 { get; set; }
    [RegularExpression(@"^\+?[0-9][0-9 ()\-]{5,19}$", ErrorMessage = "Enter a valid phone number.")]
    public string? Phone3 { get; set; }
    [StringLength(100)]
    public string? City { get; set; }
    [StringLength(100)]
    public string? Country { get; set; }
    [StringLength(300)]
    public string? Address { get; set; }
    [StringLength(50)]
    public string? TaxIdentificationNumber { get; set; }
    [StringLength(50)]
    public string? RegistrationNumber { get; set; }
    [StringLength(34)]
    [RegularExpression(@"^[A-Za-z0-9 ]*$", ErrorMessage = "Bank account number may contain only letters, digits and spaces.")]
    public string? BankAccountNumber { get; set; }
    [StringLength(500)]
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "Logo URL must be an http(s) URL.")]
    public string? LogoUrl { get; set; }

}