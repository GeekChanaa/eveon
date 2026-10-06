using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  /// <summary>OCPP 2.0.1 ReportChargingProfiles.req, answer to GetChargingProfiles (several parts while tbc is true).</summary>
  public class ReportChargingProfilesRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public int RequestId { get; set; }

      [Required]
      public ChargingLimitSourceEnumType ChargingLimitSource { get; set; }

      [Required]
      [MinLength(1)]
      public List<ChargingProfileType> ChargingProfile { get; set; }

      public bool? Tbc { get; set; }

      [Required]
      public int EvseId { get; set; }
  }
}
