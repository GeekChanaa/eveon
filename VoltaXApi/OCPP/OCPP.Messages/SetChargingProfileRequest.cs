using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetChargingProfileRequest
  {
    public string ChargingProfileId { get; set; }
    public List<ChargingScheduleType> ChargingSchedules { get; set; }
    public string ConnectorId { get; set; }
    public DateTime StartTime { get; set; }
    public int Duration { get; set; }
    public SalesTariffType SalesTariff { get; set; }
  }

}