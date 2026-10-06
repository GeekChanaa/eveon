namespace VoltaXApi.Dtos
{
    public class NotificationDto
    {
        public int ID { get; set; }
        public string? Type { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
        public string Url { get; set; } = "";
        public bool Urgent { get; set; }
        public bool Read { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationListDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }
}
