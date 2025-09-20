

using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
  public class ChargingSessionInformationsDto
  {
    public int ID { get; set; }
    public int? ChargePointID { get; set; }
    public string? ChargePointName { get; set; }
    public string? UserName { get; set; }
    public int? UserID { get; set; }
    public int? CardID { get; set; }
    public double CardBalance { get; set; }
    public string CardNumber { get; set; }
    public int? ConnectorID { get; set; }
    public double? ChargedMinutes { get; set; }
    public double? IdleMinutes { get; set; }
    public double? PricePerMinute { get; set; }
    public double? PricePerIdleMinute { get; set; }
    public double? ChargingPriceWithoutVAT { get; set; }
    public double? ChargingPriceWithVAT { get; set; }
    public double? IdlePriceWithoutVAT { get; set; }
    public double? IdldePriceWithVAT { get; set; }
    public double? TotalPriceWithoutVAT { get; set; }
    public double? TotalPriceWithVAT { get; set; }
    public double? KwhCharged { get; set; }
    public double? CostPerKwh { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public Card? Card { get; set; }
    public Connector? Connector { get; set; }
    public List<Transaction>? Transactions { get; set; }
    
  }
}