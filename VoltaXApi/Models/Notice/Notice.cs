namespace VoltaXApi.Models;

public class Notice : IEntity
{
    public int ID { get; set; }
    public NoticeTypeEnum Type { get; set; }
    public string? EmailTemplatePath { get; set; }
    public string Title { get; set; }
    public bool IsEmail { get; set; }
    public bool IsSms { get; set; }
    public bool IsPushNotification { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool ForAdmins { get; set; }
    public bool ForSupports { get; set; }
    public bool ForPartners { get; set; }
    public bool ForUsers { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}