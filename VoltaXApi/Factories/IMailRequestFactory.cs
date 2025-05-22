using VoltaXApi.Models;

namespace VoltaXApi.Factories;

public interface IMailRequestFactory
{
    MailRequest CreateResetPasswordMailRequest(string to);
    MailRequest CreateVerificationMailRequest(string to);
    MailRequest CreateLoginFailedAttemptMailRequest(string to);
}