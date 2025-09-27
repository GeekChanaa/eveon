
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos;

public class PartnerChargingSessionListDto
{
    public int ID { get; set; }
    public int ConnectorID { get; set; }
    public int? ChargePointID { get; set; }
    public string Connector { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string ChargePointName { get; set; }
    public double? ChargedMinutes { get; set; }
    public double? IdleMinutes { get; set; }
    public double? ChargedKwhs { get; set; }
    public double? TotalPriceWithVAT { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
}