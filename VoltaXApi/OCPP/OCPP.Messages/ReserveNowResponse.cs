using System;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  
  public class ReserveNowResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public ReserveNowStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum ReserveNowStatusEnumType
  {
      Accepted,
      Faulted,
      Occupied,
      Rejected,
      Unavailable
  }

}