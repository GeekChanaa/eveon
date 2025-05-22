public class LoginAttemptFailedException : Exception
{
    public string Email { get; }
    public DateTime LockoutEndTime { get; }

    public LoginAttemptFailedException(string email, DateTime lockoutEndTime)
        : base($"Too many failed login attempts. Account locked until {lockoutEndTime}")
    {
        Email = email;
        LockoutEndTime = lockoutEndTime;
    }
}