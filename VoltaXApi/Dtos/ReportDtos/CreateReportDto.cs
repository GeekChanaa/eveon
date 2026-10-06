using System.ComponentModel.DataAnnotations;
using System;

namespace VoltaXApi.Models
{
    public class CreateReportDto
    {
        public int UserID { get; set; } 
        public int? ConnectorID { get; set; }
        public int? ChargePointID { get; set; }
        [EnumDataType(typeof(ReportTypeEnum))]
        public ReportTypeEnum ReportType { get; set; } 
        public bool IsEmail { get; set; } 
        public bool IsNotification { get; set; } 
        [EnumDataType(typeof(ReportCategoryEnum))]
        public ReportCategoryEnum ReportCategory { get; set; } 
        [Required, StringLength(2000)]
        public string IssueDescription { get; set; }
        [EnumDataType(typeof(ReportStatusEnum))]
        public ReportStatusEnum Status { get; set; }
        public DateTime? ReportDate { get; set; } = new DateTime();
    }
}
