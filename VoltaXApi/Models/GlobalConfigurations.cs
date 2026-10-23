
namespace VoltaXApi.Models;

public class GlobalConfigurations
{
    public decimal DefaultPricePerKwh { get; set; }
    public decimal DefaultCostPerKwh { get; set; }
    public decimal DefaultFlatFee { get; set; }
    public decimal DefaultPricePerMinute { get; set; }
    public decimal DefaultIdleTimePricing { get; set; }
    public decimal DefaultMinimumRequiredBalance { get; set; }
    public decimal DefaultLowBalanceThresholdBuffer { get; set; }
    public double GracePeriod { get; set; }
    public decimal GraceAmount { get; set; }
    public decimal Vat { get; set; }
}