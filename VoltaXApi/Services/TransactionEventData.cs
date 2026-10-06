using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    /// <summary>
    /// One charger transaction event in version-neutral form (OCPP 2.0.1 TransactionEvent, OCPP 1.6
    /// StartTransaction / MeterValues / StopTransaction), as consumed by <see cref="ITransactionService"/>.
    /// </summary>
    public sealed record TransactionEventData
    {
        public required string ChargePointId { get; init; }
        public required string TransactionUid { get; init; }
        public required TransactionEventEnumType EventType { get; init; }
        /// <summary>Charger timestamp as sent (parsed as UTC; invalid values are handled by the service).</summary>
        public string? Timestamp { get; init; }
        public string? IdTag { get; init; }
        /// <summary>Energy register in kWh, null when the event carried none.</summary>
        public double? MeterKWh { get; init; }
        /// <summary>Stored as Transaction.StartResult / StopReason.</summary>
        public string TriggerReason { get; init; } = "";
        /// <summary>Updated event reporting an abnormal condition (customer is notified).</summary>
        public bool AbnormalCondition { get; init; }
        /// <summary>Ended event whose reason is abnormal (customer is notified).</summary>
        public bool AbnormalStop { get; init; }
        /// <summary>" (emergency stop)" style suffix for customer notifications, empty when no reason.</summary>
        public string ReasonSuffix { get; init; } = "";

        public static TransactionEventData FromTransactionEvent(TransactionEventRequest request, string chargePointId, string? idTag, double? meterKWh) => new()
        {
            ChargePointId = chargePointId,
            TransactionUid = request.TransactionInfo?.TransactionId ?? "",
            EventType = request.EventType,
            Timestamp = request.Timestamp,
            IdTag = idTag,
            MeterKWh = meterKWh,
            TriggerReason = request.TriggerReason.ToString(),
            AbnormalCondition = request.TriggerReason == TriggerReasonEnumType.AbnormalCondition,
            AbnormalStop = IsAbnormalStop(request.TriggerReason, request.Reason),
            ReasonSuffix = FormatReason(request.Reason?.ToString())
        };

        /// <summary>Idempotency key of a customer notification about this event.</summary>
        public string EventKey(Card card) => !string.IsNullOrWhiteSpace(TransactionUid)
            ? $"TransactionUid:{TransactionUid}:{EventType}:{Timestamp}"
            : $"Card:{card.ID}:{EventType}:{Timestamp}";

        public string TransactionKey(Transaction transaction) => $"Transaction:{transaction.ID}:{EventType}:{Timestamp}";

        public static bool IsAbnormalStop(TriggerReasonEnumType triggerReason, ReasonEnumType? reason)
        {
            if (triggerReason is TriggerReasonEnumType.AbnormalCondition or
                TriggerReasonEnumType.EVCommunicationLost or
                TriggerReasonEnumType.ResetCommand)
                return true;

            return reason is ReasonEnumType.EmergencyStop or
                ReasonEnumType.GroundFault or
                ReasonEnumType.OvercurrentFault or
                ReasonEnumType.PowerLoss or
                ReasonEnumType.PowerQuality or
                ReasonEnumType.Reboot or
                ReasonEnumType.ImmediateReset or
                ReasonEnumType.Other;
        }

        public static string FormatReason(string? reason) =>
            string.IsNullOrEmpty(reason) ? string.Empty : $" ({Humanize(reason)})";

        internal static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2").ToLowerInvariant();
    }
}
