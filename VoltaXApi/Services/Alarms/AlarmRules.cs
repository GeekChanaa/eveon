using VoltaXApi.Models.Alarms;
using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.Services.Alarms
{
    /// <summary>A fault reported by a charger, to be raised as an alarm (or counted on the open one).</summary>
    public sealed record AlarmSignal(AlarmType Type, string Code, AlarmSeverity Severity, string Description, string? Details,
        int? EvseId, int? ConnectorNumber, DateTime At);

    /// <summary>Pure mapping of OCPP messages to alarm signals and clearances.</summary>
    public static class AlarmRules
    {
        public const string FaultedCode = "Faulted";
        public const string OfflineCode = "Offline";
        public const string BootLoopCode = "BootLoop";

        // 1.6 error codes that endanger the installation or the vehicle even when the connector is not Faulted.
        private static readonly HashSet<string> MajorErrorCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "GroundFailure", "HighTemperature", "OverCurrentFailure", "OverVoltage", "PowerSwitchFailure", "InternalError"
        };

        private static readonly string[] FaultVariables = { "Problem", "Tripped", "Fault", "Overheat", "Fallback", "Overcurrent", "Overvoltage", "Undervoltage" };

        /// <summary>
        /// OCPP 1.6 StatusNotification. A Faulted status is critical; another error code is major when it endangers the
        /// installation (ground fault, overcurrent...) and minor otherwise (reader, meter, EV communication...).
        /// Null when the notification reports no fault (it then clears the connector's status alarms).
        /// </summary>
        public static AlarmSignal? FromStatus16(int connectorId, string status, string? errorCode, string? info, string? vendorErrorCode, DateTime at)
        {
            var faulted = string.Equals(status, "Faulted", StringComparison.OrdinalIgnoreCase);
            var hasError = !string.IsNullOrWhiteSpace(errorCode) && !string.Equals(errorCode, "NoError", StringComparison.OrdinalIgnoreCase);
            if (!faulted && !hasError) return null;

            var code = hasError ? errorCode!.Trim() : FaultedCode;
            var severity = faulted ? AlarmSeverity.Critical : MajorErrorCodes.Contains(code) ? AlarmSeverity.Major : AlarmSeverity.Minor;
            var where = connectorId > 0 ? $"Connector {connectorId}" : "Charge point";
            var description = faulted ? $"{where} faulted ({code})" : $"{where} reported {code} while {status}";
            return new AlarmSignal(AlarmType.ConnectorFault, code, severity, description, Join(info, vendorErrorCode is { Length: > 0 } ? $"vendor code {vendorErrorCode}" : null),
                connectorId > 0 ? connectorId : null, connectorId > 0 ? 1 : null, at);
        }

        /// <summary>OCPP 2.0.1 StatusNotification: only Faulted raises an alarm; any other status clears it.</summary>
        public static AlarmSignal? FromStatus201(int evseId, int connectorId, ConnectorStatusEnumType status, DateTime at) =>
            status != ConnectorStatusEnumType.Faulted
                ? null
                : new AlarmSignal(AlarmType.ConnectorFault, FaultedCode, AlarmSeverity.Critical,
                    evseId > 0 ? $"EVSE {evseId} connector {connectorId} faulted" : "Charge point faulted", null,
                    evseId > 0 ? evseId : null, evseId > 0 ? connectorId : null, at);

        /// <summary>The alarm code of a device event: Component[instance].Variable[instance].</summary>
        public static string EventCode(ChargerEvent e)
        {
            static string Part(string name, string? instance) => string.IsNullOrEmpty(instance) ? name : $"{name}[{instance}]";
            var code = $"{Part(e.ComponentName, e.ComponentInstance)}.{Part(e.VariableName, e.VariableInstance)}";
            return code.Length <= 100 ? code : code[..100];
        }

        /// <summary>An alarm-worthy 2.0.1 event (see <see cref="ChargerEventMapper.IsAlarm"/>) as a signal; null otherwise.</summary>
        public static AlarmSignal? FromEvent(ChargerEvent e)
        {
            if (!ChargerEventMapper.IsAlarm(e)) return null;
            var severity = e.Severity switch
            {
                <= 1 => AlarmSeverity.Critical,          // Danger, HardwareFailure
                <= 3 => AlarmSeverity.Major,             // SystemFailure, Critical
                null => AlarmSeverity.Major,             // hard-wired fault variable
                _ => AlarmSeverity.Minor
            };
            var value = string.IsNullOrEmpty(e.ActualValue) ? "" : $" = {Truncate(e.ActualValue, 100)}";
            return new AlarmSignal(AlarmType.DeviceEvent, EventCode(e), severity, $"{e.ComponentName} {e.VariableName}{value}",
                Join(e.TechCode, e.TechInfo, e.Severity.HasValue ? $"monitor severity {e.Severity}" : null),
                e.EvseId, e.EvseId.HasValue ? e.ConnectorId : null, e.Timestamp);
        }

        /// <summary>A 2.0.1 event that reports a fault gone: cleared, or a fault variable set back to false / 0.</summary>
        public static bool ClearsEvent(ChargerEvent e)
        {
            if (e.Cleared == true) return true;
            var faultVariable = FaultVariables.Any(v => e.VariableName.Contains(v, StringComparison.OrdinalIgnoreCase));
            return faultVariable && (string.Equals(e.ActualValue, "false", StringComparison.OrdinalIgnoreCase) || e.ActualValue == "0");
        }

        public static AlarmSignal Offline(TimeSpan offlineFor, DateTime since, DateTime at) =>
            new(AlarmType.Offline, OfflineCode, AlarmSeverity.Critical,
                $"Charge point offline for more than {Math.Floor(offlineFor.TotalMinutes):0} minutes", $"Last seen {since:yyyy-MM-dd HH:mm} UTC", null, null, at);

        public static AlarmSignal BootLoop(int boots, TimeSpan window, DateTime at) =>
            new(AlarmType.BootLoop, BootLoopCode, AlarmSeverity.Major,
                $"Charge point rebooted {boots} times within {window.TotalMinutes:0} minutes", null, null, null, at);

        private static string? Join(params string?[] parts)
        {
            var text = string.Join(" / ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
            return text.Length == 0 ? null : Truncate(text, 1000);
        }

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
    }
}
