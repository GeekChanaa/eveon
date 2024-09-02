using System;
using VoltaXApi.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace VoltaXApi.Services
{
    public interface IMailService
    {
        Task SendVerificationEmailAsync(MailRequest mailRequest,string verificationLink);
        Task SendEmailAsync(MailRequest mailRequest);
    }
}