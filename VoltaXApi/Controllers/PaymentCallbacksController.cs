using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Services.Payments;

namespace VoltaXApi.Controllers
{
    /// <summary>
    /// Endpoints the payment provider and the customer's browser reach without an EVEON session: provider
    /// notifications (verified by signature, they are the only thing that credits a wallet), return URLs (they only
    /// redirect to the dashboard) and the sandbox test pages.
    /// </summary>
    [ApiController]
    [Route("api/payments")]
    [PublicEndpoint]
    public sealed class PaymentCallbacksController : ControllerBase
    {
        private readonly PaymentService _payments;
        private readonly ILogger<PaymentCallbacksController> _logger;

        public PaymentCallbacksController(PaymentService payments, ILogger<PaymentCallbacksController> logger)
        {
            _payments = payments;
            _logger = logger;
        }

        private IPaymentGateway Gateway => _payments.Gateway;

        [HttpPost("webhooks/{provider}")]
        public async Task<IActionResult> Notify(string provider, CancellationToken cancellationToken)
        {
            if (!string.Equals(provider, Gateway.Name, StringComparison.OrdinalIgnoreCase) || Gateway is DisabledPaymentGateway)
                return NotFound();
            var notification = await Gateway.ParseNotificationAsync(Request, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("Rejected a {Provider} notification with an invalid signature from {RemoteIp}", provider, HttpContext.Connection.RemoteIpAddress);
                return BadRequest(Gateway.Acknowledge(null, false));
            }
            var accepted = await _payments.HandleNotificationAsync(notification, cancellationToken);
            return Content(Gateway.Acknowledge(notification, accepted), "text/plain", Encoding.UTF8);
        }

        /// <summary>The provider sends the browser back here; it lands on the dashboard, which shows the top-up status.</summary>
        [HttpGet("return/{provider}")]
        [HttpPost("return/{provider}")]
        public IActionResult Return(string provider, [FromQuery] int? order, [FromQuery] bool setup = false, [FromQuery] bool canceled = false)
        {
            var target = setup
                ? $"/my-dashboard/payment-methods?setup={(canceled ? "canceled" : "done")}"
                : $"/my-dashboard/wallet?topup={order}{(canceled ? "&canceled=true" : "")}";
            return Redirect(_payments.Options.SpaPage(target));
        }

        [HttpGet("sandbox/pay")]
        public async Task<IActionResult> SandboxPay([FromQuery] int order, [FromQuery] string amount, [FromQuery] string sig,
            [FromServices] VoltaXApiDbContext db, CancellationToken cancellationToken)
        {
            if (Gateway is not SandboxPaymentGateway sandbox || !sandbox.VerifyPage("pay", order.ToString(), amount, sig))
                return NotFound();
            var exists = await db.Orders.AsNoTracking().AnyAsync(o => o.ID == order && o.Provider == SandboxPaymentGateway.ProviderName, cancellationToken);
            if (!exists)
                return NotFound();
            return Page($"Pay {WebUtility.HtmlEncode(amount)} {WebUtility.HtmlEncode(_payments.Options.Currency)}",
                "Sandbox payment for top-up #" + order + ". No money moves: choose the result to simulate.",
                Button(sandbox.CompletionFields("pay", order.ToString(), "Paid", amount), "Approve payment", primary: true),
                Button(sandbox.CompletionFields("pay", order.ToString(), "Failed", amount), "Decline"),
                Button(sandbox.CompletionFields("pay", order.ToString(), "Canceled", amount), "Cancel"));
        }

        [HttpGet("sandbox/setup")]
        public IActionResult SandboxSetup([FromQuery] int user, [FromQuery] string sig)
        {
            if (Gateway is not SandboxPaymentGateway sandbox || !sandbox.VerifyPage("setup", user.ToString(), null, sig))
                return NotFound();
            return Page("Save a test card", "Sandbox card saving: a Visa ending in 4242 is added to your payment methods.",
                Button(sandbox.CompletionFields("setup", user.ToString(), "CardSaved"), "Save card", primary: true),
                Button(sandbox.CompletionFields("setup", user.ToString(), "Canceled"), "Cancel"));
        }

        [HttpPost("sandbox/complete")]
        public async Task<IActionResult> SandboxComplete(CancellationToken cancellationToken)
        {
            if (Gateway is not SandboxPaymentGateway sandbox)
                return NotFound();
            var notification = await sandbox.ParseNotificationAsync(Request, cancellationToken);
            if (notification == null)
                return BadRequest("Invalid signature.");
            await _payments.HandleNotificationAsync(notification, cancellationToken);
            return notification.Type == "setup"
                ? Return(SandboxPaymentGateway.ProviderName, null, setup: true, canceled: notification.Outcome != PaymentOutcome.CardSaved)
                : Return(SandboxPaymentGateway.ProviderName, notification.OrderId, canceled: notification.Outcome == PaymentOutcome.Canceled);
        }

        private static string Button(IReadOnlyDictionary<string, string> fields, string label, bool primary = false)
        {
            var inputs = string.Concat(fields.Select(f =>
                $"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(f.Key)}\" value=\"{WebUtility.HtmlEncode(f.Value)}\">"));
            return $"<form method=\"post\" action=\"/api/payments/sandbox/complete\">{inputs}<button class=\"{(primary ? "primary" : "")}\">{WebUtility.HtmlEncode(label)}</button></form>";
        }

        private ContentResult Page(string title, string text, params string[] buttons)
        {
            // Browsers apply form-action to the redirect that follows the post, which lands on the dashboard.
            var spa = Uri.TryCreate(_payments.Options.SpaUrl, UriKind.Absolute, out var spaUri) ? " " + spaUri.GetLeftPart(UriPartial.Authority) : "";
            Response.Headers.ContentSecurityPolicy = $"default-src 'none'; style-src 'unsafe-inline'; form-action 'self'{spa}; frame-ancestors 'none'";
            var html = $$"""
                <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
                <title>EVEON sandbox payment</title>
                <style>
                body{margin:0;font-family:system-ui,sans-serif;background:#eef2f9;color:#0a1a3a;display:flex;min-height:100vh;align-items:center;justify-content:center}
                main{background:#fff;border-radius:14px;padding:32px;max-width:420px;width:calc(100% - 32px);box-shadow:0 8px 30px rgba(10,26,58,.12)}
                .tag{display:inline-block;background:#fff3cd;color:#7a5b00;border-radius:6px;padding:2px 8px;font-size:12px;font-weight:600}
                h1{font-size:24px;margin:14px 0 8px} p{color:#4a5a7a;line-height:1.5}
                form{display:inline} button{margin:8px 8px 0 0;padding:10px 16px;border-radius:8px;border:1px solid #c9d3e6;background:#fff;font-size:15px;cursor:pointer}
                button.primary{background:#0a1a3a;color:#fff;border-color:#0a1a3a}
                </style></head><body><main><span class="tag">SANDBOX</span><h1>{{WebUtility.HtmlEncode(title)}}</h1><p>{{WebUtility.HtmlEncode(text)}}</p>{{string.Concat(buttons)}}</main></body></html>
                """;
            return Content(html, "text/html", Encoding.UTF8);
        }
    }
}
