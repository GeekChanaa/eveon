
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos;

public class ChargingSessionListDto
{
    public int ID { get; set; }
    public int ConnectorID { get; set; }
    public int? ChargePointID { get; set; }
    public string Connector { get; set; }
    public string? UserName { get; set; }
    public string? CardNumber { get; set; }
    public DateTime StartDate { get; set; }
    public string ChargePointName { get; set; }
    public double? ChargedMinutes { get; set; }
    public double? IdleMinutes { get; set; }
    public double? ChargedKwhs { get; set; }
    public decimal? PricePerMinute { get; set; }
    public decimal? CostPerKwh { get; set; }
    public decimal? PricePerIdleMinute { get; set; }
    public decimal? TotalPriceWithVAT { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
}