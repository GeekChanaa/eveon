

namespace VoltaXApi.Models;

public class SystemReportCommentDisplayDto
{
  
  public int ID { get; set; } 
  public string Content { get; set; } 
  public string UserName { get; set; } 
  public SystemReport? SystemReport { get; set; }
  public ICollection<string>? Images {get; set;}
}