namespace VoltaXApi.Dtos;

public class CardChangeHistoryDto
{
    public int ID { get; set; }
    public Guid ChangeSetID { get; set; }
    public int CardID { get; set; }
    public int? ChangedByUserID { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
