using VoltaXApi.Models;

namespace VoltaXApi.Factories;

public interface IMailRequestFactory
{
    MailRequest CreateResetPasswordMailRequest(string to);
    MailRequest CreatePasswordResetPasswordMailRequest(string to);
    MailRequest CreateChangedPasswordMailRequest(string to);
    MailRequest CreateChangedEmailMailRequest(string to);
    MailRequest CreateVerificationMailRequest(string to);
    MailRequest CreateLoginFailedAttemptMailRequest(string to);
    MailRequest CreateApprovedDownloadInfoRequest(string to);
    MailRequest CreateDeniedDownloadInfoRequest(string to);
    MailRequest CreateWelcomeMailRequest(string to);
    MailRequest CreateChargingSessionQuoteMail(string to);
    MailRequest CreateAccountSuspendedMailRequest(string to);
    
}