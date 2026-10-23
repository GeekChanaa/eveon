using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Payments;

/// <summary>Online top-ups and saved cards through the sandbox provider, over HTTP like the dashboard does.</summary>
public class PaymentFlowTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _customer = null!, _other = null!, _admin = null!;
    private int _cardId, _otherCardId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _customer = await Seeds.User(db, "pay@test.ma");
            _other = await Seeds.User(db, "other@test.ma");
            _admin = await Seeds.User(db, "admin@test.ma", role: "Admin");
            var card = Seeds.Card("PAY-1", 10m, _customer.ID);
            var otherCard = Seeds.Card("PAY-2", 10m, _other.ID);
            db.Cards.AddRange(card, otherCard);
            await db.SaveChangesAsync();
            _cardId = card.ID;
            _otherCardId = otherCard.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private Task<decimal> Balance(int cardId) => _api.Db(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync());

    private async Task<(int OrderId, string Url)> StartTopUp(decimal amount, int? cardId = null)
    {
        var response = await _api.Client(_customer).PostAsJsonAsync("/api/payments/top-ups", new { amount, cardId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("orderId").GetInt32(), body.GetProperty("checkout").GetProperty("url").GetString()!);
    }

    /// <summary>Opens a sandbox page and returns the hidden fields of the form whose button has this label.</summary>
    private async Task<Dictionary<string, string>> SandboxForm(string url, string button)
    {
        var html = await _api.Client().GetStringAsync(new Uri(url).PathAndQuery);
        var form = Regex.Matches(html, "<form.*?</form>", RegexOptions.Singleline).Select(m => m.Value).Single(f => f.Contains(">" + button + "<"));
        return Regex.Matches(form, "name=\"(?<n>[^\"]+)\" value=\"(?<v>[^\"]*)\"").ToDictionary(m => m.Groups["n"].Value, m => WebUtility.HtmlDecode(m.Groups["v"].Value));
    }

    private Task<HttpResponseMessage> Complete(Dictionary<string, string> fields) =>
        _api.Client().PostAsync("/api/payments/sandbox/complete", new FormUrlEncodedContent(fields));

    [Fact]
    public async Task The_sandbox_page_lets_its_form_redirect_back_to_the_dashboard()
    {
        var (_, url) = await StartTopUp(50m);

        var page = await _api.Client().GetAsync(new Uri(url).PathAndQuery);

        // Browsers check form-action on the redirect that follows the post, which goes to the dashboard.
        var csp = string.Join(";", page.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("form-action 'self' http://spa.test;", csp);
        Assert.Contains("default-src 'none'", csp);
    }

    [Fact]
    public async Task An_approved_payment_credits_the_wallet_once()
    {
        var (orderId, url) = await StartTopUp(100m);
        Assert.Equal(10m, await Balance(_cardId)); // nothing before the provider confirms
        var approve = await SandboxForm(url, "Approve payment");

        var response = await Complete(approve);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"http://spa.test/my-dashboard/wallet?topup={orderId}", response.Headers.Location!.ToString());
        Assert.Equal(110m, await Balance(_cardId));
        var order = await _api.Db(db => db.Orders.SingleAsync(o => o.ID == orderId));
        Assert.Equal(RechargeOrderStatus.Completed, order.Status);
        Assert.NotNull(order.PaidAt);
        Assert.Equal(1, await _api.Db(db => db.WalletTransactions.CountAsync(w => w.Reference == $"order:{orderId}:topup")));

        // The provider delivers the same notification again.
        await Complete(approve);
        Assert.Equal(110m, await Balance(_cardId));
    }

    [Fact]
    public async Task The_customer_sees_the_status_of_their_own_top_up_only()
    {
        var (orderId, url) = await StartTopUp(50m);
        await Complete(await SandboxForm(url, "Approve payment"));

        var mine = await _api.Client(_customer).GetFromJsonAsync<JsonElement>($"/api/payments/top-ups/{orderId}");
        var theirs = await _api.Client(_other).GetAsync($"/api/payments/top-ups/{orderId}");

        Assert.Equal("Completed", mine.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, theirs.StatusCode);
    }

    [Fact]
    public async Task A_declined_or_canceled_payment_credits_nothing()
    {
        var (declined, url1) = await StartTopUp(30m);
        var (canceled, url2) = await StartTopUp(40m);

        await Complete(await SandboxForm(url1, "Decline"));
        var back = await Complete(await SandboxForm(url2, "Cancel"));

        Assert.Equal(10m, await Balance(_cardId));
        Assert.Equal(RechargeOrderStatus.Failed, await _api.Db(db => db.Orders.Where(o => o.ID == declined).Select(o => o.Status).SingleAsync()));
        Assert.Equal(RechargeOrderStatus.Canceled, await _api.Db(db => db.Orders.Where(o => o.ID == canceled).Select(o => o.Status).SingleAsync()));
        Assert.EndsWith("&canceled=true", back.Headers.Location!.ToString());
    }

    [Fact]
    public async Task A_forged_or_tampered_notification_is_refused()
    {
        var (orderId, url) = await StartTopUp(20m);
        var fields = await SandboxForm(url, "Decline");
        fields["outcome"] = "Paid"; // turn the decline into an approval

        var tampered = await Complete(fields);
        var forged = await _api.Client().PostAsync("/api/payments/webhooks/Sandbox",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["event"] = "x", ["kind"] = "pay", ["id"] = orderId.ToString(), ["outcome"] = "Paid", ["amount"] = "20.00", ["sig"] = "00" }));

        Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal(10m, await Balance(_cardId));
    }

    [Theory]
    [InlineData(5)]       // below the minimum
    [InlineData(10000)]   // above the maximum
    [InlineData(20.555)]  // more than two decimals
    public async Task Top_up_amounts_are_validated(decimal amount)
    {
        var response = await _api.Client(_customer).PostAsJsonAsync("/api/payments/top-ups", new { amount });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nobody_can_top_up_someone_else_s_card()
    {
        var response = await _api.Client(_customer).PostAsJsonAsync("/api/payments/top-ups", new { amount = 50m, cardId = _otherCardId });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(await _api.Db(db => db.Orders.AnyAsync()));
    }

    [Fact]
    public async Task Payment_endpoints_require_a_session_except_provider_callbacks()
    {
        var anonymous = _api.Client();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/payments/top-ups", new { amount = 50m })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/payments/methods")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/api/payments/return/Sandbox?order=1")).StatusCode);
    }

    [Fact]
    public async Task The_mock_payment_endpoint_is_gone()
    {
        var response = await _api.Client(_customer).PostAsJsonAsync("/api/Order/RechargeCard", new { cardID = _cardId, rechargeAmount = 100, mockPaymentStatus = 2 });
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed, response.StatusCode.ToString());
        Assert.Equal(10m, await Balance(_cardId));
    }

    [Fact]
    public async Task Cards_are_saved_on_the_provider_page_and_managed_by_their_owner()
    {
        var client = _api.Client(_customer);
        for (var i = 0; i < 2; i++)
        {
            var setup = await (await client.PostAsync("/api/payments/methods/setup", null)).Content.ReadFromJsonAsync<JsonElement>();
            var saved = await Complete(await SandboxForm(setup.GetProperty("url").GetString()!, "Save card"));
            Assert.Equal("http://spa.test/my-dashboard/payment-methods?setup=done", saved.Headers.Location!.ToString());
        }

        var methods = await client.GetFromJsonAsync<JsonElement[]>("/api/payments/methods");
        Assert.Equal(2, methods!.Length);
        Assert.Single(methods, m => m.GetProperty("isDefault").GetBoolean());
        Assert.All(methods, m => Assert.Equal("4242", m.GetProperty("last4").GetString()));
        Assert.DoesNotContain("sandbox_", JsonSerializer.Serialize(methods)); // the provider token never leaves the API

        var second = methods.Single(m => !m.GetProperty("isDefault").GetBoolean()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/payments/methods/{second}/default", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.Client(_other).DeleteAsync($"/api/payments/methods/{second}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/payments/methods/{second}")).StatusCode);

        var left = await client.GetFromJsonAsync<JsonElement[]>("/api/payments/methods");
        Assert.True(Assert.Single(left!).GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task An_operator_credits_a_manual_top_up_with_a_reason()
    {
        var response = await _api.Client(_admin).PostAsJsonAsync("/api/Order/CreateRechargeOrder", new { cardID = _cardId, amount = 75m, note = "Cash at the front desk" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(85m, await Balance(_cardId));
        var movement = await _api.Db(db => db.WalletTransactions.SingleAsync(w => w.Type == WalletTransactionType.TopUp));
        Assert.Equal(_admin.ID, movement.CreatedByUserID);
        Assert.Contains("Cash at the front desk", movement.Description);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _api.Client(_customer).PostAsJsonAsync("/api/Order/CreateRechargeOrder", new { cardID = _cardId, amount = 75m, note = "free money" })).StatusCode);
    }
}

public class DisabledPaymentsTests
{
    [Fact]
    public async Task Without_a_provider_top_ups_are_refused_with_a_clear_message()
    {
        using var api = new ApiFactory(new Dictionary<string, string?> { ["Payments:Provider"] = "None" });
        User user = null!;
        await api.Seed(async db =>
        {
            user = await Seeds.User(db);
            db.Cards.Add(Seeds.Card("C-1", 0m, user.ID));
        });
        var client = api.Client(user);

        var config = await client.GetFromJsonAsync<JsonElement>("/api/payments/config");
        var start = await client.PostAsJsonAsync("/api/payments/top-ups", new { amount = 50m });

        Assert.False(config.GetProperty("enabled").GetBoolean());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, start.StatusCode);
        Assert.Contains("not available", await start.Content.ReadAsStringAsync());
    }
}
