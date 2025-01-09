

namespace VoltaXApi.Models
{
  public class SystemReportCommentImage
  {
    public int ID { get; set; }
    public int ImageID { get; set; }
    public int SystemReportCommentID { get; set; }
    public Image? Image { get; set; }
    public SystemReportComment? SystemReportComment { get; set; }
  }
}