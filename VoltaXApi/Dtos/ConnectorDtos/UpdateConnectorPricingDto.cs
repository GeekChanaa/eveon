namespace VoltaXApi.Dtos
{
  public class UpdateConnectorPricingDto
  {
    public decimal PricePerKWh { get; set; }   
    public decimal PricePerMinute { get; set; }   
    public decimal PricePerHour { get; set; }
  }
}