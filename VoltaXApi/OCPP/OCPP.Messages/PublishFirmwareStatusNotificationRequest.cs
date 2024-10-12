using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public enum PublishFirmwareStatusEnum
  {
      Idle,
      DownloadScheduled,
      Downloading,
      Downloaded,
      Published,
      DownloadFailed,
      DownloadPaused,
      InvalidChecksum,
      ChecksumVerified,
      PublishFailed
  }

  public class PublishFirmwareStatusNotificationRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public PublishFirmwareStatusEnum Status { get; set; }

      [MinLength(1)]
      public List<string>? Location { get; set; }

      public int? RequestId { get; set; }
  }

}