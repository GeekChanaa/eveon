

using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    public class ChargingSessionForMailDto
    {
        public int ID { get; set; }
        public string? ChargePointName { get; set; }
        public string? UserName { get; set; }
        public int? UserID { get; set; }
        public int? ConnectorID { get; set; }
        public string? ConnectorType { get; set; }
        public double? ConnectorPower { get; set; }
        public double? ChargedMinutes { get; set; }
        public double? IdleMinutes { get; set; }
        public decimal? PricePerMinute { get; set; }
        public decimal? PricePerIdleMinute { get; set; }
        public decimal? ChargingPriceWithoutVAT { get; set; }
        public decimal? ChargingPriceWithVAT { get; set; }
        public decimal? IdlePriceWithoutVAT { get; set; }
        public decimal? IdlePriceWithVAT { get; set; }
        public decimal? TotalPriceWithoutVAT { get; set; }
        public decimal? TotalPriceWithVAT { get; set; }
        public double? KwhCharged { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ReasonEnumType StoppedReason { get; set; }
        public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    
  }
}