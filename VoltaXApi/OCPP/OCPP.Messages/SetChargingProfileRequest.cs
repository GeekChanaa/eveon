using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  /// <summary>OCPP 2.0.1 SetChargingProfile.req: evseId 0 targets the whole charging station.</summary>
  public class SetChargingProfileRequest
  {
    public CustomDataType? CustomData { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int EvseId { get; set; }

    [Required]
    public ChargingProfileType ChargingProfile { get; set; }
  }
}
