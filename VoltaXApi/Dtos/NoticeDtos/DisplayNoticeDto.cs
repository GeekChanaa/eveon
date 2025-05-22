using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class DisplayNoticeDto
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public bool IsEmail { get; set; }
        public bool IsSms { get; set; }
        public bool IsPushNotification { get; set; }
        public bool ForAdmins { get; set; }
        public bool ForSupports { get; set; }
        public bool ForPartners { get; set; }
        public bool ForUsers { get; set; }
        public string Text { get; set; }
        public string? EmailTemplatePath { get; set; }
    }
    
}