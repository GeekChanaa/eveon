namespace VoltaXApi.Dtos
{
  public class UpdateConnectorPricingDto
  {
    public double PricePerKWh { get; set; }   
    public double PricePerIdleMinute { get; set; }   
    public double CostPerKwh { get; set; }
  }
}