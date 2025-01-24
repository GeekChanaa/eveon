
namespace VoltaXApi.Models;

public class OcppVariableComponent
{
  public int ID { get; set; }
  public string Component { get; set; }
  public string Variable { get; set; }
  public string? Instance { get; set; }
  public string? Unit { get; set; }
  public string? DataType { get; set; }
  public string? Description { get; set; }
  public bool Required { get; set; }

}