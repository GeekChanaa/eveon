

namespace VoltaXApi.Models
{
  public class SystemReport : IEntity
  {
    public int ID { get; set; }
    public ReportCategoryEnum ReportCategory { get; set; } 
    public int? UserID { get; set; } 
    public int? CardID { get; set; } 
    public int? ConnectorID { get; set; }
    public int? ChargePointID { get; set; }
    public int? ResolvedByID { get; set; }
    public int? AssignedID { get; set; }
    public string IssueDescription { get; set; } 
    public bool IsEmail { get; set; } 
    public bool IsNotification { get; set; } 
    public Connector? Connector { get; set; }
    public ChargePoint? ChargePoint { get; set; }
    public User? User { get; set; }
    public User? Assigned { get; set; }
    public User? Resolved { get; set; }
    public Card? Card { get; set; }
    public ICollection<SystemReportImage>? Images { get; set; }
    public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
    public ReportCriticality Criticality { get; set; } = ReportCriticality.Informational;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}