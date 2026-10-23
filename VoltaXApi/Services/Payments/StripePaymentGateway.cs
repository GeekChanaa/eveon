using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Payments
{
    /// <summary>
    /// Stripe Checkout: hosted payment page (mode=payment) and hosted card saving (mode=setup) for a Stripe
    /// customer per user, so saved cards are offered on the next payment. Results arrive as signed webhooks
    /// (Stripe-Signature: HMAC-SHA256 of "timestamp.body" with the endpoint secret, 5 minutes tolerance).
    /// </summary>
    public sealed class StripePaymentGateway : IPaymentGateway
    {
        public const string ProviderName = "Stripe";
        public const string HttpClientName = "stripe";
        private readonly PaymentOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VoltaXApiDbContext _db;
        private readonly TimeProvider _time;

        public StripePaymentGateway(IOptions<PaymentOptions> options, IHttpClientFactory httpClientFactory, VoltaXApiDbContext db, TimeProvider time)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _db = db;
            _time = time;
        }

        public string Name => ProviderName;
        public bool SupportsSavedCards => true;

        public async Task<CheckoutInstruction> CreateCheckoutAsync(Order order, User user, CancellationToken cancellationToken = default)
        {
            var customer = await CustomerIdAsync(user, cancellationToken);
            var session = await PostAsync("/v1/checkout/sessions", new Dictionary<string, string>
            {
                ["mode"] = "payment",
                ["customer"] = customer,
                ["client_reference_id"] = order.ID.ToString(CultureInfo.InvariantCulture),
                ["metadata[order_id]"] = order.ID.ToString(CultureInfo.InvariantCulture),
                ["line_items[0][quantity]"] = "1",
                ["line_items[0][price_data][currency]"] = _options.Currency.ToLowerInvariant(),
                ["line_items[0][price_data][unit_amount]"] = Cents(order.Amount).ToString(CultureInfo.InvariantCulture),
                ["line_items[0][price_data][product_data][name]"] = "EVEON wallet top-up",
                ["payment_intent_data[setup_future_usage]"] = "off_session",
                ["success_url"] = _options.ApiUrl($"/api/payments/return/{ProviderName}?order={order.ID}"),
                ["cancel_url"] = _options.ApiUrl($"/api/payments/return/{ProviderName}?order={order.ID}&canceled=true")
            }, $"topup-{order.ID}", cancellationToken);
            return new CheckoutInstruction("GET", session.GetProperty("url").GetString()!, ProviderReference: session.GetProperty("id").GetString());
        }

        public async Task<CheckoutInstruction> CreateCardSetupAsync(User user, CancellationToken cancellationToken = default)
        {
            var customer = await CustomerIdAsync(user, cancellationToken);
            var session = await PostAsync("/v1/checkout/sessions", new Dictionary<string, string>
            {
                ["mode"] = "setup",
                ["customer"] = customer,
                ["currency"] = _options.Currency.ToLowerInvariant(),
                ["metadata[user_id]"] = user.ID.ToString(CultureInfo.InvariantCulture),
                ["success_url"] = _options.ApiUrl($"/api/payments/return/{ProviderName}?setup=true"),
                ["cancel_url"] = _options.ApiUrl($"/api/payments/return/{ProviderName}?setup=true&canceled=true")
            }, null, cancellationToken);
            return new CheckoutInstruction("GET", session.GetProperty("url").GetString()!);
        }

        public async Task<PaymentNotification?> ParseNotificationAsync(HttpRequest request, CancellationToken cancellationToken = default)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);
            if (!VerifySignature(body, request.Headers["Stripe-Signature"].ToString(), _options.Stripe.WebhookSecret,
                    _time.GetUtcNow(), _options.Stripe.SignatureToleranceSeconds))
                return null;

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var eventId = root.GetProperty("id").GetString()!;
            var type = root.GetProperty("type").GetString()!;
            var session = root.GetProperty("data").GetProperty("object");
            string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            int? OrderId() => int.TryParse(Str(session, "client_reference_id"), out var id) ? id : null;
            decimal? Total() => session.TryGetProperty("amount_total", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt64() / 100m : null;

            switch (type)
            {
                case "checkout.session.completed" when Str(session, "mode") == "setup":
                {
                    var userId = session.TryGetProperty("metadata", out var meta) && int.TryParse(Str(meta, "user_id"), out var u) ? u : (int?)null;
                    var card = await SavedCardAsync(Str(session, "setup_intent"), cancellationToken);
                    return new PaymentNotification(eventId, card == null ? PaymentOutcome.Ignored : PaymentOutcome.CardSaved, UserId: userId, Card: card, Type: type);
                }
                case "checkout.session.completed":
                    // Card payments are paid at completion; delayed methods confirm with async_payment_succeeded.
                    return new PaymentNotification(eventId, Str(session, "payment_status") == "paid" ? PaymentOutcome.Paid : PaymentOutcome.Ignored,
                        OrderId(), Total(), Str(session, "id"), Type: type);
                case "checkout.session.async_payment_succeeded":
                    return new PaymentNotification(eventId, PaymentOutcome.Paid, OrderId(), Total(), Str(session, "id"), Type: type);
                case "checkout.session.async_payment_failed":
                    return new PaymentNotification(eventId, PaymentOutcome.Failed, OrderId(), Total(), Str(session, "id"), "Payment failed", Type: type);
                case "checkout.session.expired":
                    return new PaymentNotification(eventId, PaymentOutcome.Canceled, OrderId(), Total(), Str(session, "id"), "Checkout expired", Type: type);
                default:
                    return new PaymentNotification(eventId, PaymentOutcome.Ignored, Type: type);
            }
        }

        public string Acknowledge(PaymentNotification? notification, bool accepted) => accepted ? "{\"received\":true}" : "";

        /// <summary>Stripe-Signature: t=timestamp,v1=hex(HMAC-SHA256(secret, "t.body")), possibly several v1 values.</summary>
        public static bool VerifySignature(string body, string header, string secret, DateTimeOffset now, int toleranceSeconds)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(header))
                return false;
            long? timestamp = null;
            var signatures = new List<string>();
            foreach (var part in header.Split(','))
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0].Trim() == "t" && long.TryParse(kv[1], out var t)) timestamp = t;
                if (kv[0].Trim() == "v1") signatures.Add(kv[1].Trim());
            }
            if (timestamp == null || signatures.Count == 0 || Math.Abs(now.ToUnixTimeSeconds() - timestamp.Value) > toleranceSeconds)
                return false;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = Encoding.ASCII.GetBytes(Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant());
            return signatures.Any(s => CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(s)));
        }

        public static long Cents(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

        private async Task<string> CustomerIdAsync(User user, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(user.PaymentCustomerId))
                return user.PaymentCustomerId;
            var customer = await PostAsync("/v1/customers", new Dictionary<string, string>
            {
                ["email"] = user.Email ?? "",
                ["name"] = $"{user.FirstName} {user.LastName}".Trim(),
                ["metadata[user_id]"] = user.ID.ToString(CultureInfo.InvariantCulture)
            }, $"customer-{user.ID}", cancellationToken);
            var id = customer.GetProperty("id").GetString()!;
            var tracked = await _db.Users.FindAsync(new object[] { user.ID }, cancellationToken);
            if (tracked != null)
            {
                tracked.PaymentCustomerId = id;
                await _db.SaveChangesAsync(cancellationToken);
            }
            user.PaymentCustomerId = id;
            return id;
        }

        private async Task<SavedCardInfo?> SavedCardAsync(string? setupIntentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(setupIntentId))
                return null;
            var intent = await GetAsync($"/v1/setup_intents/{Uri.EscapeDataString(setupIntentId)}?expand[]=payment_method", cancellationToken);
            if (!intent.TryGetProperty("payment_method", out var method) || method.ValueKind != JsonValueKind.Object ||
                !method.TryGetProperty("card", out var card))
                return null;
            var brand = card.GetProperty("brand").GetString() switch
            {
                "visa" => DebitCardTypeEnum.Visa,
                "mastercard" => DebitCardTypeEnum.Mastercard,
                _ => DebitCardTypeEnum.Generic
            };
            return new SavedCardInfo(method.GetProperty("id").GetString()!, brand, card.GetProperty("last4").GetString()!,
                card.GetProperty("exp_month").GetInt32(), card.GetProperty("exp_year").GetInt32());
        }

        private async Task<JsonElement> PostAsync(string path, Dictionary<string, string> form, string? idempotencyKey, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Stripe.ApiBaseUrl.TrimEnd('/') + path) { Content = new FormUrlEncodedContent(form) };
            if (idempotencyKey != null)
                request.Headers.Add("Idempotency-Key", idempotencyKey);
            return await SendAsync(request, cancellationToken);
        }

        private async Task<JsonElement> GetAsync(string pathAndQuery, CancellationToken cancellationToken) =>
            await SendAsync(new HttpRequestMessage(HttpMethod.Get, _options.Stripe.ApiBaseUrl.TrimEnd('/') + pathAndQuery), cancellationToken);

        private async Task<JsonElement> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_options.Stripe.SecretKey))
                throw new InvalidOperationException("Payments:Stripe:SecretKey is not set.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Stripe.SecretKey);
            using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Stripe answered {(int)response.StatusCode}: {text}");
            return JsonDocument.Parse(text).RootElement.Clone();
        }
    }
}
