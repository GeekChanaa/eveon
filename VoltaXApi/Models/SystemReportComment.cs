

namespace VoltaXApi.Models;

public class SystemReportComment : IEntity
{
  public int ID { get; set; }
  public int SystemReportID { get; set; } 
  public string Content { get; set; } 
  public SystemReport? SystemReport { get; set; }
  public ICollection<SystemReportCommentImage> SystemReportCommentImages {get; set;}
  public bool IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}