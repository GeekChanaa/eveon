

using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
  public class ChargingSessionInformationsDto
  {
    public int ID { get; set; }
    public string? UserName { get; set; }
    public int? UserID { get; set; }
    public int? CardID { get; set; }
    public int? ConnectorID { get; set; }
    public decimal? ConnectorRatio { get; set; }
    public decimal? ConnectorCostRatio { get; set; }
    public int? ChargePointID { get; set; }
    public string? ChargePointName { get; set; }
    public double? KwhCharged { get; set; }
    public double? TotalPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public Card? Card { get; set; }
    public Connector? Connector { get; set; }
    public List<Transaction>? Transactions { get; set; }
    
  }
}