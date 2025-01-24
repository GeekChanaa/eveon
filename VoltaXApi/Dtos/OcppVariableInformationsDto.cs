namespace VoltaXApi.Dtos;

public class OcppVariableInformationsDto
{
  public int ID { get; set; }
  public string Name { get; set; }
  public string DataType { get; set; }
  public string Unit { get; set; }
  public string Description { get; set; }
  public List<OcppComponentListDto>? AssociatedComponents { get; set; }
}