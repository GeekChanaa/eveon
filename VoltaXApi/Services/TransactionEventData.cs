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
        /// <summary>Why an Ended event stopped the transaction (2.0.1 stoppedReason; 1.6 reason mapped), stored on the session.</summary>
        public ReasonEnumType? StoppedReason { get; init; }
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
            StoppedReason = StoppedReasonOf(request),
            AbnormalCondition = request.TriggerReason == TriggerReasonEnumType.AbnormalCondition,
            AbnormalStop = IsAbnormalStop(request.TriggerReason, StoppedReasonOf(request)),
            ReasonSuffix = FormatReason(StoppedReasonOf(request)?.ToString())
        };

        /// <summary>Idempotency key of a customer notification about this event.</summary>
        public string EventKey(Card card) => !string.IsNullOrWhiteSpace(TransactionUid)
            ? $"TransactionUid:{TransactionUid}:{EventType}:{Timestamp}"
            : $"Card:{card.ID}:{EventType}:{Timestamp}";

        public string TransactionKey(Transaction transaction) => $"Transaction:{transaction.ID}:{EventType}:{Timestamp}";

        /// <summary>OCPP 2.0.1 carries it in transactionInfo.stoppedReason (the top-level field is a legacy fallback).</summary>
        public static ReasonEnumType? StoppedReasonOf(TransactionEventRequest request) =>
            request.TransactionInfo?.StoppedReason ?? request.Reason;

        public static bool IsAbnormalStop(TriggerReasonEnumType triggerReason, ReasonEnumType? reason)
        {
            if (triggerReason is TriggerReasonEnumType.AbnormalCondition or
                TriggerReasonEnumType.ResetCommand)
                return true;

            // Unplugging the car ends the transaction with EVCommunicationLost: abnormal only without a normal reason.
            if (triggerReason == TriggerReasonEnumType.EVCommunicationLost &&
                reason is not (ReasonEnumType.EVDisconnected or ReasonEnumType.Local or ReasonEnumType.Remote or ReasonEnumType.StoppedByEV))
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
