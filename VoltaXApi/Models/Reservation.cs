using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public enum ReservationStatusEnum
    {
        Active = 0,
        Used = 1,
        Cancelled = 2,
        Expired = 3,
        Rejected = 4
    }

    /// <summary>
    /// A ReserveNow sent to a charger (OCPP 1.6 or 2.0.1). <see cref="ReservationId"/> is the integer id the charger
    /// knows; EvseId is the 1.6 connectorId or the 2.0.1 evseId as sent (null/0 = any connector).
    /// </summary>
    public class Reservation
    {
        public int ID { get; set; }
        public int ReservationId { get; set; }
        public int ChargePointID { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public int? ConnectorID { get; set; }
        public Connector? Connector { get; set; }
        public int? EvseId { get; set; }
        [MaxLength(36)]
        public string IdToken { get; set; } = "";
        public int? UserID { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ReservationStatusEnum Status { get; set; }
        [MaxLength(36)]
        public string? TransactionUid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// An OCPP 2.0.1 transaction whose Started event arrived without EVSE (authorization before plug-in).
    /// It becomes a <see cref="Transaction"/> when the first event carrying the EVSE arrives.
    /// </summary>
    public class OcppPendingTransaction
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        [MaxLength(36)]
        public string TransactionUid { get; set; } = "";
        [MaxLength(36)]
        public string? IdTag { get; set; }
        [MaxLength(40)]
        public string? Timestamp { get; set; }
        public double? MeterStartKWh { get; set; }
        [MaxLength(40)]
        public string TriggerReason { get; set; } = "";
        public int? ReservationId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
