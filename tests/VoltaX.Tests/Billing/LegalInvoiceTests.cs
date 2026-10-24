using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;
using VoltaXApi.Services;
using VoltaXApi.Services.Billing;

namespace VoltaX.Tests.Billing;

/// <summary>
/// Legal invoices: one gap-free number per series and year, assigned once per billed event, the PDF stored at
/// issue and served by every download endpoint.
/// </summary>
public class LegalInvoiceTests
{
    private const string Cp = "CP-INV";
    private static readonly DateTime T0 = Seeds.Now;

    private static int Year(OcppTestHost host) =>
        host.Provider.GetRequiredService<IBusinessClock>().LocalNow.Year;

    private static async Task<(OcppTestHost Host, int CardId, int UserId)> Network(decimal balance = 100m)
    {
        var host = new OcppTestHost(relational: true);
        int cardId = 0, userId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c =>
            {
                c.PricePerKWh = 2.50m;
                c.PricePerMinute = 0.10m;
                c.FlatFee = 1.00m;
                c.PricePerIdleMinute = 0.50m;
            });
            var user = await Seeds.User(db, "invoice@test.ma");
            var card = Seeds.Card("TAG-INV", balance, user.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            (cardId, userId) = (card.ID, user.ID);
        });
        host.Sender.Versions[Cp] = OcppTestHost.Ocpp201;
        return (host, cardId, userId);
    }

    private static async Task<int> PaidOrder(OcppTestHost host, int cardId, decimal amount, RechargeOrderStatus status = RechargeOrderStatus.Completed)
    {
        var id = 0;
        await host.Seed(async db =>
        {
            var order = new Order { CardID = cardId, Amount = amount, Status = status, Provider = "Manual", RechargeDate = T0, PaidAt = T0 };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            id = order.ID;
        });
        return id;
    }

    [Fact]
    public async Task Top_up_invoices_are_numbered_without_gaps_even_when_issued_concurrently()
    {
        var (host, cardId, _) = await Network();
        await using var _ = host;
        var orders = new List<int>();
        for (var i = 0; i < 12; i++)
            orders.Add(await PaidOrder(host, cardId, 20m + i));

        var issued = await Task.WhenAll(orders.Select(id => Task.Run(() => host.With<IInvoiceService, Invoice?>(s => s.IssueForOrderAsync(id)))));

        var year = Year(host);
        Assert.Equal(Enumerable.Range(1, 12).Select(n => $"EV-{year}-{n:000000}"), issued.Select(i => i!.Number).OrderBy(n => n));
        Assert.Equal(12, await host.Query(db => db.InvoiceSequences.Where(s => s.Series == "EV" && s.Year == year).Select(s => s.LastNumber).SingleAsync()));
        Assert.Equal(12, await host.Query(db => db.InvoiceFiles.CountAsync()));
    }

    [Fact]
    public async Task An_event_is_invoiced_once()
    {
        var (host, cardId, userId) = await Network();
        await using var _ = host;
        var orderId = await PaidOrder(host, cardId, 120m);

        var first = await host.With<IInvoiceService, Invoice?>(s => s.IssueForOrderAsync(orderId));
        var again = await host.With<IInvoiceService, Invoice?>(s => s.IssueForOrderAsync(orderId));

        Assert.Equal(first!.Number, again!.Number);
        Assert.Equal(1, await host.Query(db => db.Invoices.CountAsync()));
        Assert.Equal(1, await host.Query(db => db.InvoiceSequences.Select(s => s.LastNumber).SingleAsync()));
        Assert.Equal(userId, first.UserID);
        Assert.Equal(120m, first.AmountInclVat);
        Assert.Equal(100m, first.AmountExclVat);
        Assert.Equal(20m, first.VatAmount);
        Assert.Equal("fr", first.Language);
        var pdf = await host.Query(db => db.InvoiceFiles.Where(f => f.InvoiceID == first.ID).Select(f => f.Content).SingleAsync());
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task Unpaid_top_ups_and_running_sessions_are_not_invoiced()
    {
        var (host, cardId, _) = await Network();
        await using var _ = host;
        var pending = await PaidOrder(host, cardId, 50m, RechargeOrderStatus.Pending);
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-INV", 0, T0);
        var sessionId = await host.Query(db => db.ChargingSessions.Select(s => s.ID).SingleAsync());

        Assert.Null(await host.With<IInvoiceService, Invoice?>(s => s.IssueForOrderAsync(pending)));
        Assert.Null(await host.With<IInvoiceService, Invoice?>(s => s.IssueForSessionAsync(sessionId)));
        Assert.False(await host.Query(db => db.InvoiceSequences.AnyAsync()));
    }

    [Fact]
    public async Task A_session_is_invoiced_when_the_car_leaves_with_its_idle_fee()
    {
        var (host, _, userId) = await Network();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-INV", 0, T0);
        await charger.Ended("TX-1", 10000, T0.AddMinutes(60));
        Assert.False(await host.Query(db => db.Invoices.AnyAsync())); // the car is still plugged in

        await charger.Status("Available", T0.AddMinutes(80));

        var session = await host.Query(db => db.ChargingSessions.SingleAsync());
        var invoice = await host.Query(db => db.Invoices.SingleAsync());
        Assert.Equal($"session:{session.ID}", invoice.SourceReference);
        Assert.Equal(session.TotalAmount, invoice.AmountInclVat);
        Assert.Equal(25m + 6m + 1m + 10m, invoice.AmountInclVat);
        Assert.Equal(invoice.AmountInclVat, invoice.AmountExclVat + invoice.VatAmount);
        Assert.Equal(CostCalculator.SplitVat(42m, 0.2m).ExclVat, invoice.AmountExclVat);
        Assert.Equal(userId, invoice.UserID);
        Assert.Equal(InvoiceKind.Invoice, invoice.Kind);
    }

    [Fact]
    public async Task An_idle_fee_settled_after_the_invoice_gets_its_own_invoice()
    {
        var (host, _, _) = await Network();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-INV", 0, T0);
        await charger.Ended("TX-1", 10000, T0.AddMinutes(60));
        var sessionId = await host.Query(db => db.ChargingSessions.Select(s => s.ID).SingleAsync());

        // Downloaded while the car is still plugged in: invoiced as billed so far.
        var main = await host.With<IInvoiceService, Invoice?>(s => s.IssueForSessionAsync(sessionId));
        Assert.Equal(32m, main!.AmountInclVat);

        await charger.Status("Available", T0.AddMinutes(80));

        var invoices = await host.Query(db => db.Invoices.OrderBy(i => i.ID).ToListAsync());
        Assert.Equal(2, invoices.Count);
        Assert.Equal($"session:{sessionId}:idle", invoices[1].SourceReference);
        Assert.Equal(10m, invoices[1].AmountInclVat);
        var year = Year(host);
        Assert.Equal(new[] { $"EV-{year}-000001", $"EV-{year}-000002" }, invoices.Select(i => i.Number));
        Assert.Equal(await host.Query(db => db.ChargingSessions.Select(s => s.TotalAmount).SingleAsync()), invoices.Sum(i => i.AmountInclVat));
    }

    [Fact]
    public async Task The_sweeper_invoices_sessions_whose_charger_never_freed_the_connector()
    {
        var (host, cardId, _) = await Network();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-INV", 0, T0);
        await charger.Ended("TX-1", 4000, T0.AddMinutes(20));
        var orderId = await PaidOrder(host, cardId, 60m);
        var endedAt = await host.Query(db => db.ChargingSessions.Select(s => s.EndDate!.Value).SingleAsync());

        var early = await host.With<IServiceProvider, int>(sp => InvoiceSweeper.SweepAsync(sp, endedAt.AddHours(1)));
        var late = await host.With<IServiceProvider, int>(sp => InvoiceSweeper.SweepAsync(sp, endedAt.AddHours(25)));

        Assert.Equal(1, early); // the top-up only
        Assert.Equal(1, late);  // then the session, a day later
        Assert.Equal(2, await host.Query(db => db.Invoices.CountAsync()));
        Assert.True(await host.Query(db => db.Invoices.AnyAsync(i => i.OrderID == orderId)));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    [InlineData("ar")]
    public void Invoices_and_credit_notes_render_in_every_language(string language)
    {
        var company = new CompanyInformations
        {
            CompanyName = "EVEON SARL", Address = "Bd Anfa, Casablanca", ICE = "001234567000089", IF = "12345678", RC = "123456", TP = "34567890",
            CNSS = "1234567", EmailContactAddress = "contact@eveon.ma", FixPhone = "+212 5 22 00 00 00", Website = "eveon.ma"
        };
        foreach (var kind in new[] { InvoiceKind.Invoice, InvoiceKind.CreditNote })
        {
            var pdf = InvoicePdf.Render(new InvoicePdfModel(kind, "EV-2026-000001", T0, language, "MAD", "سعيد العلمي", "said@test.ma", "4242",
                new[] { BillingTexts.Get(language, "Session") + " #12" },
                new[] { new InvoiceLine(BillingTexts.Get(language, "EnergyLine"), null, "12 kWh", "2,50 MAD", 30m), new InvoiceLine(BillingTexts.Get(language, "TimeLine"), null, "30 min", null, 4m) },
                0.2m, 28.33m, 5.67m, 34m, BillingTexts.Get(language, "PaidByWallet"), kind == InvoiceKind.CreditNote ? "EV-2026-000001" : null, "Geste commercial"), company);

            Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        }
    }
}

/// <summary>The download endpoints of customers and operators return the stored invoice.</summary>
public class InvoiceApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _customer = null!, _other = null!, _admin = null!;
    private int _cardId, _sessionId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _customer = await Seeds.User(db, "inv-customer@test.ma");
            _other = await Seeds.User(db, "inv-other@test.ma");
            _admin = await Seeds.User(db, "inv-admin@test.ma", role: "Admin");
            var chargePoint = await Seeds.ChargePoint(db, "CP-API-INV", c => c.PricePerKWh = 2m);
            var card = Seeds.Card("INV-1", 100m, _customer.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            _cardId = card.ID;
            var connector = await db.Connectors.SingleAsync();
            var session = new ChargingSession
            {
                ConnectorID = connector.ID, CardID = card.ID, UserID = _customer.ID, StartDate = Seeds.Now, EndDate = Seeds.Now.AddMinutes(30),
                ChargedKwhs = 10, ChargedMinutes = 30, PricePerKWh = 2m, PricePerMinute = 0, FlatFee = 0, PricePerIdleMinute = 0, VatRate = 0.2m,
                EnergyAmount = 20m, TimeAmount = 0, FlatFeeAmount = 0, IdleAmount = 0, IdleMinutes = 0, TotalAmount = 20m, BilledAt = Seeds.Now.AddMinutes(30),
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

    [Fact]
    public async Task Customers_download_the_same_stored_invoice_every_time()
    {
        var client = _api.Client(_customer);

        var first = await client.GetAsync($"/api/my/sessions/{_sessionId}/invoice");
        var second = await client.GetAsync($"/api/my/sessions/{_sessionId}/invoice");
        var admin = await _api.Client(_admin).GetAsync($"/api/ChargingSession/GetChargingSessionInvoice/{_sessionId}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("application/pdf", first.Content.Headers.ContentType!.MediaType);
        var bytes = await first.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, await second.Content.ReadAsByteArrayAsync());
        Assert.Equal(bytes, await admin.Content.ReadAsByteArrayAsync());
        Assert.StartsWith("EV-", first.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        Assert.Equal(1, await _api.Db(db => db.Invoices.CountAsync()));
    }

    [Fact]
    public async Task Customers_list_only_their_own_invoices()
    {
        await _api.Client(_customer).GetAsync($"/api/my/sessions/{_sessionId}/invoice");
        var invoiceId = await _api.Db(db => db.Invoices.Select(i => i.ID).SingleAsync());

        var mine = await _api.Client(_customer).GetFromJsonAsync<JsonElement>("/api/my/invoices");
        var theirs = await _api.Client(_other).GetFromJsonAsync<JsonElement>("/api/my/invoices");

        Assert.Equal(1, mine.GetProperty("total").GetInt32());
        Assert.Equal(20m, mine.GetProperty("items")[0].GetProperty("amountInclVat").GetDecimal());
        Assert.Equal(0, theirs.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await _api.Client(_other).GetAsync($"/api/my/invoices/{invoiceId}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.Client(_other).GetAsync($"/api/my/sessions/{_sessionId}/invoice")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _api.Client(_customer).GetAsync($"/api/my/invoices/{invoiceId}/pdf")).StatusCode);
    }

    [Fact]
    public async Task A_manual_top_up_is_invoiced_and_the_invoice_is_emailed()
    {
        var response = await _api.Client(_admin).PostAsJsonAsync("/api/Order/CreateRechargeOrder", new { cardID = _cardId, amount = 240m, note = "cash" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orderId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetInt32();

        var invoice = await _api.Db(db => db.Invoices.SingleAsync(i => i.OrderID == orderId));
        Assert.Equal(240m, invoice.AmountInclVat);
        Assert.Equal(200m, invoice.AmountExclVat);
        var mail = Assert.Single(_api.Mail.CallsTo(nameof(IMailService.SendHtmlEmailAsync)));
        var request = (MailRequest)mail[0]!;
        Assert.Equal(new[] { "inv-customer@test.ma" }, request.ToEmails);
        Assert.Contains(invoice.Number, request.Subject);
        Assert.Equal($"{invoice.Number}.pdf", Assert.Single(request.Attachments!).FileName);

        var download = await _api.Client(_admin).GetAsync($"/api/Order/GetOrderInvoice/{orderId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var stored = await _api.Db(db => db.InvoiceFiles.Where(f => f.InvoiceID == invoice.ID).Select(f => f.Content).SingleAsync());
        Assert.Equal(stored, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task The_register_lists_documents_for_operators_only()
    {
        await _api.Client(_customer).GetAsync($"/api/my/sessions/{_sessionId}/invoice");

        var register = await _api.Client(_admin).GetFromJsonAsync<JsonElement>("/api/Invoice/Search");
        var journal = await _api.Client(_admin).GetStringAsync("/api/Invoice/Journal");

        Assert.Equal(1, register.GetProperty("total").GetInt32());
        Assert.Contains(register.GetProperty("items")[0].GetProperty("number").GetString()!, journal);
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.Client(_customer).GetAsync("/api/Invoice/Search")).StatusCode);
    }
}
