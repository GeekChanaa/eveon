namespace VoltaXApi.OCPP.Ocpp16
{
    // OCPP 1.6J payloads (camelCase JSON through OCPPMessageFactory.DefaultSettings). Enumerations whose 1.6 values
    // contain dots or dashes (measurand, phase, context, unit...) stay strings.

    public class IdTagInfo
    {
        public string Status { get; set; } = Ocpp16AuthorizationStatus.Invalid;
        public DateTime? ExpiryDate { get; set; }
        public string? ParentIdTag { get; set; }
    }

    public static class Ocpp16AuthorizationStatus
    {
        public const string Accepted = "Accepted";
        public const string Blocked = "Blocked";
        public const string Expired = "Expired";
        public const string Invalid = "Invalid";
        public const string ConcurrentTx = "ConcurrentTx";
    }

    // ---------------------------------------------------------------- charger -> CSMS
    public class BootNotification16Request
    {
        public string ChargePointVendor { get; set; } = "";
        public string ChargePointModel { get; set; } = "";
        public string? ChargePointSerialNumber { get; set; }
        public string? ChargeBoxSerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Iccid { get; set; }
        public string? Imsi { get; set; }
        public string? MeterType { get; set; }
        public string? MeterSerialNumber { get; set; }
    }

    public class BootNotification16Response
    {
        public string Status { get; set; } = "Rejected";
        public DateTime CurrentTime { get; set; }
        public int Interval { get; set; }
    }

    public class Heartbeat16Response
    {
        public DateTime CurrentTime { get; set; }
    }

    public class StatusNotification16Request
    {
        public int ConnectorId { get; set; }
        public string ErrorCode { get; set; } = "NoError";
        public string? Info { get; set; }
        public string Status { get; set; } = "";
        public DateTime? Timestamp { get; set; }
        public string? VendorId { get; set; }
        public string? VendorErrorCode { get; set; }
    }

    public class Authorize16Request
    {
        public string IdTag { get; set; } = "";
    }

    public class Authorize16Response
    {
        public IdTagInfo IdTagInfo { get; set; } = new();
    }

    public class StartTransaction16Request
    {
        public int ConnectorId { get; set; }
        public string IdTag { get; set; } = "";
        public int MeterStart { get; set; }
        public int? ReservationId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class StartTransaction16Response
    {
        public IdTagInfo IdTagInfo { get; set; } = new();
        public int TransactionId { get; set; }
    }

    public class StopTransaction16Request
    {
        public string? IdTag { get; set; }
        public int MeterStop { get; set; }
        public DateTime Timestamp { get; set; }
        public int TransactionId { get; set; }
        public string? Reason { get; set; }
        public List<MeterValue16>? TransactionData { get; set; }
    }

    public class StopTransaction16Response
    {
        public IdTagInfo? IdTagInfo { get; set; }
    }

    public class MeterValues16Request
    {
        public int ConnectorId { get; set; }
        public int? TransactionId { get; set; }
        public List<MeterValue16> MeterValue { get; set; } = new();
    }

    public class MeterValue16
    {
        public DateTime Timestamp { get; set; }
        public List<SampledValue16> SampledValue { get; set; } = new();
    }

    public class SampledValue16
    {
        public string Value { get; set; } = "";
        public string? Context { get; set; }
        public string? Format { get; set; }
        public string? Measurand { get; set; }
        public string? Phase { get; set; }
        public string? Location { get; set; }
        public string? Unit { get; set; }
    }

    public class DataTransfer16Request
    {
        public string VendorId { get; set; } = "";
        public string? MessageId { get; set; }
        public string? Data { get; set; }
    }

    public class DataTransfer16Response
    {
        public string Status { get; set; } = "UnknownVendorId";
        public string? Data { get; set; }
    }

    public class DiagnosticsStatusNotification16Request
    {
        public string Status { get; set; } = "";
    }

    public class FirmwareStatusNotification16Request
    {
        public string Status { get; set; } = "";
    }

    public class Empty16Response
    {
    }

    // ---------------------------------------------------------------- CSMS -> charger
    public class StatusResponse16
    {
        public string Status { get; set; } = "";
    }

    public class RemoteStartTransaction16Request
    {
        public int? ConnectorId { get; set; }
        public string IdTag { get; set; } = "";
        public object? ChargingProfile { get; set; }
    }

    public class RemoteStopTransaction16Request
    {
        public int TransactionId { get; set; }
    }

    public class Reset16Request
    {
        public string Type { get; set; } = "Soft";
    }

    public class UnlockConnector16Request
    {
        public int ConnectorId { get; set; }
    }

    public class ChangeAvailability16Request
    {
        public int ConnectorId { get; set; }
        public string Type { get; set; } = "Operative";
    }

    public class TriggerMessage16Request
    {
        public string RequestedMessage { get; set; } = "";
        public int? ConnectorId { get; set; }
    }

    public class ClearCache16Request
    {
    }

    public class GetLocalListVersion16Request
    {
    }

    public class GetLocalListVersion16Response
    {
        public int ListVersion { get; set; }
    }

    public class SendLocalList16Request
    {
        public int ListVersion { get; set; }
        public List<AuthorizationData16>? LocalAuthorizationList { get; set; }
        public string UpdateType { get; set; } = "Full";
    }

    public class AuthorizationData16
    {
        public string IdTag { get; set; } = "";
        public IdTagInfo? IdTagInfo { get; set; }
    }

    public class UpdateFirmware16Request
    {
        public string Location { get; set; } = "";
        public int? Retries { get; set; }
        public DateTime RetrieveDate { get; set; }
        public int? RetryInterval { get; set; }
    }

    public class GetDiagnostics16Request
    {
        public string Location { get; set; } = "";
        public int? Retries { get; set; }
        public int? RetryInterval { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? StopTime { get; set; }
    }

    public class GetDiagnostics16Response
    {
        public string? FileName { get; set; }
    }

    public class ChangeConfiguration16Request
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class GetConfiguration16Request
    {
        public List<string>? Key { get; set; }
    }

    public class GetConfiguration16Response
    {
        public List<KeyValue16>? ConfigurationKey { get; set; }
        public List<string>? UnknownKey { get; set; }
    }

    public class KeyValue16
    {
        public string Key { get; set; } = "";
        public bool Readonly { get; set; }
        public string? Value { get; set; }
    }

    public class ReserveNow16Request
    {
        public int ConnectorId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string IdTag { get; set; } = "";
        public string? ParentIdTag { get; set; }
        public int ReservationId { get; set; }
    }

    public class CancelReservation16Request
    {
        public int ReservationId { get; set; }
    }

    // ---------------------------------------------------------------- dashboard API bodies
    public class ChangeConfigurationDto
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class GetConfigurationDto
    {
        public List<string>? Keys { get; set; }
    }
}
