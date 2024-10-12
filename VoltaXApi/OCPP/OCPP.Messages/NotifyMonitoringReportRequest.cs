using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  
  public class NotifyMonitoringReportRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<MonitoringDataType> Monitor { get; set; }

      [Required]
      public int RequestId { get; set; }

      public bool? Tbc { get; set; } = false;

      public int? SeqNo { get; set; }

      [Required]
      [DataType(DataType.DateTime)]
      public DateTime GeneratedAt { get; set; }
  }

  public class MonitoringDataType
  {
      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }

      [Required]
      [MinLength(1)]
      public List<VariableMonitoringType> VariableMonitoring { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public class VariableMonitoringType
  {
      [Required]
      public int Id { get; set; }

      [Required]
      public bool Transaction { get; set; }

      [Required]
      public double Value { get; set; }

      [Required]
      public MonitorEnumType Type { get; set; }

      [Required]
      [Range(0, 9)]
      public int Severity { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}