using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class NotifyChargingLimitResponse
  {
      public CustomDataType CustomData { get; set; }
  }
}