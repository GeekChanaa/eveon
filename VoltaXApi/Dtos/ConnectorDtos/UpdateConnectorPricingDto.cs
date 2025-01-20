namespace VoltaXApi.Dtos
{
  public class UpdateConnectorPricingDto
  {
    public decimal PricePerKWh { get; set; }   
    public decimal PricePerIdleMinute { get; set; }   
    public decimal CostPerKwh { get; set; }
  }
}