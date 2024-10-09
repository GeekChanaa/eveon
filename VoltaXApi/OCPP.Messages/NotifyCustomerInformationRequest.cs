using System;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  
  public class NotifyCustomerInformationRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MaxLength(512)]
      public string Data { get; set; }

      public bool Tbc { get; set; } = false; // Default value is false

      [Required]
      public int SeqNo { get; set; }

      [Required]
      [DataType(DataType.DateTime)]
      public DateTime GeneratedAt { get; set; }

      [Required]
      public int RequestId { get; set; }
  }
}