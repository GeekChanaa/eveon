

namespace VoltaxApi.Dtos;

public class ChargingSessionInvoice
{
    public string UserName { get; set; }
    public DateTime SessionDate { get; set; }
    public string ChargePointName { get; set; }
    public double TotalKwhCharged { get; set; }
    public double TotalPrice { get; set; }
    public int? ConnectorID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CardNumber { get; set; }
    public double ChargedMinutes { get; set; }
    public double IdleMinutes { get; set; }
    public double PricePerIdleMinute { get; set; }
    public double PricePerMinute { get; set; }
    public double KwhsCharged { get; set; }
    public double TotalPriceWithVAT { get; set; }
    public double TotalPriceWithoutVAT { get; set; }
    public double ChargingPriceWithVAT { get; set; }
    public double IdlePriceWithVAT { get; set; }
    public double CardBalance { get; set; }
    public double ChargingSessionID { get; set; }
    public List<TransactionItem> Transactions { get; set; }
}

public class TransactionItem
{
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public double MeterStart { get; set; }
    public double? MeterStop { get; set; }
    public double Amount { get; set; }
}