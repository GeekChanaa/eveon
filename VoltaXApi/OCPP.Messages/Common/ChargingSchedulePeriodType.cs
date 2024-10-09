

namespace VoltaXApi.OCPP.Messages
{
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