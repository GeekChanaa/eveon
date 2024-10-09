using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class StatusNotificationResponse
  {
      public CustomDataType? CustomData { get; set; }
  }
}