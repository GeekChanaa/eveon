

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models
{
  public class ChargingSession: IEntity
  {
    public int ID { get; set; }
    public int ChargePointID { get; set; }
    public int ConnectorID { get; set; }
    public int UserID { get; set; }
    public int CardID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
    
  }
}