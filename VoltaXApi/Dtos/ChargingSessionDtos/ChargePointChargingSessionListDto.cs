

using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
  public class ChargePointChargingSessionListDto
  {
    public string? UserName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    
  }
}