using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;
using VoltaXApi.Services;
using VoltaXApi.Services.Billing;
using VoltaXApi.Services.Payments;

namespace VoltaX.Tests.Billing;

/// <summary>Operator refunds of top-ups (through the sandbox provider) and sessions (to the wallet), over HTTP.</summary>
public class RefundTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _customer = null!, _admin = null!;
    private int _cardId, _sessionId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _customer = await Seeds.User(db, "refund@test.ma");
            _admin = await Seeds.User(db, "refund-admin@test.ma", role: "Admin");
            await Seeds.ChargePoint(db, "CP-REFUND", c => c.PricePerKWh = 2m);
            var card = Seeds.Card("REF-1", 10m, _customer.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            _cardId = card.ID;
            var session = new ChargingSession
            {
                ConnectorID = (await db.Connectors.SingleAsync()).ID, CardID = card.ID, UserID = _customer.ID, StartDate = Seeds.Now,
                EndDate = Seeds.Now.AddMinutes(30), ChargedKwhs = 17, ChargedMinutes = 30, PricePerKWh = 2m, VatRate = 0.2m,
                EnergyAmount = 34m, TimeAmount = 0, FlatFeeAmount = 0, IdleAmount = 0, IdleMinutes = 0, TotalAmount = 34m,
                ChargingSessionStatus = ChargingSessionStatusEnum.Completed
            };
            db.ChargingSessions.Add(session);
            await db.SaveChangesAsync();
            _sessionId = session.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>A paid online (sandbox) or manual top-up, credited through the wallet like the real flows.</summary>
    private async Task<int> TopUp(decimal amount, string provider = "Sandbox")
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
        var order = new Order { CardID = _cardId, Amount = amount, Status = RechargeOrderStatus.Completed, Provider = provider,
            ProviderReference = $"{provider}-ref", RechargeDate = Seeds.Now, PaidAt = Seeds.Now };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<IWalletService>().PostAsync(_cardId,
            new WalletPosting(amount, WalletTransactionType.TopUp, $"order:{order.ID}:topup", "test top-up", OrderID: order.ID));
        return order.ID;
    }

    private Task<HttpResponseMessage> Refund(string target, int id, decimal amount, string? key = null, string? method = null) =>
        _api.Client(_admin).PostAsJsonAsync($"/api/Refund/{target}/{id}", new { amount, reason = "Customer request", idempotencyKey = key ?? Guid.NewGuid().ToString("N"), method });

    private Task<decimal> Balance() => _api.Db(db => db.Cards.Where(c => c.ID == _cardId).Select(c => c.Balance).SingleAsync());

    private static async Task<string> Code(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;

    [Fact]
    public async Task A_top_up_is_refunded_to_the_payment_method_with_a_credit_note()
    {
        var orderId = await TopUp(100m);

        var response = await Refund("Order", orderId, 40m);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refund = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Succeeded", refund.GetProperty("status").GetString());
        Assert.Equal("Gateway", refund.GetProperty("method").GetString());
        Assert.StartsWith("sandbox-refund-", refund.GetProperty("providerReference").GetString());
        Assert.Equal(110m - 40m, await Balance());
        var movement = await _api.Db(db => db.WalletTransactions.SingleAsync(w => w.Type == WalletTransactionType.Refund));
        Assert.Equal(-40m, movement.Amount);
        Assert.Equal(orderId, movement.OrderID);

        var invoices = await _api.Db(db => db.Invoices.OrderBy(i => i.ID).ToListAsync());
        var creditNote = invoices.Single(i => i.Kind == InvoiceKind.CreditNote);
        Assert.Matches(@"^AV-\d{4}-000001$", creditNote.Number);
        Assert.Equal(40m, creditNote.AmountInclVat);
        Assert.Equal(invoices.Single(i => i.Kind == InvoiceKind.Invoice).ID, creditNote.CreditedInvoiceID);
        Assert.Equal(creditNote.Number, refund.GetProperty("creditNoteNumber").GetString());

        Assert.True(await _api.Db(db => db.Notifications.AnyAsync(n => n.ReceiverID == _customer.ID && n.Action == "Refund")));
        var mail = (MailRequest)_api.Mail.CallsTo(nameof(IMailService.SendHtmlEmailAsync)).Last()[0]!;
        Assert.Contains(creditNote.Number, mail.Subject);
        Assert.Equal($"{creditNote.Number}.pdf", Assert.Single(mail.Attachments!).FileName);

        var summary = await _api.Client(_admin).GetFromJsonAsync<JsonElement>($"/api/Refund/Order/{orderId}");
        Assert.Equal(60m, summary.GetProperty("refundable").GetDecimal());
        Assert.Equal(RechargeOrderStatus.Completed, await _api.Db(db => db.Orders.Where(o => o.ID == orderId).Select(o => o.Status).SingleAsync()));
    }

    [Fact]
    public async Task A_top_up_is_never_refunded_beyond_what_was_paid()
    {
        var orderId = await TopUp(100m);

        var tooMuch = await Refund("Order", orderId, 100.01m);
        var full = await Refund("Order", orderId, 100m);
        var again = await Refund("Order", orderId, 1m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMuch.StatusCode);
        Assert.Equal("exceeds_paid", await Code(tooMuch));
        Assert.Equal(HttpStatusCode.OK, full.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal(RechargeOrderStatus.Refunded, await _api.Db(db => db.Orders.Where(o => o.ID == orderId).Select(o => o.Status).SingleAsync()));
        Assert.Equal(10m, await Balance());
    }

    [Fact]
    public async Task The_same_refund_request_sent_twice_refunds_once()
    {
        var orderId = await TopUp(100m);

        var first = await Refund("Order", orderId, 25m, key: "refund-key-0001");
        var second = await Refund("Order", orderId, 25m, key: "refund-key-0001");
        var reused = await Refund("Order", orderId, 30m, key: "refund-key-0001");

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32(),
            (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
        Assert.Equal(110m - 25m, await Balance());
        Assert.Equal(1, await _api.Db(db => db.Set<Refund>().CountAsync()));
    }

    [Fact]
    public async Task A_top_up_already_spent_cannot_be_refunded_to_the_card()
    {
        var orderId = await TopUp(100m);
        await _api.Seed(async db =>
        {
            await using var scope = _api.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IWalletService>().PostAsync(_cardId,
                new WalletPosting(-90m, WalletTransactionType.SessionCharge, "session:spent:charge"));
        });

        var response = await Refund("Order", orderId, 50m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("insufficient_balance", await Code(response));
        Assert.Equal(20m, await Balance());
    }

    [Fact]
    public async Task A_manual_top_up_is_refunded_outside_the_platform_only()
    {
        var orderId = await TopUp(50m, provider: "Manual");

        var summary = await _api.Client(_admin).GetFromJsonAsync<JsonElement>($"/api/Refund/Order/{orderId}");
        var gateway = await Refund("Order", orderId, 20m, method: "Gateway");
        var external = await Refund("Order", orderId, 20m, method: "External");

        Assert.Equal(new[] { "External" }, summary.GetProperty("methods").EnumerateArray().Select(m => m.GetString()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, gateway.StatusCode);
        Assert.Equal("refund_not_supported", await Code(gateway));
        Assert.Equal(HttpStatusCode.OK, external.StatusCode);
        Assert.Equal("External", (await external.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("method").GetString());
        Assert.Equal(60m - 20m, await Balance());
    }

    [Fact]
    public async Task A_session_is_refunded_to_the_wallet_within_its_bill()
    {
        var partial = await Refund("Session", _sessionId, 14m);
        var tooMuch = await Refund("Session", _sessionId, 20.01m);
        var rest = await Refund("Session", _sessionId, 20m);

        Assert.Equal(HttpStatusCode.OK, partial.StatusCode);
        Assert.Equal("Wallet", (await partial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("method").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMuch.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rest.StatusCode);
        Assert.Equal(10m + 34m, await Balance());
        var creditNotes = await _api.Db(db => db.Invoices.Where(i => i.Kind == InvoiceKind.CreditNote).OrderBy(i => i.ID).ToListAsync());
        Assert.Equal(new[] { 14m, 20m }, creditNotes.Select(c => c.AmountInclVat));
        var sessionInvoice = await _api.Db(db => db.Invoices.SingleAsync(i => i.SourceReference == $"session:{_sessionId}"));
        Assert.All(creditNotes, c => Assert.Equal(sessionInvoice.ID, c.CreditedInvoiceID));
        Assert.All(creditNotes, c => Assert.Equal(_sessionId, c.ChargingSessionID));
    }

    [Fact]
    public async Task Customers_and_partners_cannot_refund()
    {
        var orderId = await TopUp(100m);

        var response = await _api.Client(_customer).PostAsJsonAsync($"/api/Refund/Order/{orderId}",
            new { amount = 10m, reason = "Customer request", idempotencyKey = Guid.NewGuid().ToString("N") });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(110m, await Balance());
    }
}

/// <summary>Refund guarantees that need a real database under concurrency, and the providers' refund calls.</summary>
public class RefundConcurrencyTests
{
    [Fact]
    public async Task Concurrent_refunds_of_one_session_never_exceed_its_bill()
    {
        await using var host = new OcppTestHost(relational: true, configure: s => s.AddScoped<IPaymentGateway, DisabledPaymentGateway>());
        int cardId = 0, sessionId = 0, adminId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, "CP-RACE", c => c.PricePerKWh = 2m);
            var user = await Seeds.User(db, "race@test.ma");
            adminId = (await Seeds.User(db, "race-admin@test.ma", role: "Admin")).ID;
            var card = Seeds.Card("RACE-1", 5m, user.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            var session = new ChargingSession
            {
                ConnectorID = (await db.Connectors.SingleAsync()).ID, CardID = card.ID, UserID = user.ID, StartDate = Seeds.Now, EndDate = Seeds.Now.AddMinutes(10),
                ChargedKwhs = 17, EnergyAmount = 34m, TimeAmount = 0, FlatFeeAmount = 0, IdleAmount = 0, TotalAmount = 34m, VatRate = 0.2m,
                ChargingSessionStatus = ChargingSessionStatusEnum.Completed
            };
            db.ChargingSessions.Add(session);
            await db.SaveChangesAsync();
            (cardId, sessionId) = (card.ID, session.ID);
        });

        var attempts = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Task.Run(async () =>
        {
            try
            {
                await host.With<RefundService, RefundDto>(s => s.RefundSessionAsync(sessionId, new RefundRequest(10m, "Race", $"race-key-{i:0000}"), adminId));
                return true;
            }
            catch (RefundRejectedException ex) when (ex.Code == "exceeds_paid")
            {
                return false;
            }
        })));

        Assert.Equal(3, attempts.Count(ok => ok));
        Assert.Equal(30m, await host.Query(db => db.Set<Refund>().SumAsync(r => r.Amount)));
        Assert.Equal(5m + 30m, await host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync()));
        Assert.Equal(3, await host.Query(db => db.Invoices.CountAsync(i => i.Kind == InvoiceKind.CreditNote)));
    }

    private sealed class StubStripe(Func<HttpRequestMessage, string> answer) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request, request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer(request)) };
        }
    }

    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [Fact]
    public async Task Stripe_refunds_the_payment_intent_of_the_checkout_session_once()
    {
        var stripe = new StubStripe(r => r.RequestUri!.AbsolutePath.StartsWith("/v1/checkout/sessions/")
            ? "{\"id\":\"cs_test_1\",\"payment_intent\":\"pi_123\"}"
            : "{\"id\":\"re_456\",\"status\":\"succeeded\"}");
        var options = Options.Create(new PaymentOptions { Stripe = new StripePaymentOptions { SecretKey = "sk_test", ApiBaseUrl = "https://stripe.test" } });
        var gateway = new StripePaymentGateway(options, new Clients(stripe), null!, TimeProvider.System);

        var result = await gateway.RefundAsync(new Order { ID = 7, Amount = 100m, ProviderReference = "cs_test_1" }, 12.34m, "refund-9");

        Assert.Equal(GatewayRefundOutcome.Succeeded, result.Outcome);
        Assert.Equal("re_456", result.ProviderReference);
        var (refund, body) = stripe.Requests.Last();
        Assert.Equal("/v1/refunds", refund.RequestUri!.AbsolutePath);
        Assert.Contains("payment_intent=pi_123", body);
        Assert.Contains("amount=1234", body);
        Assert.Equal("refund-9", refund.Headers.GetValues("Idempotency-Key").Single());
    }

    [Fact]
    public async Task Cmi_and_disabled_providers_say_refunds_go_through_their_back_office()
    {
        var cmi = new CmiPaymentGateway(Options.Create(new PaymentOptions()));

        var result = await cmi.RefundAsync(new Order { ID = 1, Amount = 10m }, 5m, "refund-1");

        Assert.False(cmi.SupportsRefunds);
        Assert.Equal(GatewayRefundOutcome.NotSupported, result.Outcome);
        Assert.Contains("back office", result.Message);
        Assert.Equal(GatewayRefundOutcome.NotSupported, (await new DisabledPaymentGateway().RefundAsync(new Order(), 1m, "x")).Outcome);
    }
}
