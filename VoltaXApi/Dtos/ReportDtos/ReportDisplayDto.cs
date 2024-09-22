using System;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ReportDisplayDto
    {
        public int ID { get; set; } 
        public int UserID { get; set; }
        public string? UserName { get; set; } 
        public int? ConnectorID { get; set; }
        public int? ChargePointID { get; set; }
        public string? ConnectorName { get; set; }
        public string? ChargePointName { get; set; }
        public ReportTypeEnum ReportType { get; set; } 
        public ReportCategoryEnum ReportCategory { get; set; } 
        public string IssueDescription { get; set; } 
        public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
        public DateTime ReportDate { get; set; } = new DateTime();
        public DateTime? ResolvedDate { get; set; } = null;
    }
}
