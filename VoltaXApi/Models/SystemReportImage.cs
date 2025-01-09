

namespace VoltaXApi.Models
{
  public class SystemReportImage
  {
    public int ID { get; set; }
    public int ImageID { get; set; }
    public int SystemReportID { get; set; }
    public Image? Image { get; set; }
    public SystemReport? SystemReport { get; set; }
  }
}