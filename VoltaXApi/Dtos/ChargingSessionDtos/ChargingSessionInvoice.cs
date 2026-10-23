

namespace VoltaxApi.Dtos;

public class ChargingSessionInvoice
{
    public string UserName { get; set; }
    public DateTime SessionDate { get; set; }
    public string ChargePointName { get; set; }
    public double TotalKwhCharged { get; set; }
    public decimal TotalPrice { get; set; }
    public int? ConnectorID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CardNumber { get; set; }
    public double ChargedMinutes { get; set; }
    public double IdleMinutes { get; set; }
    public decimal PricePerIdleMinute { get; set; }
    public decimal PricePerMinute { get; set; }
    public double KwhsCharged { get; set; }
    public decimal TotalPriceWithVAT { get; set; }
    public decimal TotalPriceWithoutVAT { get; set; }
    public decimal ChargingPriceWithVAT { get; set; }
    public decimal IdlePriceWithVAT { get; set; }
    public decimal CardBalance { get; set; }
    public bool IsBilled { get; set; }
    public decimal PricePerKWh { get; set; }
    public decimal FlatFee { get; set; }
    public decimal VatRate { get; set; }
    public decimal EnergyPriceWithVAT { get; set; }
    public decimal TimePriceWithVAT { get; set; }
    public decimal FlatFeeWithVAT { get; set; }
    public decimal? BalanceBefore { get; set; }
    public decimal? BalanceAfter { get; set; }
    public double ChargingSessionID { get; set; }
    public List<TransactionItem> Transactions { get; set; }
}

public class TransactionItem
{
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public double MeterStart { get; set; }
    public double? MeterStop { get; set; }
    public decimal Amount { get; set; }
}