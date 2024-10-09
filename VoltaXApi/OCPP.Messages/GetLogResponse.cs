using System;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class GetLogResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public LogStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }

      [MaxLength(255)]
      public string Filename { get; set; }
  }

  public enum LogStatusEnumType
  {
      Accepted,
      Rejected,
      AcceptedCanceled
  }

}