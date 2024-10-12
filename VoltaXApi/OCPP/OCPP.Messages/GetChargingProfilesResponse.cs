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

}