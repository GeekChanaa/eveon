using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MimeKit;
using VoltaXApi.Services;
using VoltaXApi.Settings;

namespace VoltaX.Tests.Api;

/// <summary>Mail over Brevo's HTTPS API, for hosts that block outbound SMTP (Railway Hobby).</summary>
public class BrevoApiMailTransportTests
{
    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static BrevoApiMailTransport Transport(Handler handler) =>
        new(new Clients(handler), Options.Create(new MailSettings { Mail = "no-reply@eveon.test", BrevoApiKey = "xkeysib-test" }));

    private static MimeMessage Message()
    {
        var message = new MimeMessage { Subject = "Reset your password" };
        message.From.Add(new MailboxAddress("EVEON", "no-reply@eveon.test"));
        message.To.Add(new MailboxAddress("Driver", "driver@eveon.test"));
        var body = new BodyBuilder { HtmlBody = "<p>Hello</p>", TextBody = "Hello" };
        body.Attachments.Add("invoice.pdf", Encoding.ASCII.GetBytes("%PDF-1.4"), new ContentType("application", "pdf"));
        message.Body = body.ToMessageBody();
        return message;
    }

    [Fact]
    public async Task A_message_is_posted_to_the_transactional_api_with_the_key()
    {
        var handler = new Handler(HttpStatusCode.Created, "{\"messageId\":\"<1@relay>\"}");

        await Transport(handler).SendAsync(Message(), CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(BrevoApiMailTransport.Endpoint, handler.Request.RequestUri!.ToString());
        Assert.Equal("xkeysib-test", Assert.Single(handler.Request.Headers.GetValues("api-key")));
        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal("no-reply@eveon.test", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("EVEON", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("driver@eveon.test", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Reset your password", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Hello</p>", root.GetProperty("htmlContent").GetString());
        Assert.Equal("Hello", root.GetProperty("textContent").GetString());
        var attachment = root.GetProperty("attachment")[0];
        Assert.Equal("invoice.pdf", attachment.GetProperty("name").GetString());
        Assert.Equal("%PDF-1.4", Encoding.ASCII.GetString(Convert.FromBase64String(attachment.GetProperty("content").GetString()!)));
        Assert.False(root.TryGetProperty("cc", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task A_refused_message_or_account_is_not_worth_retrying(HttpStatusCode status)
    {
        var handler = new Handler(status, "{\"code\":\"unauthorized\",\"message\":\"Key not found\"}");

        var error = await Assert.ThrowsAsync<MailRejectedException>(() => Transport(handler).SendAsync(Message(), CancellationToken.None));

        Assert.Contains(((int)status).ToString(), error.Message);
        Assert.Contains("Key not found", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task A_busy_or_failing_api_is_retried(HttpStatusCode status)
    {
        var handler = new Handler(status, "{}");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => Transport(handler).SendAsync(Message(), CancellationToken.None));

        Assert.Equal(status, error.StatusCode);
    }
}
