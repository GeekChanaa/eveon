using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class AdditionalInfoType
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MaxLength(36)]
      public string AdditionalIdToken { get; set; }

      [Required]
      [MaxLength(50)]
      public string Type { get; set; }
  }
}