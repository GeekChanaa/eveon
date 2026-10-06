using System.ComponentModel.DataAnnotations;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models;

/// <summary>
/// One OCPP 2.0.1 device-model setting of the default provisioning profile, applied to a charge
/// point with SetVariables the first time it connects.
/// </summary>
public class OcppDefaultVariable : IEntity
{
    public int ID { get; set; }

    /// <summary>Display grouping only (Transactions, Authorization, …).</summary>
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

    public AttributeEnumType AttributeType { get; set; } = AttributeEnumType.Actual;

    [Required, MaxLength(1000)]
    public string Value { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Disabled rows stay in the profile but are not sent.</summary>
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
