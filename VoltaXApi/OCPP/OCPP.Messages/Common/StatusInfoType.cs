using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class StatusInfoType
  {
      [Required]
      [MaxLength(20)]
      public string ReasonCode { get; set; }

      public CustomDataType CustomData { get; set; }

      [MaxLength(512)]
      public string AdditionalInfo { get; set; }
  }
}