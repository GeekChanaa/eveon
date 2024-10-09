using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetVariableMonitoringResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<SetMonitoringResultType> SetMonitoringResult { get; set; }
  }

  public enum SetMonitoringStatusEnumType
  {
      Accepted,
      UnknownComponent,
      UnknownVariable,
      UnsupportedMonitorType,
      Rejected,
      Duplicate
  }

  public class SetMonitoringResultType
  {
      [Required]
      public SetMonitoringStatusEnumType Status { get; set; }

      [Required]
      public MonitorEnumType Type { get; set; }

      [Required]
      public int Severity { get; set; }

      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }

      public CustomDataType CustomData { get; set; }
      
      public int? Id { get; set; }
      
      public StatusInfoType StatusInfo { get; set; }
  }

}