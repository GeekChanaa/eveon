

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ChargingScheduleType
  {
      [Required]
      public int Duration { get; set; }

      [Required]
      public int StartSchedule { get; set; }

      [Required]
      public ChargingRateUnitEnumType ChargingRateUnit { get; set; }

      public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }
  }
}