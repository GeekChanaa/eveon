using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Api;

/// <summary>The order counters are one grouped query each; they must equal the per-day queries they replace.</summary>
public class OrderStatisticsTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc);

    private sealed class FixedTime(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private List<Order> _orders = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
        var card = Seeds.Card("C-1", 0);
        db.Cards.Add(card);
        await db.SaveChangesAsync();
        var random = new Random(7);
        var orders = Enumerable.Range(0, 800).Select(i => new Order
        {
            CardID = card.ID,
            Amount = 5m + i % 13 * 2.5m,
            Status = (RechargeOrderStatus)(i % 3),
            RechargeDate = Now.AddMinutes(-random.Next(0, 60 * 24 * 400))
        }).ToList();
        // Just after business midnights that are not whole UTC hours (Kolkata, UTC+5:30), and a deleted order.
        orders.AddRange(Enumerable.Range(1, 5).Select(d => new Order { CardID = card.ID, Amount = 7, RechargeDate = Now.Date.AddDays(-d).AddHours(18).AddMinutes(35) }));
        orders.Add(new Order { CardID = card.ID, Amount = 1000, RechargeDate = Now.AddHours(-1), IsDeleted = true });
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
        _orders = orders.Where(o => !o.IsDeleted).ToList();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private VoltaXApiDbContext NewContext() => new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(_connection).Options);

    [Theory]
    [InlineData("Africa/Casablanca")]
    [InlineData("Asia/Kolkata")]
    public async Task Series_match_one_query_per_period(string zone)
    {
        var clock = new BusinessClock(new FixedTime(Now), BusinessClock.ResolveTimeZone(zone));
        await using var db = NewContext();
        var service = new OrderStatisticsService(db, clock);
        IEnumerable<Order> In(DateTime from, DateTime to) => _orders.Where(o => o.RechargeDate >= from && o.RechargeDate < to);
        List<(DateTime, DateTime)> Days(int count) => Enumerable.Range(0, count).Select(i => clock.Today.AddDays(i - count + 1))
            .Select(d => (clock.StartOfDayUtc(d), clock.StartOfDayUtc(d.AddDays(1)))).ToList();
        var months = new List<(DateTime, DateTime)>();
        for (var month = clock.Today.AddYears(-1).AddMonths(1); month <= clock.Today; month = month.AddMonths(1))
            months.Add((clock.StartOfMonthUtc(month.Year, month.Month), clock.StartOfDayUtc(new DateTime(month.Year, month.Month, 1).AddMonths(1))));

        Assert.Equal(_orders.Sum(o => o.Amount), await service.RechargedAmountAsync());
        Assert.Equal(Days(1).Sum(d => In(d.Item1, d.Item2).Sum(o => o.Amount)), await service.RechargedAmountTodayAsync());
        Assert.Equal(Days(1).Sum(d => In(d.Item1, d.Item2).Count()), await service.CountTodayAsync());
        Assert.Equal(_orders.Where(o => o.RechargeDate >= Now.AddDays(-10) && o.RechargeDate <= Now).Sum(o => o.Amount),
            await service.RechargedAmountBetweenAsync(Now.AddDays(-10), Now));
        Assert.Equal(Days(30).Select(d => In(d.Item1, d.Item2).Sum(o => o.Amount)), await service.RechargedAmountPerDayAsync(30));
        Assert.Equal(Days(30).Select(d => (double)In(d.Item1, d.Item2).Count()), await service.CountPerDayAsync(30));
        Assert.Equal(Days(7).Select(d => (double)In(d.Item1, d.Item2).Count()), await service.CountPerDayAsync(7));
        Assert.Equal(12, months.Count);
        Assert.Equal(months.Select(m => (double)In(m.Item1, m.Item2).Count()), await service.CountPerMonthAsync(12));
    }
}
