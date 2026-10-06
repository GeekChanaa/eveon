using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos;

/// <summary>
/// Customer-safe charging-session history. This intentionally excludes user data,
/// full card details, internal entity IDs, and operator cost information.
/// </summary>
public sealed class MyChargingSessionDto
{
    public int ID { get; set; }
    public string? StationName { get; set; }
    public string? StationAddress { get; set; }
    public string? ChargePointName { get; set; }
    public string? Connector { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double ChargedMinutes { get; set; }
    public double IdleMinutes { get; set; }
    public double ChargedKwhs { get; set; }
    public double TotalPriceWithVAT { get; set; }
    public string Currency { get; set; } = "MAD";
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum Status { get; set; }
    public bool InvoiceAvailable { get; set; }
}
