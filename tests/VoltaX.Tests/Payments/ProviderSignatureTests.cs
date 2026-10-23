using System.Security.Cryptography;
using System.Text;
using VoltaXApi.Services.Payments;

namespace VoltaX.Tests.Payments;

public class ProviderSignatureTests
{
    private static readonly Dictionary<string, string> CmiCallback = new()
    {
        ["clientid"] = "600000000",
        ["oid"] = "42",
        ["amount"] = "100.00",
        ["currency"] = "504",
        ["ProcReturnCode"] = "00",
        ["Response"] = "Approved",
        ["TransId"] = "26296Ldfg31",
        ["BillToName"] = "Ali|Ben\\Salah",
        ["encoding"] = "UTF-8"
    };

    [Fact]
    public void Cmi_hash_v3_sorts_names_escapes_values_and_appends_the_store_key()
    {
        const string storeKey = "TEST1234";
        // Sorted case-insensitively, hash/encoding excluded, \ and | escaped.
        var plain = "100.00|Ali\\|Ben\\\\Salah|600000000|504|42|00|Approved|26296Ldfg31|TEST1234";
        var expected = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(plain)));

        Assert.Equal(expected, CmiPaymentGateway.Hash(CmiCallback, storeKey));
    }

    [Fact]
    public void Cmi_callbacks_are_verified_against_the_store_key()
    {
        var hash = CmiPaymentGateway.Hash(CmiCallback, "TEST1234");
        var tampered = new Dictionary<string, string>(CmiCallback) { ["amount"] = "1000.00" };

        Assert.True(CmiPaymentGateway.Verify(CmiCallback, hash, "TEST1234"));
        Assert.False(CmiPaymentGateway.Verify(tampered, hash, "TEST1234"));
        Assert.False(CmiPaymentGateway.Verify(CmiCallback, hash, "OTHERKEY"));
        Assert.False(CmiPaymentGateway.Verify(CmiCallback, hash, ""));
    }

    private static string StripeHeader(string body, string secret, long timestamp)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    [Fact]
    public void Stripe_webhooks_are_verified_with_the_endpoint_secret_and_a_time_window()
    {
        const string body = "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\"}";
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var header = StripeHeader(body, "whsec_test", now.ToUnixTimeSeconds());

        Assert.True(StripePaymentGateway.VerifySignature(body, header, "whsec_test", now, 300));
        Assert.True(StripePaymentGateway.VerifySignature(body, "v1=deadbeef," + header, "whsec_test", now, 300)); // several v1 values
        Assert.False(StripePaymentGateway.VerifySignature(body + " ", header, "whsec_test", now, 300));
        Assert.False(StripePaymentGateway.VerifySignature(body, header, "whsec_other", now, 300));
        Assert.False(StripePaymentGateway.VerifySignature(body, header, "whsec_test", now.AddMinutes(10), 300)); // replay
        Assert.False(StripePaymentGateway.VerifySignature(body, "", "whsec_test", now, 300));
    }

    [Theory]
    [InlineData(100, 10000)]
    [InlineData(19.99, 1999)]
    [InlineData(0.105, 11)]
    public void Stripe_amounts_are_sent_in_cents(decimal amount, long cents) =>
        Assert.Equal(cents, StripePaymentGateway.Cents(amount));
}
