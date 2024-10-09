using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class PublishFirmwareStatusNotificationResponse
  {
      public CustomDataType? CustomData { get; set; }
  }
}