

namespace VoltaXApi.Models
{
  public class ChargePointFeatures
  {
    public int ID { get; set; }
    public int ChargePointModelID { get; set; }
    public ChargePointModel? Model { get; set; }
    public bool AutoSchedules { get; set; }
    public bool AutoStart { get; set; }
    public bool LocalAuthListManagement { get; set; }
    public bool? Powerbank { get; set; }
    public bool? ReleaseDetection { get; set; }
    public bool? AutoCharge { get; set; }
    public bool? LoadBalancing { get; set; }
    public bool? FirmwareManagement { get; set; }
    public bool? SolarCharge { get; set; }
  }

}