using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  
  public class GetMonitoringReportRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public List<ComponentVariableType> ComponentVariable { get; set; }

      [Required]
      public int RequestId { get; set; }

      [Required]
      [MinLength(1)]
      [MaxLength(3)]
      public List<MonitoringCriterionEnumType> MonitoringCriteria { get; set; }
  }

  public enum MonitoringCriterionEnumType
  {
      ThresholdMonitoring,
      DeltaMonitoring,
      PeriodicMonitoring
  }

}