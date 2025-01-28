using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetVariableMonitoringRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<SetMonitoringDataType> SetMonitoringData { get; set; }
  }

  public class SetMonitoringDataType
  {
      public CustomDataType? CustomData { get; set; }

      public int? Id { get; set; }

      public bool Transaction { get; set; } = false;

      [Required]
      public double Value { get; set; }

      [Required]
      public MonitorEnumType Type { get; set; }

      [Required]
      [Range(0, 9)]
      public int Severity { get; set; }

      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }
  }

}