using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class GetCompositeScheduleResponse
  {
      [Required]
      public GenericStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }

      public CompositeScheduleType Schedule { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public enum GenericStatusEnumType
  {
      Accepted,
      Rejected
  }

  public class CompositeScheduleType
  {
      [Required]
      public int EvseId { get; set; }

      [Required]
      public int Duration { get; set; }

      [Required]
      [DataType(DataType.DateTime)]
      public DateTime ScheduleStart { get; set; }

      [Required]
      public ChargingRateUnitEnumType ChargingRateUnit { get; set; }

      [Required]
      [MinLength(1)]
      public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public class ChargingSchedulePeriodType
  {
      [Required]
      public int StartPeriod { get; set; }

      [Required]
      public double Limit { get; set; }

      public int? NumberPhases { get; set; }

      public int? PhaseToUse { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}