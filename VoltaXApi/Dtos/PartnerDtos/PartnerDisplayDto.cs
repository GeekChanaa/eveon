

using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class PartnerDisplayDto
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PartnerTypeEnum Type { get; set; }
    public string Email { get; set; }
    public string? Email2 { get; set; }
    public string? Email3 { get; set; }
    public string Phone { get; set; }
    public string? Phone2 { get; set; }
    public string? Phone3 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? LogoUrl { get; set; }
    public ICollection<ChargingStation>? ChargingStations { get; set; }
    public ICollection<User>? Users { get; set; }
}