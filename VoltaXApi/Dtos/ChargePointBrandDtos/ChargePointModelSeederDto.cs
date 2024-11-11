
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
  public class ChargePointModelSeederDto
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public string Identifier { get; set; }
    public List<double> SupportedKwhs { get; set; }
    public int ConnectorCount { get; set; }
    public List<ChargePointIntegration>? Integrations { get; set; }
    public string? ImageUrl { get; set; }
    public ChargePointFeatures? Features { get; set; }
    public int? ChargePointBrandID { get; set; }
    public ChargePointBrand? ChargePointBrand { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}