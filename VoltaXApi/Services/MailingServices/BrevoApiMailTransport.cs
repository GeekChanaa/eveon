using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MimeKit;
using VoltaXApi.Settings;

namespace VoltaXApi.Services
{
    /// <summary>
    /// The mail service refused the message or the account (bad request, wrong key, no credits): sending it again
    /// cannot work, so <see cref="MailDeliveryService"/> drops it at once.
    /// </summary>
    public sealed class MailRejectedException(string message) : Exception(message);

    /// <summary>
    /// Delivery through Brevo's transactional API over HTTPS (MailSettings:BrevoApiKey), for hosts that block
    /// outbound SMTP: Railway's Hobby plan blocks ports 25, 465, 587 and 2525.
    /// </summary>
    public sealed class BrevoApiMailTransport(IHttpClientFactory httpClients, IOptions<MailSettings> settings) : IMailTransport
    {
        public const string HttpClientName = "brevo";
        public const string Endpoint = "https://api.brevo.com/v3/smtp/email";

        public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(Payload(message)) };
            request.Headers.Add("api-key", settings.Value.BrevoApiKey);
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await httpClients.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var reason = $"Brevo answered {(int)response.StatusCode}: {(body.Length > 300 ? body[..300] : body)}";
            // 429 and 5xx are worth another attempt; any other 4xx is about the message or the account
            // (401 "unrecognised IP address": Brevo's authorised IPs list is on, see docs/configuration.md).
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                throw new HttpRequestException(reason, null, response.StatusCode);
            throw new MailRejectedException(reason);
        }

        internal static BrevoEmail Payload(MimeMessage message)
        {
            var from = message.From.Mailboxes.FirstOrDefault() ?? message.Sender
                ?? throw new MailRejectedException("The message has no sender");
            return new BrevoEmail(
                new BrevoAddress(from.Address, NameOrNull(from.Name)),
                Addresses(message.To),
                NullIfEmpty(Addresses(message.Cc)),
                NullIfEmpty(Addresses(message.Bcc)),
                message.ReplyTo.Mailboxes.FirstOrDefault() is { } replyTo ? new BrevoAddress(replyTo.Address, NameOrNull(replyTo.Name)) : null,
                message.Subject ?? "",
                // Brevo needs htmlContent; a text-only message is sent as preformatted HTML.
                message.HtmlBody ?? $"<pre>{WebUtility.HtmlEncode(message.TextBody ?? "")}</pre>",
                message.TextBody,
                NullIfEmpty(message.Attachments.OfType<MimePart>().Select(Attachment).ToList()));
        }

        private static List<BrevoAddress> Addresses(InternetAddressList list) =>
            list.Mailboxes.Select(m => new BrevoAddress(m.Address, NameOrNull(m.Name))).ToList();

        private static BrevoAttachment Attachment(MimePart part)
        {
            using var content = new MemoryStream();
            part.Content.DecodeTo(content);
            return new BrevoAttachment(part.FileName ?? "attachment", Convert.ToBase64String(content.ToArray()));
        }

        // Brevo limits names to 70 characters.
        private static string? NameOrNull(string? name) => string.IsNullOrWhiteSpace(name) ? null : name.Length > 70 ? name[..70] : name;

        private static List<T>? NullIfEmpty<T>(List<T> list) => list.Count == 0 ? null : list;

        public override string ToString() => "Brevo API (HTTPS)";

        internal sealed record BrevoAddress(
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

        internal sealed record BrevoAttachment(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("content")] string Content);

        internal sealed record BrevoEmail(
            [property: JsonPropertyName("sender")] BrevoAddress Sender,
            [property: JsonPropertyName("to")] List<BrevoAddress> To,
            [property: JsonPropertyName("cc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<BrevoAddress>? Cc,
            [property: JsonPropertyName("bcc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<BrevoAddress>? Bcc,
            [property: JsonPropertyName("replyTo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BrevoAddress? ReplyTo,
            [property: JsonPropertyName("subject")] string Subject,
            [property: JsonPropertyName("htmlContent")] string HtmlContent,
            [property: JsonPropertyName("textContent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TextContent,
            [property: JsonPropertyName("attachment"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<BrevoAttachment>? Attachment);
    }
}
