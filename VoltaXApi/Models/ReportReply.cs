namespace VoltaXApi.Models
{
  public class ReportReply
  {
    public int ID { get; set; } 
    public int ReportID { get; set; }
    public string Reply { get; set; }
    public Report? Report { get; set; }
  }
}