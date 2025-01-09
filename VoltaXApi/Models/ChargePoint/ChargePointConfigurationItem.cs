

namespace VoltaXApi.Models;

public class ChargePointConfigurationItem
{
  public int ID { get; set; }
  public string ComponentName { get; set; }
  public string VariableName { get; set; }
  public string VariableInstance { get; set; }
  public string VariableUnit { get; set; }
  public double? VariableMinLimit { get; set; }
  public double? VariableMaxLimit { get; set; }
  public string VariableValuesList { get; set; }
  public  List<ConfigurationItemVariableAttribute>? VariableAttributes { get; set; }   
 
}