using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Payments
{
    /// <summary>
    /// Test provider: a page served by this API lets a tester approve, decline or cancel a payment, or save a test
    /// card. No money moves, so it is refused in Production unless explicitly allowed. Every form is signed with
    /// <see cref="SandboxPaymentOptions.Secret"/>: the browser cannot invent an approval.
    /// </summary>
    public sealed class SandboxPaymentGateway : IPaymentGateway
    {
        public const string ProviderName = "Sandbox";
        private readonly PaymentOptions _options;

        public SandboxPaymentGateway(IOptions<PaymentOptions> options)
        {
            _options = options.Value;
        }

        public string Name => ProviderName;
        public bool SupportsSavedCards => true;

        public Task<CheckoutInstruction> CreateCheckoutAsync(Order order, User user, CancellationToken cancellationToken = default)
        {
            var amount = Amount(order.Amount);
            var url = _options.ApiUrl($"/api/payments/sandbox/pay?order={order.ID}&amount={amount}&sig={Sign("pay", order.ID.ToString(), amount)}");
            return Task.FromResult(new CheckoutInstruction("GET", url, ProviderReference: $"sandbox-{order.ID}"));
        }

        public Task<CheckoutInstruction> CreateCardSetupAsync(User user, CancellationToken cancellationToken = default)
        {
            var url = _options.ApiUrl($"/api/payments/sandbox/setup?user={user.ID}&sig={Sign("setup", user.ID.ToString())}");
            return Task.FromResult(new CheckoutInstruction("GET", url));
        }

        public bool VerifyPage(string kind, string id, string? amount, string? signature) =>
            Equal(amount == null ? Sign(kind, id) : Sign(kind, id, amount), signature);

        /// <summary>Hidden fields of a test page button: what the tester chose, signed.</summary>
        public IReadOnlyDictionary<string, string> CompletionFields(string kind, string id, string outcome, string amount = "")
        {
            var eventId = Guid.NewGuid().ToString("N");
            return new Dictionary<string, string>
            {
                ["event"] = eventId,
                ["kind"] = kind,
                ["id"] = id,
                ["outcome"] = outcome,
                ["amount"] = amount,
                ["sig"] = Sign(eventId, kind, id, outcome, amount)
            };
        }

        public async Task<PaymentNotification?> ParseNotificationAsync(HttpRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.HasFormContentType)
                return null;
            var form = await request.ReadFormAsync(cancellationToken);
            string F(string key) => form[key].ToString();
            if (!Equal(Sign(F("event"), F("kind"), F("id"), F("outcome"), F("amount")), F("sig")))
                return null;
            if (!int.TryParse(F("id"), out var id) || !Enum.TryParse<PaymentOutcome>(F("outcome"), out var outcome))
                return null;

            if (F("kind") == "setup")
                return outcome == PaymentOutcome.CardSaved
                    ? new PaymentNotification(F("event"), outcome, UserId: id, Type: "setup",
                        Card: new SavedCardInfo("sandbox_" + F("event"), DebitCardTypeEnum.Visa, "4242", 12, DateTime.UtcNow.Year + 3))
                    : new PaymentNotification(F("event"), PaymentOutcome.Ignored, UserId: id, Type: "setup");

            decimal? amount = decimal.TryParse(F("amount"), NumberStyles.Number, CultureInfo.InvariantCulture, out var a) ? a : null;
            return new PaymentNotification(F("event"), outcome, OrderId: id, Amount: amount, ProviderReference: $"sandbox-{id}",
                FailureReason: outcome == PaymentOutcome.Failed ? "Declined on the sandbox page" : null, Type: "pay");
        }

        public string Acknowledge(PaymentNotification? notification, bool accepted) => accepted ? "OK" : "REJECTED";

        public static string Amount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

        private string Sign(params string[] parts)
        {
            if (string.IsNullOrEmpty(_options.Sandbox.Secret))
                throw new InvalidOperationException("Payments:Sandbox:Secret is not set.");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Sandbox.Secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(string.Join("|", parts)))).ToLowerInvariant();
        }

        private static bool Equal(string expected, string? actual) =>
            actual != null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));
    }
}
