using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ChargingScheduleType
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public int Id { get; set; }

      public DateTime? StartSchedule { get; set; }

      /// <summary>Seconds; absent means the schedule lasts until the profile ends.</summary>
      public int? Duration { get; set; }

      [Required]
      public ChargingRateUnitEnumType ChargingRateUnit { get; set; }

      [Required]
      [MinLength(1)]
      [MaxLength(1024)]
      public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }

      public double? MinChargingRate { get; set; }

      public SalesTariffType? SalesTariff { get; set; }
  }
}
