using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class FirmwareStatusNotificationRequest
  {
      public CustomDataType CustomData { get; set; }
      
      [Required]
      public FirmwareStatusEnumType Status { get; set; }
      
      public int? RequestId { get; set; }  // Optional field, so no [Required] annotation
  }

  public enum FirmwareStatusEnumType
  {
      Downloaded,
      DownloadFailed,
      Downloading,
      DownloadScheduled,
      DownloadPaused,
      Idle,
      InstallationFailed,
      Installing,
      Installed,
      InstallRebooting,
      InstallScheduled,
      InstallVerificationFailed,
      InvalidSignature,
      SignatureVerified
  }

}