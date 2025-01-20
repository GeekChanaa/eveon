

namespace VoltaXApi.Dtos
{
  public class ChargePointStatisticsSummaryDto
  {
    public int NbrChargingSessions { get; set; }
    public int? NbrChargingSessionsLastWeek { get; set; }
    public double TotalEnergy { get; set; }
    public double? TotalEnergyLastWeek { get; set; }
  }
}