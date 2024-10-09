using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class UnpublishFirmwareRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      [MaxLength(32)]
      public string Checksum { get; set; }
  }

}