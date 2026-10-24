using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Dtos
{
  public class UpdateConnectorPricingDto
  {
    [Range(0, 10000)]
    public decimal PricePerKWh { get; set; }   
    [Range(0, 10000)]
    public decimal PricePerIdleMinute { get; set; }   
    [Range(0, 10000)]
    public decimal CostPerKwh { get; set; }
  }
}