using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class PublishFirmwareRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      [MaxLength(512)]
      public string Location { get; set; } = string.Empty;

      public int? Retries { get; set; }

      [Required]
      [MaxLength(32)]
      public string Checksum { get; set; } = string.Empty;

      [Required]
      public int RequestId { get; set; }

      public int? RetryInterval { get; set; }
  }

}