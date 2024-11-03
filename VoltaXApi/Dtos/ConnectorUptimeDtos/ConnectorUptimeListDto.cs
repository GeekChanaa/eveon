


using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
  public class ConnectorUptimeListDto
  {
    public int ConnectorID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ConnectorUptimeStatusEnum ConnectorUptimeStatus { get; set; }
  }
}