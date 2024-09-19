using System;

namespace VoltaXApi.Models
{
    public class Report
    {
        public int ID { get; set; } 
        public long UserID { get; set; } 
        public int? ConnectorID { get; set; }
        public int? ChargePointID { get; set; }
        public ReportTypeEnum ReportType { get; set; } 
        public ReportCategoryEnum EntityId { get; set; } 
        public string IssueDescription { get; set; } 
        public ReportStatusEnum Status { get; set; } 
        public DateTime ReportDate { get; set; } 
        public DateTime? ResolvedDate { get; set; } 
        public Connector? Connector { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public User? User { get; set; }

    }
}
