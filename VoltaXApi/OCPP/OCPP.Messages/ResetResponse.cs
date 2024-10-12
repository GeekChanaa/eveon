using System;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ResetResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public ResetStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum ResetStatusEnumType
  {
      Accepted,
      Rejected,
      Scheduled
  }

}