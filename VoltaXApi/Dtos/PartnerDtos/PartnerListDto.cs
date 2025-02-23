using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class PartnerListDto
{
    public int ID { get; set; }
    public string? Name { get; set; }
    public PartnerTypeEnum Type { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
}