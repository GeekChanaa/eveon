using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetMonitoringLevelResponse
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public GenericStatusEnumType Status { get; set; }

      public StatusInfoType? StatusInfo { get; set; }
  }
}