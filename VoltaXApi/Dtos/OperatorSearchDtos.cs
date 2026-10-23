using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    public sealed class OperatorSessionFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        /// <summary>"active", "completed" or empty for both.</summary>
        public string? Status { get; set; }
        /// <summary>"#123" for a session id, or text found in the id, charge point, station, card number, customer name or email.</summary>
        public string? Search { get; set; }
        public int? StationId { get; set; }
        public int? ChargePointId { get; set; }
        public int? UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    /// <summary>A session in the operator's list. Total is VAT included (running cost while the session is live).</summary>
    public sealed record OperatorSessionDto(int Id, int? ChargePointId, string? StationName, string? ChargePointName, string? Connector,
        int? UserId, string? UserName, string? CardLast4, DateTime StartDate, DateTime? EndDate, double ChargedMinutes, double IdleMinutes,
        double ChargedKwhs, decimal Total, ReasonEnumType StoppedReason, ChargingSessionStatusEnum Status, bool Active, bool Billed);

    public sealed class OperatorTransactionFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public TransactionStatusEnum? Status { get; set; }
        /// <summary>Transaction id (exact), charge point, id tag or stop reason.</summary>
        public string? Search { get; set; }
        public int? ChargePointId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    /// <summary>An OCPP transaction: meter readings in Wh as reported by the charger.</summary>
    public sealed record OperatorTransactionDto(int Id, string? Uid, int ChargingSessionId, int? ChargePointId, string? ChargePointName,
        string? StationName, string? Connector, DateTime StartTime, DateTime? StopTime, double MeterStart, double? MeterStop, string? IdTag,
        string? StartResult, string? StopReason, TransactionStatusEnum? Status, decimal Amount);
}
