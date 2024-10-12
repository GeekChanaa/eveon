using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class FirmwareStatusNotificationResponse
  {
      public CustomDataType CustomData { get; set; }
  }
}