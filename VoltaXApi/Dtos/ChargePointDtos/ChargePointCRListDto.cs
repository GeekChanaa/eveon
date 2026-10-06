
using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class ChargePointCRListDto
{
    public int ID  { get; set; }
    public string ChargePointId  { get; set; }
    public string ChargingStationName  { get; set; }
    public string SerialNumber  { get; set; }
    public ChargePointCategoryEnum Category  { get; set; }
    public ChargePointStatusEnum Status  { get; set; }
    public string PartnerName  { get; set; }
    /// <summary>True once our system sent the charge point its configuration (automatic or manual provisioning).</summary>
    public bool IsConfigured { get; set; }
    /// <summary>How it was accepted; null when it never was (it is kept Pending on BootNotification).</summary>
    public ChargePointProvisioningMethodEnum? ConfigurationMethod { get; set; }
    public bool IsOnline { get; set; }
    /// <summary>OCPP version of the current connection ("ocpp1.6" / "ocpp2.0.1"); null when offline.</summary>
    public string? ProtocolVersion { get; set; }
}