using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models;

public class CardChangeHistory
{
    public int ID { get; set; }
    public Guid ChangeSetID { get; set; }
    public int CardID { get; set; }
    public Card Card { get; set; } = null!;
    public int? ChangedByUserID { get; set; }
    public User? ChangedByUser { get; set; }

    [MaxLength(200)]
    public string ChangedByName { get; set; } = "System";

    [MaxLength(50)]
    public string Source { get; set; } = "System";

    [MaxLength(100)]
    public string PropertyName { get; set; } = string.Empty;

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
