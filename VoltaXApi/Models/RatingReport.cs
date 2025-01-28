using System;

namespace VoltaXApi.Models
{
    public class RatingReport : IEntity
    {
        public int ID { get; set; } 
        public int UserID { get; set; } 
        public int? RatingID { get; set; }
        public RatingReportCategoryEnum ReportCategory { get; set; } 
        public string IssueDescription { get; set; } 
        public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
        public Rating? Rating { get; set; }
        public User? User { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
