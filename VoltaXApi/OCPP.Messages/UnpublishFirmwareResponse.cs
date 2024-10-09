using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class UnpublishFirmwareResponse
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public UnpublishFirmwareStatusEnumType Status { get; set; }
  }

  public enum UnpublishFirmwareStatusEnumType
  {
      DownloadOngoing,
      NoFirmware,
      Unpublished
  }

}