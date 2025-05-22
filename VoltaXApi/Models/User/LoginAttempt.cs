

namespace VoltaXApi.Models;

public class LoginAttempt : IEntity
{
    public int ID { get; set; }
    public string? IpAddress { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime LockoutEndTime { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}