namespace VoltaXApi.Models
{
  public class ReportImage
  {
    public int ID { get; set; }
    public int ReportID { get; set; }
    public int ImageID { get; set; }  
    public int ImagePriority { get; set; }
    public Report? Report { get; set; }
    public Image? Image { get; set; }
  }
}