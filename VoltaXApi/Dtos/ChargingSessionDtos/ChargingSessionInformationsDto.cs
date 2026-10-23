

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
    public decimal CardBalance { get; set; }
    public string CardNumber { get; set; }
    public int? ConnectorID { get; set; }
    public double? ChargedMinutes { get; set; }
    public double? IdleMinutes { get; set; }
    public decimal? PricePerMinute { get; set; }
    public decimal? PricePerIdleMinute { get; set; }
    public decimal? ChargingPriceWithoutVAT { get; set; }
    public decimal? ChargingPriceWithVAT { get; set; }
    public decimal? IdlePriceWithoutVAT { get; set; }
    public decimal? IdldePriceWithVAT { get; set; }
    public decimal? TotalPriceWithoutVAT { get; set; }
    public decimal? TotalPriceWithVAT { get; set; }
    public double? KwhCharged { get; set; }
    public decimal? CostPerKwh { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public Card? Card { get; set; }
    public Connector? Connector { get; set; }
    public List<Transaction>? Transactions { get; set; }
    
  }
}