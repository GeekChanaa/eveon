using VoltaXApi.Settings;
using VoltaXApi.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO;
using System;
using MailKit;
using VoltaXApi.EmailTemplates;

namespace VoltaXApi.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        public MailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
            email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
            email.Subject = mailRequest.Subject;
            var builder = new BodyBuilder();
            if (mailRequest.Attachments != null)
            {
                byte[] fileBytes;
                foreach (var file in mailRequest.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            file.CopyTo(ms);
                            fileBytes = ms.ToArray();
                        }
                        builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                    }
                }
            }
            builder.HtmlBody = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>A simple, clean, and responsive HTML invoice template</title><style>.invoice-box {max-width: 800px;margin: auto;padding: 30px;border: 1px solid #eee;box-shadow: 0 0 10px rgba(0, 0, 0, .15);font-size: 16px;line-height: 24px;font-family: 'Helvetica Neue', 'Helvetica', Helvetica, Arial, sans-serif;color: #555;}.invoice-box table {width: 100%;line-height: inherit;text-align: left;}.invoice-box table td {padding: 5px;vertical-align: top;}.invoice-box table tr td:nth-child(2) {text-align: right;}.invoice-box table tr.top table td {padding-bottom: 20px;}.invoice-box table tr.top table td.title {font-size: 45px;line-height: 45px;color: #333;}.invoice-box table tr.information table td {padding-bottom: 40px;}.invoice-box table tr.heading td {background: #eee;border-bottom: 1px solid #ddd;font-weight: bold;}.invoice-box table tr.details td {padding-bottom: 20px;}.invoice-box table tr.item td{border-bottom: 1px solid #eee;}.invoice-box table tr.item.last td {border-bottom: none;}.invoice-box table tr.total td:nth-child(2) {border-top: 2px solid #eee;font-weight: bold;}@media only screen and (max-width: 600px) {.invoice-box table tr.top table td {width: 100%;display: block;text-align: center;}.invoice-box table tr.information table td {width: 100%;display: block;text-align: center;}}/** RTL **/.rtl {direction: rtl;font-family: Tahoma, 'Helvetica Neue', 'Helvetica', Helvetica, Arial, sans-serif;}.rtl table {text-align: right;}.rtl table tr td:nth-child(2) {text-align: left;}</style></head><body><div class=\"invoice-box\"><table cellpadding=\"0\" cellspacing=\"0\"><tr class=\"top\"><td colspan=\"2\"><table><tr><td class=\"title\"><img src=\"\" style=\"width:100%; max-width:300px;\"></td><td>Contact Us Message</td></tr></table></td></tr><tr class=\"information\"><td colspan=\"2\"><table><tr><td>  Promocups, Inc.<br>   Industrieweg 20-15 3846 BD<br>Harderwijk,Nederland</td><td><br>From : "+mailRequest.Name+" <br>Subject : "+mailRequest.Subject+" <br>Email : "+mailRequest.Email+"  <br>Phone : "+mailRequest.Phone+"</td></tr></table></td></tr></table><tr class=\"information\"><td colspan=\"2\"><table><tr><td>  Message</td><td><br>"+mailRequest.Body+"</td></tr></table></td></tr></div></body></html>";
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public async Task SendVerificationEmailAsync(MailRequest mailRequest, string verificationLink)
    {
      var email = new MimeMessage();
      Console.WriteLine("this is the mail settings : ");
      Console.WriteLine("MAIL : " + _mailSettings.Mail);
      Console.WriteLine("MAIL : " + _mailSettings.Host);
      Console.WriteLine("MAIL : " + _mailSettings.Password);
      Console.WriteLine("MAIL : " + _mailSettings.Port);
      email.From.Add(new MailboxAddress("TESTER", _mailSettings.Mail));
      email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
      email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
      email.Subject = mailRequest.Subject;
      var builder = new BodyBuilder();
      if (mailRequest.Attachments != null)
      {
        byte[] fileBytes;
        foreach (var file in mailRequest.Attachments)
        {
          if (file.Length > 0)
          {
            using (var ms = new MemoryStream())
            {
              file.CopyTo(ms);
              fileBytes = ms.ToArray();
            }
            builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
          }
        }
      }
      builder.HtmlBody = EmailTemplate1.Header + $@"Click here to verify your email : <a href=""{verificationLink}""> Verify Email </a>
    " + EmailTemplate1.Footer;

      email.Body = builder.ToMessageBody();
      using var smtp = new SmtpClient();
      smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);

      smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
      await smtp.SendAsync(email);
      smtp.Disconnect(true);
    }

    }
}