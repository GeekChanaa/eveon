
namespace VoltaXApi.Models
{
  public class ReportImage : IEntity
  {
    public int ID { get; set; }
    public int ReportID { get; set; }
    public int ImageID { get; set; }  
    public int ImagePriority { get; set; }
    public Report? Report { get; set; }
    public Image? Image { get; set; }
      public bool IsDeleted { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
    }
}