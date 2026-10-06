public class LoginAttemptFailedException : Exception
{
    public string Email { get; }

    // The lockout end is deliberately left out of the message: it reaches the client.
    public LoginAttemptFailedException(string email)
        : base("Too many failed attempts")
    {
        Email = email;
    }
}
