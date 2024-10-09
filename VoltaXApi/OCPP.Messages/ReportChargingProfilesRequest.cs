using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ReportChargingProfilesRequest
  {
      public List<ChargingProfileType> ChargingProfiles { get; set; }
  }

}