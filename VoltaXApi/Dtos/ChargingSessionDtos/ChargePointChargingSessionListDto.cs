

using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
  public class ChargePointChargingSessionListDto
  {
    public int ID { get; set; }
    public string? UserName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    
  }
}