
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models;

public class ConfigurationItemVariableAttribute
{
  public int ID { get; set; }
  public DataEnumType DataType { get; set; }
  public AttributeEnumType Type { get; set; }
  public string Value { get; set; }
  public MutabilityEnumType? Mutability { get; set; }
  public bool? Persistent { get; set; }
  public bool? Constant { get; set; }
  public int ChargePointConfigurationItemID { get; set; }
  public ChargePointConfigurationItem ChargePointConfigurationItem { get; set; }
    
}