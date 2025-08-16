

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
        public double? PricePerMinute { get; set; }
        public double? PricePerIdleMinute { get; set; }
        public double? ChargingPriceWithoutVAT { get; set; }
        public double? ChargingPriceWithVAT { get; set; }
        public double? IdlePriceWithoutVAT { get; set; }
        public double? IdlePriceWithVAT { get; set; }
        public double? TotalPriceWithoutVAT { get; set; }
        public double? TotalPriceWithVAT { get; set; }
        public double? KwhCharged { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ReasonEnumType StoppedReason { get; set; }
        public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    
  }
}