namespace VoltaXApi.Dtos;

public class OcppComponentInformationsDto
{
  public int ID { get; set; }
  public string Component { get; set; }
  public string Description { get; set; }
  public List<OcppVariableListDto>? AssociatedVariables { get; set; }
}