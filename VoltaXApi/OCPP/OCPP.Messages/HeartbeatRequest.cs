using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class HeartbeatRequest
  {
      public CustomDataType CustomData { get; set; }
  }
}
