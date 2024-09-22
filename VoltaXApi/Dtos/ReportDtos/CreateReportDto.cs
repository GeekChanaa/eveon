using System;

namespace VoltaXApi.Models
{
    public class CreateReportDto
    {
        public int UserID { get; set; } 
        public int? ConnectorID { get; set; }
        public int? ChargePointID { get; set; }
        public ReportTypeEnum ReportType { get; set; } 
        public ReportCategoryEnum ReportCategory { get; set; } 
        public string IssueDescription { get; set; }
        public ReportStatusEnum Status { get; set; }
        public DateTime? ReportDate { get; set; } = new DateTime();
    }
}
