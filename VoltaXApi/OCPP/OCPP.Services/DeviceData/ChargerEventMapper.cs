using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Pure mapping of OCPP 2.0.1 device-model messages to their stored rows.</summary>
    public static class ChargerEventMapper
    {
        /// <summary>Severities 0-3 (Danger, HardwareFailure, SystemFailure, Critical) raise an alarm.</summary>
        public const int AlarmSeverityThreshold = 3;

        private static readonly string[] FaultVariables = { "Problem", "Tripped", "Fault", "Overheat", "Fallback", "Overcurrent", "Overvoltage", "Undervoltage" };

        /// <summary>An OCPP timestamp as UTC (a value without offset is taken as UTC).</summary>
        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        public static DateTime? ToUtc(DateTime? value) => value.HasValue ? ToUtc(value.Value) : null;

        public static string? Truncate(string? value, int max) =>
            value == null || value.Length <= max ? value : value[..max];

        /// <summary>
        /// Maps every eventData of a NotifyEvent part. <paramref name="severityByMonitorId"/> resolves the severity of the
        /// monitor that raised an event (from the last monitoring report); hard-wired notifications have none.
        /// </summary>
        public static List<ChargerEvent> Map(string chargePointId, NotifyEventRequest request, DateTime receivedAtUtc,
            IReadOnlyDictionary<int, int>? severityByMonitorId = null)
        {
            var events = new List<ChargerEvent>();
            foreach (var data in request.EventData ?? new List<EventDataType>())
            {
                if (data == null) continue;
                int? severity = data.VariableMonitoringId.HasValue && severityByMonitorId != null &&
                                severityByMonitorId.TryGetValue(data.VariableMonitoringId.Value, out var s) ? s : null;
                events.Add(new ChargerEvent
                {
                    ChargePointID = chargePointId,
                    EventId = data.EventId,
                    Timestamp = data.Timestamp == default ? ToUtc(request.GeneratedAt) : ToUtc(data.Timestamp),
                    Trigger = data.Trigger.ToString(),
                    ActualValue = Truncate(data.ActualValue, 2500),
                    TechCode = Truncate(data.TechCode, 50),
                    TechInfo = Truncate(data.TechInfo, 500),
                    Cleared = data.Cleared,
                    Cause = data.Cause,
                    TransactionId = Truncate(data.TransactionId, 36),
                    ComponentName = Truncate(data.Component?.Name, 50) ?? "",
                    ComponentInstance = Truncate(data.Component?.Instance, 50),
                    VariableName = Truncate(data.Variable?.Name, 50) ?? "",
                    VariableInstance = Truncate(data.Variable?.Instance, 50),
                    EvseId = data.Component?.Evse?.Id,
                    ConnectorId = data.Component?.Evse?.ConnectorId,
                    VariableMonitoringId = data.VariableMonitoringId,
                    EventNotificationType = data.EventNotificationType.ToString(),
                    Severity = severity,
                    ReceivedAt = receivedAtUtc
                });
            }
            return events;
        }

        /// <summary>
        /// True for events that belong in the alarm feed: a monitor of severity &lt;= 3, or a fault-like variable
        /// (Problem, Tripped, Fault...) that became active. Cleared events never alarm.
        /// </summary>
        public static bool IsAlarm(ChargerEvent e)
        {
            if (e.Cleared == true) return false;
            if (e.Severity is <= AlarmSeverityThreshold) return true;
            var faultVariable = FaultVariables.Any(v => e.VariableName.Contains(v, StringComparison.OrdinalIgnoreCase));
            if (!faultVariable) return false;
            // Boolean problem variables only alarm when set; other values (e.g. a fault code) always do.
            return !string.Equals(e.ActualValue, "false", StringComparison.OrdinalIgnoreCase) && e.ActualValue != "0";
        }

        public static List<VariableMonitor> MapMonitors(string chargePointId, int requestId, IEnumerable<MonitoringDataType> monitoring, DateTime reportedAtUtc) =>
            monitoring.Where(m => m != null)
                .SelectMany(m => (m.VariableMonitoring ?? new List<VariableMonitoringType>()).Where(v => v != null).Select(v => new VariableMonitor
                {
                    ChargePointID = chargePointId,
                    ComponentName = Truncate(m.Component?.Name, 50) ?? "",
                    ComponentInstance = Truncate(m.Component?.Instance, 50),
                    VariableName = Truncate(m.Variable?.Name, 50) ?? "",
                    VariableInstance = Truncate(m.Variable?.Instance, 50),
                    EvseId = m.Component?.Evse?.Id,
                    ConnectorId = m.Component?.Evse?.ConnectorId,
                    MonitoringId = v.Id,
                    Type = v.Type.ToString(),
                    Value = v.Value,
                    Severity = v.Severity,
                    Transaction = v.Transaction,
                    RequestId = requestId,
                    ReportedAt = reportedAtUtc
                }))
                // A monitor id is unique per charger; a repeated id in a report is the same monitor.
                .GroupBy(m => m.MonitoringId)
                .Select(g => g.Last())
                .ToList();

        public static List<DisplayMessageSnapshot> MapDisplayMessages(string chargePointId, int requestId, IEnumerable<MessageInfoType> messages, DateTime receivedAtUtc) =>
            messages.Where(m => m != null).Select(m => new DisplayMessageSnapshot
            {
                ChargePointID = chargePointId,
                RequestId = requestId,
                MessageId = m.Id,
                Priority = m.Priority.ToString(),
                State = m.State?.ToString(),
                StartDateTime = ToUtc(m.StartDateTime),
                EndDateTime = ToUtc(m.EndDateTime),
                TransactionId = Truncate(m.TransactionId, 36),
                Content = Truncate(m.Message?.Content, 1024),
                Format = m.Message?.Format.ToString(),
                Language = Truncate(m.Message?.Language, 8),
                DisplayComponentName = Truncate(m.Display?.Name, 50),
                DisplayComponentInstance = Truncate(m.Display?.Instance, 50),
                DisplayEvseId = m.Display?.Evse?.Id,
                ReceivedAt = receivedAtUtc
            }).ToList();
    }
}
