using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    /// <summary>A connected charge point that has never been provisioned.</summary>
    public class PendingProvisioningChargePointDto
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; } = "";
        public string? StationName { get; set; }
        public string? VendorName { get; set; }
        public string? ModelName { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
    }

    /// <summary>One device-model setting, as edited in the dashboard and sent with SetVariables.</summary>
    public class ProvisioningVariableDto
    {
        [MaxLength(50)]
        public string GroupName { get; set; } = "General";
        [Required, MaxLength(50)]
        public string ComponentName { get; set; } = "";
        [MaxLength(50)]
        public string? ComponentInstance { get; set; }
        public int? EvseId { get; set; }
        public int? ConnectorId { get; set; }
        [Required, MaxLength(50)]
        public string VariableName { get; set; } = "";
        [MaxLength(50)]
        public string? VariableInstance { get; set; }
        /// <summary>Actual, Target, MinSet or MaxSet.</summary>
        public string AttributeType { get; set; } = "Actual";
        [Required(AllowEmptyStrings = true), MaxLength(1000)]
        public string Value { get; set; } = "";
        [MaxLength(500)]
        public string? Description { get; set; }
    }

    /// <summary>A profile row with what the charger reported about it during discovery.</summary>
    public class ProvisioningPlanVariableDto : ProvisioningVariableDto
    {
        /// <summary>Writable, ReadOnly, NotReported (discovery ran but the charger does not list it) or Unknown (no discovery data).</summary>
        public string Support { get; set; } = "Unknown";
        public string? CurrentValue { get; set; }
        public string? Mutability { get; set; }
        public string? DataType { get; set; }
        public string? Unit { get; set; }
        public string? ValuesList { get; set; }
        public double? MinLimit { get; set; }
        public double? MaxLimit { get; set; }
    }

    public class ProvisioningPlanDto
    {
        public string ChargePointId { get; set; } = "";
        /// <summary>Completed, NotSupported, Rejected, TimedOut or Failed.</summary>
        public string DiscoveryStatus { get; set; } = "Completed";
        public string? DiscoveryMessage { get; set; }
        public int ReportedVariableCount { get; set; }
        public int ItemsPerMessage { get; set; }
        public List<ProvisioningPlanVariableDto> Variables { get; set; } = new();
    }

    public class ApplyProvisioningDto
    {
        /// <summary>Automatic or Manual.</summary>
        public string Method { get; set; } = "Automatic";
        [Required, MinLength(1)]
        public List<ProvisioningVariableDto> Variables { get; set; } = new();
    }

    public class ProvisioningVariableResultDto : ProvisioningVariableDto
    {
        /// <summary>A SetVariableStatus (Accepted, Rejected, UnknownComponent, …), NoResponse or CallError.</summary>
        public string Status { get; set; } = "";
        public string? StatusReason { get; set; }
    }

    public class ProvisioningResultDto
    {
        public bool Provisioned { get; set; }
        public int AcceptedCount { get; set; }
        public int FailedCount { get; set; }
        public bool RebootRequired { get; set; }
        /// <summary>Reset or TriggerMessage(BootNotification) outcome, for the operator.</summary>
        public string? FollowUp { get; set; }
        public string? Message { get; set; }
        public List<ProvisioningVariableResultDto> Results { get; set; } = new();
    }

    /// <summary>A charge point's connection and configuration state, for the configuration pages.</summary>
    public class ChargePointConfigurationStatusDto
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; } = "";
        public string? StationName { get; set; }
        public string? VendorName { get; set; }
        public string? ModelName { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public bool IsOnline { get; set; }
        /// <summary>Our system sent it its configuration (Automatic or Manual).</summary>
        public bool IsConfigured { get; set; }
        /// <summary>Accepted on BootNotification (configured, skipped or legacy). False: kept Pending.</summary>
        public bool IsAccepted { get; set; }
        /// <summary>Automatic, Manual, Skipped or Legacy.</summary>
        public ChargePointProvisioningMethodEnum? ConfigurationMethod { get; set; }
        /// <summary>Provisioned or AwaitingReboot.</summary>
        public ChargePointProvisioningStatusEnum? ConfigurationStatus { get; set; }
        public DateTime? ConfiguredAt { get; set; }
        public string? ConfiguredBy { get; set; }
        public int AcceptedCount { get; set; }
        public int FailedCount { get; set; }
        public int ReportedVariableCount { get; set; }
        public DateTime? LastReportAt { get; set; }
    }

    public class DeviceModelAttributeDto
    {
        /// <summary>Actual, Target, MinSet or MaxSet.</summary>
        public string Type { get; set; } = "Actual";
        public string? Value { get; set; }
        /// <summary>ReadOnly, WriteOnly or ReadWrite.</summary>
        public string Mutability { get; set; } = "ReadWrite";
        public bool Persistent { get; set; }
        public bool Constant { get; set; }
    }

    /// <summary>One component/variable of the charger's device model, as reported by NotifyReport.</summary>
    public class DeviceModelVariableDto
    {
        public string ComponentName { get; set; } = "";
        public string? ComponentInstance { get; set; }
        public int? EvseId { get; set; }
        public int? ConnectorId { get; set; }
        public string VariableName { get; set; } = "";
        public string? VariableInstance { get; set; }
        public string? DataType { get; set; }
        public string? Unit { get; set; }
        public double? MinLimit { get; set; }
        public double? MaxLimit { get; set; }
        public string? ValuesList { get; set; }
        public bool SupportsMonitoring { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<DeviceModelAttributeDto> Attributes { get; set; } = new();
    }

    public class DeviceModelReportRequestDto
    {
        /// <summary>FullInventory, ConfigurationInventory or SummaryInventory.</summary>
        public string ReportBase { get; set; } = "FullInventory";
    }

    public class DeviceModelReportResultDto
    {
        public string ReportBase { get; set; } = "FullInventory";
        /// <summary>Completed, NotSupported, Rejected, TimedOut or Failed.</summary>
        public string Status { get; set; } = "Completed";
        public string? Message { get; set; }
        public int ReportedVariableCount { get; set; }
    }

    public class DeviceModelVariablesRequestDto
    {
        [Required, MinLength(1)]
        public List<ProvisioningVariableDto> Variables { get; set; } = new();
    }

    public class DeviceModelSetResultDto
    {
        public bool AnyAnswer { get; set; }
        public int AcceptedCount { get; set; }
        public int FailedCount { get; set; }
        public bool RebootRequired { get; set; }
        public List<ProvisioningVariableResultDto> Results { get; set; } = new();
    }

    public class DeviceModelRebootRequestDto
    {
        /// <summary>False waits until no session is running (OnIdle).</summary>
        public bool Immediate { get; set; }
    }

    public class OcppDefaultVariableDto : ProvisioningVariableDto
    {
        public int ID { get; set; }
        public bool Enabled { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
