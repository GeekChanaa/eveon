using VoltaXApi.Models;

namespace VoltaXApi.Services.Payments
{
    /// <summary>Payments configuration (section "Payments"). Secrets come from user-secrets / environment variables.</summary>
    public sealed class PaymentOptions
    {
        public const string None = "None";

        /// <summary>None (online payment off), Sandbox (test page, no money), Cmi or Stripe.</summary>
        public string Provider { get; set; } = None;
        public string Currency { get; set; } = "MAD";
        public decimal MinTopUp { get; set; } = 20;
        public decimal MaxTopUp { get; set; } = 5000;
        /// <summary>Public base URL of this API, used for provider callbacks and return URLs (https://api.eveon.ma).</summary>
        public string PublicApiUrl { get; set; } = "";
        /// <summary>Base URL of the dashboard the customer comes back to after paying.</summary>
        public string SpaUrl { get; set; } = "";
        public SandboxPaymentOptions Sandbox { get; set; } = new();
        public CmiPaymentOptions Cmi { get; set; } = new();
        public StripePaymentOptions Stripe { get; set; } = new();

        public string ApiUrl(string path) => PublicApiUrl.TrimEnd('/') + path;
        public string SpaPage(string path) => SpaUrl.TrimEnd('/') + path;
    }

    public sealed class SandboxPaymentOptions
    {
        /// <summary>Signs the test page forms. Required when Provider=Sandbox.</summary>
        public string Secret { get; set; } = "";
        /// <summary>The sandbox credits wallets without money: refused in Production unless this is set on purpose.</summary>
        public bool AllowInProduction { get; set; }
    }

    public sealed class CmiPaymentOptions
    {
        public string ClientId { get; set; } = "";
        public string StoreKey { get; set; } = "";
        /// <summary>3D hosting gateway (test: https://testpayment.cmi.co.ma/fim/est3Dgate).</summary>
        public string GatewayUrl { get; set; } = "https://testpayment.cmi.co.ma/fim/est3Dgate";
        /// <summary>ISO 4217 numeric currency (504 = MAD).</summary>
        public string CurrencyCode { get; set; } = "504";
        public string Lang { get; set; } = "fr";
    }

    public sealed class StripePaymentOptions
    {
        public string SecretKey { get; set; } = "";
        public string WebhookSecret { get; set; } = "";
        public string ApiBaseUrl { get; set; } = "https://api.stripe.com";
        /// <summary>Webhook signatures older than this are refused (replay protection).</summary>
        public int SignatureToleranceSeconds { get; set; } = 300;
    }

    /// <summary>
    /// What the browser must do to pay: open <see cref="Url"/> (GET) or post <see cref="Fields"/> to it (POST, for
    /// hosted payment pages such as CMI's).
    /// </summary>
    public sealed record CheckoutInstruction(string Method, string Url, IReadOnlyDictionary<string, string>? Fields = null,
        string? ProviderReference = null);

    public enum PaymentOutcome
    {
        Paid,
        Failed,
        Canceled,
        CardSaved,
        Ignored
    }

    public sealed record SavedCardInfo(string Token, DebitCardTypeEnum Brand, string Last4, int ExpiryMonth, int ExpiryYear);

    /// <summary>A verified provider notification, in provider-neutral form.</summary>
    public sealed record PaymentNotification(
        string EventId,
        PaymentOutcome Outcome,
        int? OrderId = null,
        decimal? Amount = null,
        string? ProviderReference = null,
        string? FailureReason = null,
        int? UserId = null,
        SavedCardInfo? Card = null,
        string? Type = null);

    /// <summary>One payment provider. Implementations never trust the browser: only verified notifications move money.</summary>
    public interface IPaymentGateway
    {
        string Name { get; }
        bool SupportsSavedCards { get; }

        Task<CheckoutInstruction> CreateCheckoutAsync(Order order, User user, CancellationToken cancellationToken = default);

        /// <summary>Starts saving a card for <paramref name="user"/> (provider-hosted page).</summary>
        Task<CheckoutInstruction> CreateCardSetupAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>Reads and verifies a provider callback; null when its signature is invalid.</summary>
        Task<PaymentNotification?> ParseNotificationAsync(HttpRequest request, CancellationToken cancellationToken = default);

        /// <summary>The body the provider expects in answer to a notification.</summary>
        string Acknowledge(PaymentNotification? notification, bool accepted);
    }

    /// <summary>Online payment is not configured: every attempt is refused with a clear message.</summary>
    public sealed class DisabledPaymentGateway : IPaymentGateway
    {
        public string Name => PaymentOptions.None;
        public bool SupportsSavedCards => false;

        public Task<CheckoutInstruction> CreateCheckoutAsync(Order order, User user, CancellationToken cancellationToken = default) =>
            throw new PaymentUnavailableException();

        public Task<CheckoutInstruction> CreateCardSetupAsync(User user, CancellationToken cancellationToken = default) =>
            throw new PaymentUnavailableException();

        public Task<PaymentNotification?> ParseNotificationAsync(HttpRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PaymentNotification?>(null);

        public string Acknowledge(PaymentNotification? notification, bool accepted) => "";
    }

    public sealed class PaymentUnavailableException() : Exception("Online payment is not available yet. Please contact support to top up your wallet.");
}
