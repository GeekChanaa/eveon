
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

    public MailRequest CreatePasswordResetPasswordMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Reset Password Request – VoltaX Charging PRO"
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

    public MailRequest CreateApprovedDownloadInfoRequest(string to)
    {
        return new MailRequest
        {
            Phone = "",
            Email = "support@voltaxcharging.com",
            Name = "CHANAA mohammed",
            ToEmails = new List<string>() { to },
            Subject = "VoltaX Charging - Your Request to Download Personal Information Has Been Approved",
            Body = ""
        };
    }

    public MailRequest CreateDeniedDownloadInfoRequest(string to)
    {
        return new MailRequest
        {
            Phone = "",
            Email = "support@voltaxcharging.com",
            Name = "CHANAA mohammed",
            ToEmails = new List<string>() { to },
            Subject = "VoltaX Charging - Your Request to Download Personal Information Was Denied",
            Body = ""
        };
    }

    public MailRequest CreateChangedPasswordMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Password Changed – VoltaX Charging"
        };
    }

    public MailRequest CreateChangedEmailMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Email Changed – VoltaX Charging"
        };
    }

    public MailRequest CreateWelcomeMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Welcome to VolaX ! – VoltaX Charging"
        };
    }

    public MailRequest CreateDebitCardRemovedMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Debit Card Removed  – VoltaX Charging"
        };
    }

    public MailRequest CreateChargingSessionQuoteMail(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "Charging Session Completed  – VoltaX Charging"
        };
    }

    public MailRequest CreateDebitCardAddedMailRequest(string to)
    {
        return new MailRequest
        {
            Email = "no-reply@voltaxcharging.com",
            Name = "VoltaX Charging",
            ToEmails = new List<string> { to },
            Subject = "New Debit Card Added – VoltaX Charging"
        };
    }
}