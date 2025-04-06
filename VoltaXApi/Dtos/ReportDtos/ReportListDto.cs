

using VoltaXApi.Models;

namespace VoltaxApi.Dtos;

public class ReportListDto
{
    public int ID { get; set; } 
    public string UserName { get; set; } 
    public string? ConnectorName { get; set; }
    public string? ChargePointName { get; set; }
    public ReportTypeEnum ReportType { get; set; } 
    public bool IsEmail { get; set; } 
    public bool IsNotification { get; set; } 
    public ReportCategoryEnum ReportCategory { get; set; } 
    public string IssueDescription { get; set; } 
    public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
    public DateTime ReportDate { get; set; } = new DateTime();
    public DateTime? ResolvedDate { get; set; } = null;

}