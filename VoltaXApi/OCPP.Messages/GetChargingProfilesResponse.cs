using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetChargingProfilesResponse
  {
      [Required]
      public GetChargingProfileStatusEnumType Status { get; set; }

      public CustomDataType CustomData { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum GetChargingProfileStatusEnumType
  {
      Accepted,
      NoProfiles
  }

  public class StatusInfoType
  {
      [Required]
      [MaxLength(20)]
      public string ReasonCode { get; set; }

      [MaxLength(512)]
      public string AdditionalInfo { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}