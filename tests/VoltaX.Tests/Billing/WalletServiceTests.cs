using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Billing;

public class WalletServiceTests
{
    // Relational: SaveChanges must be atomic for the concurrency retry, as on SQL Server / MariaDB.
    private static async Task<(OcppTestHost Host, int CardId)> HostWithCard(decimal balance)
    {
        var host = new OcppTestHost(relational: true);
        var cardId = 0;
        await host.Seed(async db =>
        {
            var card = Seeds.Card("W-1", balance);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            cardId = card.ID;
        });
        return (host, cardId);
    }

    private static Task<decimal> LedgerSum(OcppTestHost host, int cardId) =>
        host.Query(db => db.WalletTransactions.Where(w => w.CardID == cardId).SumAsync(w => w.Amount));

    [Fact]
    public async Task A_card_created_with_a_balance_gets_an_opening_movement()
    {
        var (host, cardId) = await HostWithCard(42.50m);
        await using var _ = host;

        var movements = await host.Query(db => db.WalletTransactions.Where(w => w.CardID == cardId).ToListAsync());

        var opening = Assert.Single(movements);
        Assert.Equal(WalletTransactionType.Adjustment, opening.Type);
        Assert.Equal(42.50m, opening.Amount);
        Assert.Equal(42.50m, opening.BalanceAfter);
    }

    [Fact]
    public async Task Credits_and_debits_move_the_balance_and_are_recorded()
    {
        var (host, cardId) = await HostWithCard(10m);
        await using var _ = host;

        var topUp = await host.With<IWalletService, WalletTransaction>(w =>
            w.PostAsync(cardId, new WalletPosting(50m, WalletTransactionType.TopUp, "order:1:topup", OrderID: 1)));
        var charge = await host.With<IWalletService, WalletTransaction>(w =>
            w.PostAsync(cardId, new WalletPosting(-12.345m, WalletTransactionType.SessionCharge, "session:7:charge", ChargingSessionID: 7)));

        var card = await host.Query(db => db.Cards.SingleAsync(c => c.ID == cardId));
        Assert.Equal(60m, topUp.BalanceAfter);
        Assert.Equal(-12.35m, charge.Amount); // rounded half away from zero to cents
        Assert.Equal(47.65m, card.Balance);
        Assert.Equal(card.Balance, await LedgerSum(host, cardId));
        Assert.Equal(2, card.BalanceVersion);
    }

    [Fact]
    public async Task Posting_the_same_reference_twice_moves_money_once()
    {
        var (host, cardId) = await HostWithCard(0m);
        await using var _ = host;
        var posting = new WalletPosting(25m, WalletTransactionType.TopUp, "order:9:topup", OrderID: 9);

        var first = await host.With<IWalletService, WalletTransaction>(w => w.PostAsync(cardId, posting));
        var second = await host.With<IWalletService, WalletTransaction>(w => w.PostAsync(cardId, posting));

        Assert.Equal(first.ID, second.ID);
        Assert.Equal(25m, await host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync()));
        Assert.Equal(1, await host.Query(db => db.WalletTransactions.CountAsync(w => w.CardID == cardId)));
    }

    [Fact]
    public async Task A_concurrent_balance_change_is_retried_instead_of_overwritten()
    {
        var (host, cardId) = await HostWithCard(100m);
        await using var _ = host;
        await using var scope = host.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
        var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
        var attempts = 0;

        await wallet.SaveWithRetryAsync(async () =>
        {
            attempts++;
            var card = await db.Cards.SingleAsync(c => c.ID == cardId);
            if (attempts == 1)
                // Another request tops the card up between our read and our save.
                await host.With<IWalletService>(other => other.PostAsync(cardId, new WalletPosting(30m, WalletTransactionType.TopUp, "order:2:topup")));
            wallet.Stage(card, new WalletPosting(-20m, WalletTransactionType.SessionCharge, "session:3:charge"));
        });

        Assert.Equal(2, attempts);
        Assert.Equal(110m, await host.Query(db2 => db2.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync()));
        Assert.Equal(110m, await LedgerSum(host, cardId));
        Assert.Equal(1, await host.Query(db2 => db2.WalletTransactions.CountAsync(w => w.Reference == "session:3:charge")));
    }

    [Fact]
    public async Task A_debit_below_zero_is_recorded_and_raises_a_system_report()
    {
        var host = new OcppTestHost(relational: true);
        await using var _ = host;
        int cardId = 0, connectorId = 0;
        await host.Seed(async db =>
        {
            var chargePoint = await Seeds.ChargePoint(db, "CP-W", c => c.PricePerIdleMinute = 1m);
            connectorId = (await db.Connectors.SingleAsync(c => c.ChargePointID == chargePoint.ID)).ID;
            var card = Seeds.Card("W-NEG", 5m);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            cardId = card.ID;
        });

        var covered = await host.With<ICardService, bool>(c => c.SubstractAmountFromCardByIdleMinutes(cardId, 8, connectorId, chargingSessionID: 11));

        Assert.False(covered);
        Assert.Equal(-3m, await host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync()));
        Assert.Single(host.SystemReports.CallsTo(nameof(ISystemReportService.HandleReport)));
        var idle = await host.Query(db => db.WalletTransactions.SingleAsync(w => w.Reference == "session:11:idle"));
        Assert.Equal(WalletTransactionType.IdleFee, idle.Type);
        Assert.Equal(-8m, idle.Amount);
    }

    [Fact]
    public async Task Top_ups_require_a_positive_amount_and_credit_once_per_order()
    {
        var (host, cardId) = await HostWithCard(0m);
        await using var _ = host;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => host.With<ICardService>(c => c.AddAmountToCard(cardId, 0m, 5)));
        await host.With<ICardService>(c => c.AddAmountToCard(cardId, 100m, 5));
        await host.With<ICardService>(c => c.AddAmountToCard(cardId, 100m, 5));

        Assert.Equal(100m, await host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync()));
    }
}
