

namespace VoltaxApi.Dtos;

public class ChargingSessionInvoice
{
    public string UserName { get; set; }
    public DateTime SessionDate { get; set; }
    public string ChargePointName { get; set; }
    public double TotalKwhCharged { get; set; }
    public double TotalPrice { get; set; }
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