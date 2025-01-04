
using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class SystemReportDisplayDto
{
  public int ID { get; set; }
    public ReportCategoryEnum ReportCategory { get; set; } 
    public int? UserID { get; set; } 
    public string? UserName { get; set; } 
    public int? CardID { get; set; } 
    public string? CardNumber { get; set; }
    public int? ConnectorID { get; set; }
    public int? ChargePointID { get; set; }
    public string? ChargePointName { get; set; }
    public int? ResolvedByID { get; set; }
    public string? ResolvedUserName { get; set; }
    public int? AssignedID { get; set; }
    public string? AssignedUserName { get; set; }
    public string IssueDescription { get; set; } 
    public bool IsEmail { get; set; } 
    public bool IsNotification { get; set; } 
    public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
    public ReportCriticality Criticality { get; set; } = ReportCriticality.Informational;
}