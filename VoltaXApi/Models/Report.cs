using System;

namespace VoltaXApi.Models
{
    public class Report : IEntity
    {
        public int ID { get; set; } 
        public int UserID { get; set; } 
        public int? ConnectorID { get; set; }
        public int? ChargePointID { get; set; }
        public ReportTypeEnum ReportType { get; set; } 
        public ReportCategoryEnum ReportCategory { get; set; } 
        public string IssueDescription { get; set; } 
        public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
        public bool IsEmail { get; set; } 
        public bool IsNotification { get; set; } 
        public DateTime ReportDate { get; set; } = new DateTime();
        public DateTime? ResolvedDate { get; set; } = null;
        public Connector? Connector { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public User? User { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
