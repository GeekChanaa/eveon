

namespace VoltaXApi.Models;

public class LoginAttempt
{
    public int Id { get; set; }
    public string? IpAddress { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockoutEndTime { get; set; }
}