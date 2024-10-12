using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class GetCompositeScheduleRequest
  {
      [Required]
      public int Duration { get; set; }

      [Required]
      public int EvseId { get; set; }

      public ChargingRateUnitEnumType? ChargingRateUnit { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}