using System;
using VoltaXApi.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IMailService
    {
        Task SendVerificationEmailAsync(MailRequest mailRequest, string verificationLink, string userName);
        Task SendVerificationCodeEmailAsync(MailRequest mailRequest, string verificationCode, string userName);
        Task SendEmailAsync(MailRequest mailRequest);
        Task SendReportEmailToAdmin(SystemReport report);
        Task SendReportEmailToSupport(int reportID);
        Task SendNoticeEmail(MailRequest mailRequest, string noticeText);
        Task SendWarningEmail(MailRequest mailRequest, string userName);
        Task SendWelcomeEmail(MailRequest mailRequest, string userName);
        Task SendDebitCardRemovedEmail(MailRequest mailRequest, string userName);
        Task SendDebitCardAddedEmail(MailRequest mailRequest, string userName);
        Task SendPasswordChangedMail(MailRequest mailRequest, string userName);
        Task SendDownloadInfoRequestApproved(MailRequest mailRequest, string userName);
        Task SendDownloadInfoRequestDenied(MailRequest mailRequest, string userName);
        Task SendLoginAttemptFailedEmail(MailRequest mailRequest, string userName, string ipAddress, string resetPasswordLink);
        Task SendResetPasswordMailRequest(MailRequest mailRequest, string userName, string resetPasswordLink);
        Task SendResetPasswordForMobileMailRequest(MailRequest mailRequest, string userName, string resetCode);
        Task SendPartnerResetPasswordMailRequest(MailRequest mailRequest, string userName, string password);
        Task SendChargingSessionQuoteMailRequest(MailRequest mailRequest, ChargingSessionForMailDto chargingSession);
        Task SendSuspendedAccountMail(MailRequest mailRequest, UserSuspendedForMailDto userDto);
        // Task SendLoginAttemptEmail()
    }
}