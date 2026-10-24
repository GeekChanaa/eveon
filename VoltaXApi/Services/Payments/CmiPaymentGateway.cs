using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Payments
{
    /// <summary>
    /// CMI (Centre Monétique Interbancaire) 3D Secure hosted payment page. The browser posts a signed form to the
    /// CMI gateway; CMI calls <c>callbackUrl</c> server to server with the result, signed the same way, and the
    /// wallet is credited only on that verified callback. Hash version 3: every parameter except hash/encoding,
    /// sorted by name (case-insensitive), values escaped (\ → \\, | → \|), joined with |, then |storeKey, SHA-512,
    /// Base64.
    ///
    /// To validate with the CMI test platform before going live: the callback answer (ACTION=POSTAUTH to capture
    /// an approved PreAuth) and the exact parameter list must match the merchant's CMI integration guide.
    /// Saved cards are not supported through CMI here.
    /// </summary>
    public sealed class CmiPaymentGateway : IPaymentGateway
    {
        public const string ProviderName = "Cmi";
        private static readonly HashSet<string> Unsigned = new(StringComparer.OrdinalIgnoreCase) { "hash", "encoding" };
        private readonly PaymentOptions _options;

        public CmiPaymentGateway(IOptions<PaymentOptions> options)
        {
            _options = options.Value;
        }

        public string Name => ProviderName;
        public bool SupportsSavedCards => false;

        public Task<CheckoutInstruction> CreateCheckoutAsync(Order order, User user, CancellationToken cancellationToken = default)
        {
            var cmi = _options.Cmi;
            if (string.IsNullOrEmpty(cmi.ClientId) || string.IsNullOrEmpty(cmi.StoreKey))
                throw new InvalidOperationException("Payments:Cmi:ClientId and StoreKey are required.");
            var fields = new Dictionary<string, string>
            {
                ["clientid"] = cmi.ClientId,
                ["storetype"] = "3D_PAY_HOSTING",
                ["trantype"] = "PreAuth",
                ["amount"] = order.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["currency"] = cmi.CurrencyCode,
                ["oid"] = order.ID.ToString(CultureInfo.InvariantCulture),
                ["okUrl"] = _options.ApiUrl($"/api/payments/return/{ProviderName}?order={order.ID}"),
                ["failUrl"] = _options.ApiUrl($"/api/payments/return/{ProviderName}?order={order.ID}"),
                ["callbackUrl"] = _options.ApiUrl($"/api/payments/webhooks/{ProviderName}"),
                ["shopurl"] = _options.SpaPage("/my-dashboard/wallet"),
                ["lang"] = cmi.Lang,
                ["email"] = user.Email ?? "",
                ["BillToName"] = $"{user.FirstName} {user.LastName}".Trim(),
                ["rnd"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(10)),
                ["hashAlgorithm"] = "ver3",
                ["encoding"] = "UTF-8",
                ["refreshtime"] = "5",
                ["AutoRedirect"] = "true"
            };
            fields["HASH"] = Hash(fields, cmi.StoreKey);
            return Task.FromResult(new CheckoutInstruction("POST", cmi.GatewayUrl, fields, ProviderReference: null));
        }

        public Task<CheckoutInstruction> CreateCardSetupAsync(User user, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Saving a card is not available with CMI.");

        public async Task<PaymentNotification?> ParseNotificationAsync(HttpRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.HasFormContentType)
                return null;
            var form = await request.ReadFormAsync(cancellationToken);
            var values = form.ToDictionary(p => p.Key, p => p.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            if (!values.TryGetValue("HASH", out var received) || !Verify(values, received, _options.Cmi.StoreKey))
                return null;

            values.TryGetValue("oid", out var oid);
            values.TryGetValue("ProcReturnCode", out var code);
            values.TryGetValue("Response", out var response);
            values.TryGetValue("TransId", out var transId);
            values.TryGetValue("ErrMsg", out var error);
            decimal? amount = values.TryGetValue("amount", out var a) && decimal.TryParse(a, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed : null;
            var paid = code == "00" && string.Equals(response, "Approved", StringComparison.OrdinalIgnoreCase);
            return new PaymentNotification(
                EventId: string.IsNullOrEmpty(transId) ? $"{oid}-{code}-{values.GetValueOrDefault("rnd")}" : transId,
                Outcome: paid ? PaymentOutcome.Paid : PaymentOutcome.Failed,
                OrderId: int.TryParse(oid, out var orderId) ? orderId : null,
                Amount: amount,
                ProviderReference: transId,
                FailureReason: paid ? null : string.IsNullOrEmpty(error) ? $"{response} ({code})" : error,
                Type: "callback");
        }

        public bool SupportsRefunds => false;

        /// <summary>
        /// CMI refunds ("remboursement" / credit) go through the CMI merchant back office or its back-office API,
        /// which needs API credentials this integration does not have yet: the operator refunds there and records
        /// the refund as made outside the platform.
        /// </summary>
        public Task<GatewayRefundResult> RefundAsync(Order order, decimal amount, string refundReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(GatewayRefundResult.NotSupported(
                "CMI refunds are made in the CMI merchant back office. Refund there, then record the refund as made outside the platform."));

        /// <summary>CMI captures an approved PreAuth when the merchant answers ACTION=POSTAUTH.</summary>
        public string Acknowledge(PaymentNotification? notification, bool accepted) =>
            !accepted ? "FAILURE" : notification?.Outcome == PaymentOutcome.Paid ? "ACTION=POSTAUTH" : "APPROVED";

        public static string Hash(IReadOnlyDictionary<string, string> parameters, string storeKey)
        {
            var plain = string.Join("|", parameters
                .Where(p => !Unsigned.Contains(p.Key))
                .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(p => Escape(p.Value.Trim()))) + "|" + Escape(storeKey);
            return Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(plain)));
        }

        public static bool Verify(IReadOnlyDictionary<string, string> parameters, string receivedHash, string storeKey)
        {
            if (string.IsNullOrEmpty(storeKey) || string.IsNullOrEmpty(receivedHash))
                return false;
            var expected = Hash(parameters, storeKey);
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(receivedHash));
        }

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("|", "\\|");
    }
}
