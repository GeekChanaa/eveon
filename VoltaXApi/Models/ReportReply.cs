
namespace VoltaXApi.Models
{
  public class ReportReply : IEntity
  {
    public int ID { get; set; } 
    public int ReportID { get; set; }
    public string Reply { get; set; }
    public Report? Report { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}