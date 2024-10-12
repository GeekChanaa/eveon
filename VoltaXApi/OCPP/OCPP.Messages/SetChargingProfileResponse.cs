using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class SetChargingProfileResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public ChargingProfileStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum ChargingProfileStatusEnumType
  {
      Accepted,
      Rejected
  }

}