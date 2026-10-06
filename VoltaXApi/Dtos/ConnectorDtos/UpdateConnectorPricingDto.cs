using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Dtos
{
  public class UpdateConnectorPricingDto
  {
    [Range(0, 10000)]
    public double PricePerKWh { get; set; }   
    [Range(0, 10000)]
    public double PricePerIdleMinute { get; set; }   
    [Range(0, 10000)]
    public double CostPerKwh { get; set; }
  }
}