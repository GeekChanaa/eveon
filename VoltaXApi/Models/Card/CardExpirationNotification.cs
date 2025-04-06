namespace VoltaXApi.Models;

public class CardExpirationNotification : IEntity
{
    public int ID { get; set; }
    public int CardID { get; set; }
    public string IntervalName { get; set; }
    public DateTime SentDate { get; set; }
    public Card? Card { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}