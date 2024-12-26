namespace VoltaXApi.Dtos
{
  public class ConnectorSelectDto
  {
    public int ID { get; set; }
    public int? ConnectorID { get; set; }
    public int EvseID { get; set; }
    public string ChargePointID { get; set; }
  }
}