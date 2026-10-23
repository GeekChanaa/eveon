

namespace VoltaxApi.Dtos;

public class ChargingSessionInvoice
{
    public static ChargingSessionInvoice From(VoltaXApi.Dtos.ChargingSessionInformationsDto session) => new()
    {
        UserName = session.UserName ?? "",
        SessionDate = session.StartDate,
        StartDate = session.StartDate,
        EndDate = session.EndDate,
        ConnectorID = session.ConnectorID,
        ChargePointName = session.ChargePointName ?? "",
        TotalKwhCharged = session.KwhCharged ?? 0,
        TotalPrice = session.TotalPriceWithVAT ?? 0,
        TotalPriceWithoutVAT = session.TotalPriceWithoutVAT ?? 0,
        TotalPriceWithVAT = session.TotalPriceWithVAT ?? 0,
        ChargingPriceWithVAT = session.ChargingPriceWithVAT ?? 0,
        IdlePriceWithVAT = session.IdldePriceWithVAT ?? 0,
        CardNumber = session.CardNumber,
        ChargedMinutes = session.ChargedMinutes ?? 0,
        IdleMinutes = session.IdleMinutes ?? 0,
        PricePerIdleMinute = session.PricePerIdleMinute ?? 0,
        PricePerMinute = session.PricePerMinute ?? 0,
        KwhsCharged = session.KwhCharged ?? 0,
        CardBalance = session.CardBalance,
        ChargingSessionID = session.ID,
        IsBilled = session.IsBilled,
        PricePerKWh = session.PricePerKWh ?? 0,
        FlatFee = session.FlatFee ?? 0,
        VatRate = session.VatRate ?? 0,
        EnergyPriceWithVAT = session.EnergyPriceWithVAT ?? 0,
        TimePriceWithVAT = session.TimePriceWithVAT ?? 0,
        FlatFeeWithVAT = session.FlatFeeWithVAT ?? 0,
        BalanceBefore = session.BalanceBefore,
        BalanceAfter = session.BalanceAfter
    };

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