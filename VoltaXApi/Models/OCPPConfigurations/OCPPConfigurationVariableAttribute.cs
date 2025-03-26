using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models;
public class OCPPConfigurationVariableAttribute: IEntity
{
    public int ID { get; set; }
    public AttributeEnumType? Type { get; set; } = AttributeEnumType.Actual;
    public string? Value { get; set; }
    public MutabilityEnumType? Mutability { get; set; } = MutabilityEnumType.ReadWrite;
    public bool? Persistent { get; set; } = false;
    public bool? Constant { get; set; } = false;    
    public int OCPPConfigurationItemID { get; set; }
    public OCPPConfigurationItem? ConfigurationItem { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}