
using VoltaXApi.Models;

namespace VoltaXApi.Factories;

public class MailRequestFactory : IMailRequestFactory
{
    public MailRequestFactory()
    {
    }

    public MailRequest CreateResetPasswordMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Password Reset Request – VoltaX Charging"
        };
    }

    public MailRequest CreateVerificationMailRequest(string to)
    {
        return new MailRequest
        {
            Phone = "",
            Email = "support@voltaxcharging.com",
            Name = "CHANAA mohammed",
            ToEmails = new List<string>() { to },
            Subject = "Complete Your Registration – Verify Your Email",
            Body = ""
        };
    }

    public MailRequest CreateLoginFailedAttemptMailRequest(string to)
    {
        return new MailRequest
        {
            Phone = "",
            Email = "support@voltaxcharging.com",
            Name = "CHANAA mohammed",
            ToEmails = new List<string>() { to },
            Subject = "Security Alert: New Login Attempt Detected on Your VoltaX Charging Account",
            Body = ""
        };
    }

}