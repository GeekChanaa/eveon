

namespace VoltaXApi.Models
{
  public class SupportedKwh
  {
    public int ID { get; set; }
    public double Value { get; set; }
    public int ChargePointModelID { get; set; }
    public ChargePointModel? ChargePointModel { get; set; }
  }
}