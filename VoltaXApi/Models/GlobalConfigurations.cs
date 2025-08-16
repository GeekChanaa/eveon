
namespace VoltaXApi.Models;

public class GlobalConfigurations
{
    public double DefaultPricePerKwh { get; set; }
    public double DefaultCostPerKwh { get; set; }
    public double DefaultFlatFee { get; set; }
    public double DefaultPricePerMinute { get; set; }
    public double DefaultIdleTimePricing { get; set; }
    public double GracePeriod { get; set; }
    public double GraceAmount { get; set; }
    public double Vat { get; set; }
}