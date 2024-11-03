


using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
  public class ChargePointUptimeListDto
  {
    public int ChargePointID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ChargePointUptimeStatusEnum ChargePointUptimeStatus { get; set; }
  }
}