using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CreateNoticeDto
    {
        [EnumDataType(typeof(NoticeTypeEnum))]
        public NoticeTypeEnum Type { get; set; }
        [StringLength(200)]
        public string? Title { get; set; }
        public bool IsEmail { get; set; }
        public bool IsSms { get; set; }
        public bool IsPushNotification { get; set; }
        public bool ForAdmins { get; set; }
        public bool ForSupports { get; set; }
        public bool ForPartners { get; set; }
        public bool ForUsers { get; set; }
        [StringLength(5000)]
        public string? Text { get; set; }
        public IFormFile? EmailTemplate { get; set; }
    }
    
}