using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetMonitoringLevelRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      [Range(0, 9)]
      public int Severity { get; set; }
  }

}