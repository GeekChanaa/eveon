using System;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  
  public class NotifyEVChargingNeedsResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public NotifyEVChargingNeedsStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum NotifyEVChargingNeedsStatusEnumType
  {
      Accepted,
      Rejected,
      Processing
  }

}