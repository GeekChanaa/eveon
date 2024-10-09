using System;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  
  public class ResetRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public ResetEnumType Type { get; set; }

      public int? EvseId { get; set; }
  }

  public enum ResetEnumType
  {
      Immediate,
      OnIdle
  }

}