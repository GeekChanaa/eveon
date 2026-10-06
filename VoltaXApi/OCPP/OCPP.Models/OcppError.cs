namespace VoltaXApi.OCPP.Models
{
    /// <summary>OCPP-J CALLERROR codes. The wire name can differ per protocol version, see <see cref="OcppErrors.ToWireName"/>.</summary>
    public enum OcppError
    {
        NotImplemented,
        NotSupported,
        InternalError,
        ProtocolError,
        SecurityError,
        FormationViolation,
        PropertyConstraintViolation,
        OccurrenceConstraintViolation,
        TypeConstraintViolation,
        GenericError,
        MessageTypeNotSupported,
        RpcFrameworkError
    }

    public static class OcppErrors
    {
        /// <summary>
        /// OCPP 2.0.1 spells two codes differently from 1.6 ("FormatViolation", "OccurrenceConstraintViolation")
        /// and adds MessageTypeNotSupported / RpcFrameworkError, which 1.6 does not know.
        /// </summary>
        public static string ToWireName(OcppError error, string? protocolVersion)
        {
            if (protocolVersion == OcppProtocols.Ocpp16)
            {
                return error switch
                {
                    OcppError.OccurrenceConstraintViolation => "OccurenceConstraintViolation",
                    OcppError.RpcFrameworkError => "FormationViolation",
                    OcppError.MessageTypeNotSupported => "ProtocolError",
                    _ => error.ToString()
                };
            }

            return error switch
            {
                OcppError.FormationViolation => "FormatViolation",
                _ => error.ToString()
            };
        }

        /// <summary>Maps a code received on the wire or returned by a handler (any version's spelling) to the enum.</summary>
        public static OcppError Parse(string? code)
        {
            switch (code?.Trim())
            {
                case "FormatViolation":
                case "FormationViolation":
                    return OcppError.FormationViolation;
                case "OccurenceConstraintViolation":
                case "OccurrenceConstraintViolation":
                    return OcppError.OccurrenceConstraintViolation;
            }
            return Enum.TryParse<OcppError>(code, false, out var parsed) ? parsed : OcppError.GenericError;
        }

        public static string DefaultDescription(OcppError error) => error switch
        {
            OcppError.NotImplemented => "Requested Action is not known by receiver.",
            OcppError.NotSupported => "Requested Action is recognized but not supported by the receiver.",
            OcppError.InternalError => "An internal error occurred and the receiver was not able to process the requested Action successfully.",
            OcppError.ProtocolError => "Payload for Action is incomplete.",
            OcppError.SecurityError => "A security issue occurred preventing the receiver from completing the Action successfully.",
            OcppError.FormationViolation => "Payload for Action is syntactically incorrect.",
            OcppError.PropertyConstraintViolation => "Payload is syntactically correct but at least one field contains an invalid value.",
            OcppError.OccurrenceConstraintViolation => "Payload for Action is syntactically correct but at least one of the fields violates occurrence constraints.",
            OcppError.TypeConstraintViolation => "Payload for Action is syntactically correct but at least one of the fields violates data type constraints.",
            OcppError.MessageTypeNotSupported => "A message with a Message Type Number received that is not supported by this implementation.",
            OcppError.RpcFrameworkError => "Content of the call is not a valid RPC Request.",
            _ => "Any other error not covered by the more specific error codes."
        };
    }

    public static class OcppProtocols
    {
        public const string Ocpp16 = "ocpp1.6";
        public const string Ocpp201 = "ocpp2.0.1";

        // CSMS -> charging station CALLs defined by each version (1.6: core + reservation, local list,
        // firmware, remote trigger and smart charging profiles; the 1.6 security extension is not supported).
        private static readonly HashSet<string> Outbound16 = new(StringComparer.Ordinal)
        {
            "CancelReservation", "ChangeAvailability", "ChangeConfiguration", "ClearCache", "ClearChargingProfile",
            "DataTransfer", "GetCompositeSchedule", "GetConfiguration", "GetDiagnostics", "GetLocalListVersion",
            "RemoteStartTransaction", "RemoteStopTransaction", "ReserveNow", "Reset", "SendLocalList",
            "SetChargingProfile", "TriggerMessage", "UnlockConnector", "UpdateFirmware",
        };

        private static readonly HashSet<string> Outbound201 = new(StringComparer.Ordinal)
        {
            "CancelReservation", "CertificateSigned", "ChangeAvailability", "ClearCache", "ClearChargingProfile",
            "ClearDisplayMessage", "ClearVariableMonitoring", "CostUpdated", "CustomerInformation", "DataTransfer",
            "DeleteCertificate", "GetBaseReport", "GetChargingProfiles", "GetCompositeSchedule", "GetDisplayMessages",
            "GetInstalledCertificateIds", "GetLocalListVersion", "GetLog", "GetMonitoringReport", "GetReport",
            "GetTransactionStatus", "GetVariables", "InstallCertificate", "PublishFirmware", "RequestStartTransaction",
            "RequestStopTransaction", "ReserveNow", "Reset", "SendLocalList", "SetChargingProfile", "SetDisplayMessage",
            "SetMonitoringBase", "SetMonitoringLevel", "SetNetworkProfile", "SetVariableMonitoring", "SetVariables",
            "TriggerMessage", "UnlockConnector", "UnpublishFirmware", "UpdateFirmware",
        };

        /// <summary>True when <paramref name="action"/> is a CSMS-initiated message of that protocol version.</summary>
        public static bool SupportsOutbound(string protocolVersion, string action) => protocolVersion switch
        {
            Ocpp16 => Outbound16.Contains(action),
            Ocpp201 => Outbound201.Contains(action),
            _ => true,
        };

        /// <summary>Preference order when a charger offers several sub-protocols.</summary>
        public static readonly string[] PreferenceOrder = { Ocpp201, Ocpp16 };
    }
}
