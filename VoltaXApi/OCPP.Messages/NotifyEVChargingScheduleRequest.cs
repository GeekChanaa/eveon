using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class NotifyEVChargingScheduleRequest
  {
      [Required]
      public string ChargingStationId { get; set; }

      [Required]
      public int EvseId { get; set; }

      [Required]
      public string ConnectorId { get; set; }

      [Required]
      public ChargingScheduleType ChargingSchedule { get; set; }

      public string Reason { get; set; }

      public string Status { get; set; }
      public string TimeBase { get; set; }

      public string ReservationId { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}