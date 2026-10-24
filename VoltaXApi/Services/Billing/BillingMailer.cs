using System.Net;
using Microsoft.AspNetCore.Http;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Services.Billing
{
    /// <summary>
    /// Billing emails (top-up invoice, refund credit note) in the document's language, with the stored PDF
    /// attached. Arabic mails are written right to left. Sending never throws: the money already moved.
    /// </summary>
    public sealed class BillingMailer
    {
        private readonly IMailService _mail;
        private readonly IInvoiceService _invoices;
        private readonly ILogger<BillingMailer> _logger;

        public BillingMailer(IMailService mail, IInvoiceService invoices, ILogger<BillingMailer> logger)
        {
            _mail = mail;
            _invoices = invoices;
            _logger = logger;
        }

        /// <summary>Emails <paramref name="invoice"/> to its customer with <paramref name="subject"/> and <paramref name="paragraph"/>.</summary>
        public async Task SendAsync(Invoice invoice, string subject, string paragraph, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                return;
            try
            {
                var file = await _invoices.GetFileAsync(invoice.ID, ct);
                var attachments = new List<IFormFile>();
                if (file != null)
                    attachments.Add(new FormFile(new MemoryStream(file.Content), 0, file.Content.Length, "invoice", file.FileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = file.ContentType
                    });
                await _mail.SendHtmlEmailAsync(new MailRequest
                {
                    Email = invoice.CustomerEmail,
                    Name = invoice.CustomerName,
                    ToEmails = new List<string> { invoice.CustomerEmail },
                    Subject = subject,
                    Body = Html(invoice.Language, invoice.CustomerName, paragraph),
                    Attachments = attachments
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Billing email for {Number} could not be sent", invoice.Number);
            }
        }

        public static string Html(string language, string name, string paragraph)
        {
            var rtl = BillingTexts.IsRightToLeft(language);
            var greeting = WebUtility.HtmlEncode(BillingTexts.Format(language, "MailGreeting", string.IsNullOrWhiteSpace(name) ? "" : name).Replace(" ,", ","));
            return $"""
                <!DOCTYPE html>
                <html lang="{language}" dir="{(rtl ? "rtl" : "ltr")}">
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
                <body style="margin:0;padding:24px;background:#F3F6FB;font-family:Inter,'Segoe UI',Tahoma,'Noto Sans Arabic',Arial,sans-serif;color:#0A1A3A;">
                  <div style="max-width:560px;margin:0 auto;background:#FFFFFF;border:1px solid #DFE5F0;border-radius:12px;padding:28px;text-align:{(rtl ? "right" : "left")};">
                    <div style="font-size:22px;font-weight:700;letter-spacing:0.04em;color:#0A1A3A;">EVEON</div>
                    <div style="height:3px;background:#E5B223;margin:12px 0 20px;"></div>
                    <p style="font-size:15px;margin:0 0 12px;">{greeting}</p>
                    <p style="font-size:15px;line-height:1.6;margin:0 0 20px;">{WebUtility.HtmlEncode(paragraph)}</p>
                    <p style="font-size:14px;color:#5B6680;margin:0;">{WebUtility.HtmlEncode(BillingTexts.Get(language, "MailSignature"))}</p>
                  </div>
                </body>
                </html>
                """;
        }
    }
}
