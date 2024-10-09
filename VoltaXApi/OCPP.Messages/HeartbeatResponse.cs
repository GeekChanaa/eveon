using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class HeartbeatResponse
  {
      [Required]
      public string CurrentTime { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}