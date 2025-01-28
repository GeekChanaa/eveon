using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class UpdateFirmwareRequest
  {
      public CustomDataType? CustomData { get; set; }

      public int? Retries { get; set; }

      public int? RetryInterval { get; set; }

      [Required]
      public int RequestId { get; set; }

      [Required]
      public FirmwareType Firmware { get; set; }
  }
  
  public class FirmwareType
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      [MaxLength(512)]
      public string Location { get; set; }

      [Required]
      public string RetrieveDateTime { get; set; }

      public string? InstallDateTime { get; set; }

      [MaxLength(5500)]
      public string? SigningCertificate { get; set; }

      [MaxLength(800)]
      public string? Signature { get; set; }
  }

}