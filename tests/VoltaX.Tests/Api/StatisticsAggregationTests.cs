using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Api;

/// <summary>
/// The statistics are aggregated by the database; these tests compare them with the row-by-row computation they
/// replace (every session loaded and priced by <see cref="ChargingSession.TotalPriceWithVAT"/>), on SQLite.
/// </summary>
public class StatisticsAggregationTests : IAsyncLifetime
{
    private const decimal Vat = 0.2m;
    private const double Grace = 300;
    private static readonly DateTime Now = new(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc);

    private sealed class FixedTime(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private int _partnerId, _otherPartnerId;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        var partner = new Partner { Name = "P1", Description = "", Email = "p1@x.ma", Phone = "1", PartnerIdentificationNumber = "P1" };
        var other = new Partner { Name = "P2", Description = "", Email = "p2@x.ma", Phone = "2", PartnerIdentificationNumber = "P2" };
        db.Partners.AddRange(partner, other);
        await db.SaveChangesAsync();
        (_partnerId, _otherPartnerId) = (partner.ID, other.ID);

        var connectors = new List<int>();
        foreach (var (owner, name) in new[] { (partner.ID, "S1"), (partner.ID, "S2"), (other.ID, "S3") })
        {
            var station = await Seeds.Station(db, owner, name);
            var point = await Seeds.ChargePoint(db, "CP-" + name, _ => { }, station.ID);
            connectors.Add(db.Connectors.Single(c => c.ChargePointID == point.ID).ID);
        }
        var card = Seeds.Card("C-1", 0);
        db.Cards.Add(card);
        await db.SaveChangesAsync();

        // Every kind of row: billed with and without idle amounts, legacy rows priced on time (idle inside and past
        // the grace period, stored idle amount), missing values, running sessions, sessions around midnight and
        // month ends, over 14 months.
        var random = new Random(42);
        var sessions = new List<ChargingSession>();
        for (var i = 0; i < 600; i++)
        {
            var start = Now.AddMinutes(-random.Next(0, 60 * 24 * 420));
            var kind = i % 6;
            sessions.Add(new ChargingSession
            {
                ConnectorID = connectors[i % connectors.Count],
                CardID = card.ID,
                StartDate = start,
                EndDate = i % 17 == 0 ? null : start.AddMinutes(20 + random.Next(0, 200)),
                ChargedMinutes = kind == 5 ? null : Math.Round(random.NextDouble() * 180, 3),
                IdleMinutes = kind == 4 ? null : random.Next(0, 12),
                ChargedKwhs = kind == 5 ? null : Math.Round(random.NextDouble() * 60, 3),
                PricePerMinute = kind == 3 ? null : 0.35m + i % 4 * 0.05m,
                PricePerIdleMinute = 0.2m,
                EnergyAmount = kind < 2 ? 10.15m + i % 7 : null,
                TimeAmount = kind < 2 ? 2.35m : null,
                FlatFeeAmount = kind == 0 ? 1m : null,
                IdleAmount = kind == 0 || kind == 4 && i % 2 == 0 ? 0.8m + i % 3 : null,
                TotalAmount = kind < 2 ? 12.5m + i % 7 : null
            });
        }
        // Sessions just after a business midnight that is not on a whole UTC hour (Kolkata, UTC+5:30: 18:30 UTC),
        // one of them on the first day of a month, and one just after midnight in Casablanca (UTC+1: 23:00 UTC).
        var afterMidnight = Enumerable.Range(1, 4).Select(d => Now.Date.AddDays(-d).AddHours(18).AddMinutes(40))
            .Append(new DateTime(2026, 9, 30, 18, 45, 0, DateTimeKind.Utc))
            .Append(Now.Date.AddDays(-3).AddMinutes(-10));
        foreach (var start in afterMidnight)
            sessions.Add(new ChargingSession { ConnectorID = connectors[0], CardID = card.ID, StartDate = start, EndDate = start.AddMinutes(40),
                ChargedMinutes = 40, ChargedKwhs = 9, PricePerMinute = 0.5m, EnergyAmount = 18, TimeAmount = 2, FlatFeeAmount = 0, TotalAmount = 20 });
        db.ChargingSessions.AddRange(sessions);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private VoltaXApiDbContext NewContext() => new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(_connection).Options);

    private static BusinessClock Clock(string zone) => new(new FixedTime(Now), BusinessClock.ResolveTimeZone(zone));

    private async Task<List<(ChargingSession Session, int? PartnerId)>> AllSessions()
    {
        await using var db = NewContext();
        return (await db.ChargingSessions.AsNoTracking().Select(s => new { s, s.Connector!.ChargePoint!.ChargingStation!.PartnerID }).ToListAsync())
            .Select(x => (x.s, x.PartnerID)).ToList();
    }

    private static double Price(ChargingSession s) => (double)s.TotalPriceWithVAT(Vat, (int)Grace);

    private static void Same<TKey>(Dictionary<TKey, double> expected, Dictionary<TKey, double> actual) where TKey : notnull
    {
        Assert.Equal(expected.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (var (key, value) in expected)
            Assert.Equal(value, actual[key], 6);
    }

    [Theory]
    [InlineData("Africa/Casablanca")]
    [InlineData("Asia/Kolkata")]
    public async Task Operator_revenue_matches_pricing_every_session(string zone)
    {
        var clock = Clock(zone);
        await using var db = NewContext();
        var service = new StatisticsService(db, clock);
        var ended = (await AllSessions()).Select(x => x.Session).Where(s => s.EndDate != null).ToList();
        var todayFrom = clock.StartOfDayUtc(clock.Today);
        var todayTo = clock.StartOfDayUtc(clock.Today.AddDays(1));
        var dailyFrom = clock.StartOfDayUtc(clock.Today.AddDays(-30));
        var yearFrom = clock.StartOfDayUtc(clock.Today.AddYears(-1));

        Assert.Equal(ended.Sum(Price), await service.GetTotalRevenueAsync(Vat, Grace), 6);
        Assert.Equal(ended.Where(s => s.StartDate >= todayFrom && s.StartDate < todayTo).Sum(Price), await service.GetTotalRevenueTodayAsync(Vat, Grace), 6);
        Same(ended.Where(s => s.StartDate >= dailyFrom).GroupBy(s => clock.ToLocal(s.StartDate).Date).ToDictionary(g => g.Key, g => g.Sum(Price)),
            await service.GetDailyRevenueLast30DaysAsync(Vat, Grace));
        Same(ended.Where(s => s.StartDate >= yearFrom).GroupBy(s => $"{clock.ToLocal(s.StartDate).Month}-{clock.ToLocal(s.StartDate).Year}").ToDictionary(g => g.Key, g => g.Sum(Price)),
            await service.GetMonthlyRevenueLastYearAsync(Vat, Grace));
    }

    [Theory]
    [InlineData("Africa/Casablanca")]
    [InlineData("Asia/Kolkata")]
    public async Task Partner_statistics_match_every_session(string zone)
    {
        var clock = Clock(zone);
        await using var db = NewContext();
        var service = new PartnerStatisticsService(db, clock);
        var all = await AllSessions();
        var mine = all.Where(x => x.PartnerId == _partnerId && x.Session.EndDate != null).Select(x => x.Session).ToList();
        Assert.Contains(all, x => x.PartnerId == _otherPartnerId);
        var todayFrom = clock.StartOfDayUtc(clock.Today);
        var todayTo = clock.StartOfDayUtc(clock.Today.AddDays(1));
        var today = mine.Where(s => s.StartDate >= todayFrom && s.StartDate < todayTo).ToList();
        var last30 = mine.Where(s => s.StartDate >= clock.StartOfDayUtc(clock.Today.AddDays(-30))).ToList();
        var lastYear = mine.Where(s => s.StartDate >= clock.StartOfDayUtc(clock.Today.AddYears(-1))).ToList();
        Dictionary<DateTime, double> Daily(Func<ChargingSession, double> value) => last30.GroupBy(s => clock.ToLocal(s.StartDate).Date).ToDictionary(g => g.Key, g => g.Sum(value));
        Dictionary<string, double> Monthly(Func<ChargingSession, double> value) =>
            lastYear.GroupBy(s => $"{clock.ToLocal(s.StartDate).Month}-{clock.ToLocal(s.StartDate).Year}").ToDictionary(g => g.Key, g => g.Sum(value));

        Assert.Equal(mine.Sum(Price), await service.GetTotalRevenueForPartnerAsync(_partnerId, Vat, Grace), 6);
        Assert.Equal(today.Sum(Price), await service.GetTotalRevenueForPartnerTodayAsync(_partnerId, Vat, Grace), 6);
        Same(Daily(Price), await service.GetDailyRevenueForPartnerLast30DaysAsync(_partnerId, Vat, Grace));
        Same(Monthly(Price), await service.GetMonthlyRevenueForPartnerLastYearAsync(_partnerId, Vat, Grace));

        Assert.Equal(mine.Count, await service.GetTotalChargingSessionsForPartnerAsync(_partnerId));
        Assert.Equal(today.Count, await service.GetTotalChargingSessionsForPartnerTodayAsync(_partnerId));
        Same(Daily(_ => 1), await service.GetDailyChargingSessionsForPartnerLast30DaysAsync(_partnerId));
        Same(Monthly(_ => 1), await service.GetMonthlyChargingSessionsForPartnerLastYearAsync(_partnerId));

        Assert.Equal(mine.Sum(s => s.ChargedMinutes ?? 0), await service.GetTotalChargedMinutesForPartnerAsync(_partnerId), 6);
        Same(Daily(s => s.ChargedMinutes ?? 0), await service.GetDailyChargedMinutesForPartnerLast30DaysAsync(_partnerId));
        Same(Monthly(s => s.ChargedMinutes ?? 0), await service.GetMonthlyChargedMinutesForPartnerLastYearAsync(_partnerId));

        Assert.Equal(mine.Sum(s => s.ChargedKwhs ?? 0), await service.GetTotalEnergyConsumedForPartnerAsync(_partnerId), 6);
        Same(Daily(s => s.ChargedKwhs ?? 0), await service.GetDailyEnergyConsumedForPartnerLast30DaysAsync(_partnerId));
        Same(Monthly(s => s.ChargedKwhs ?? 0), await service.GetMonthlyEnergyConsumedForPartnerLastYearAsync(_partnerId));
    }

    [Fact]
    public void Half_hour_time_zones_are_grouped_by_minute()
    {
        Assert.True(SessionAggregation.HasWholeHourOffsets(BusinessClock.ResolveTimeZone("Africa/Casablanca")));
        Assert.True(SessionAggregation.HasWholeHourOffsets(TimeZoneInfo.Utc));
        Assert.False(SessionAggregation.HasWholeHourOffsets(BusinessClock.ResolveTimeZone("Asia/Kolkata")));
    }

    [Fact]
    public async Task No_sessions_give_zero_totals()
    {
        await using var db = NewContext();
        var none = db.ChargingSessions.Where(s => s.ID < 0);

        var totals = await SessionAggregation.TotalAsync(none, new SessionPricing(Vat, Grace));
        var buckets = await SessionAggregation.ByStartAsync(none, TimeZoneInfo.Utc, new SessionPricing(Vat, Grace));

        Assert.Equal(SessionTotals.Empty, totals);
        Assert.Empty(buckets);
    }
}
