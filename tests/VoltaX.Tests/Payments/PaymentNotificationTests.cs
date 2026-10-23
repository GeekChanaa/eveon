using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Services.Payments;

namespace VoltaX.Tests.Payments;

/// <summary>
/// A verified provider notification only credits a wallet when it names a real order of this provider and its
/// amount matches the order; a notification for another provider's order, an unknown order, or a tampered amount
/// is refused, and the same event id credits only once.
/// </summary>
public class PaymentNotificationTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private int _cardId, _orderId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            var user = await Seeds.User(db, "pay@test.ma");
            var card = Seeds.Card("PAY-1", 10m, user.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            var order = new Order { CardID = card.ID, Amount = 100m, Status = RechargeOrderStatus.Pending, Provider = "Sandbox", RechargeDate = Seeds.Now };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            _cardId = card.ID;
            _orderId = order.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private async Task<bool> Handle(PaymentNotification notification)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentService>();
        return await payments.HandleNotificationAsync(notification);
    }

    private Task<decimal> Balance() => _api.Db(db => db.Cards.Where(c => c.ID == _cardId).Select(c => c.Balance).SingleAsync());

    [Fact]
    public async Task A_matching_paid_notification_credits_the_order_amount_once()
    {
        Assert.True(await Handle(new PaymentNotification("evt-1", PaymentOutcome.Paid, OrderId: _orderId, Amount: 100m)));
        Assert.True(await Handle(new PaymentNotification("evt-1", PaymentOutcome.Paid, OrderId: _orderId, Amount: 100m))); // same event, delivered twice

        Assert.Equal(110m, await Balance());
        Assert.Equal(1, await _api.Db(db => db.WalletTransactions.CountAsync(w => w.Reference == $"order:{_orderId}:topup")));
    }

    [Fact]
    public async Task A_tampered_amount_is_refused_and_credits_nothing()
    {
        var accepted = await Handle(new PaymentNotification("evt-2", PaymentOutcome.Paid, OrderId: _orderId, Amount: 100000m));

        Assert.False(accepted);
        Assert.Equal(10m, await Balance());
        Assert.Equal(RechargeOrderStatus.Pending, await _api.Db(db => db.Orders.Where(o => o.ID == _orderId).Select(o => o.Status).SingleAsync()));
    }

    [Fact]
    public async Task A_notification_for_an_unknown_order_is_refused()
    {
        Assert.False(await Handle(new PaymentNotification("evt-3", PaymentOutcome.Paid, OrderId: 987654, Amount: 100m)));
        Assert.Equal(10m, await Balance());
    }

    [Fact]
    public async Task A_notification_for_another_provider_s_order_is_refused()
    {
        await _api.Db(async db =>
        {
            await db.Orders.Where(o => o.ID == _orderId).ExecuteUpdateAsync(s => s.SetProperty(o => o.Provider, "Cmi"));
            return 0;
        });

        Assert.False(await Handle(new PaymentNotification("evt-4", PaymentOutcome.Paid, OrderId: _orderId, Amount: 100m)));
        Assert.Equal(10m, await Balance());
    }

    [Fact]
    public async Task The_webhook_route_answers_404_for_a_different_provider()
    {
        var response = await _api.Client().PostAsync("/api/payments/webhooks/Stripe",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["x"] = "y" }));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
