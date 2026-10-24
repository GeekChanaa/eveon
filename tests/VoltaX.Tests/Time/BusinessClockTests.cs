using VoltaXApi.Services;

namespace VoltaX.Tests.Time;

/// <summary>Business days follow the business time zone; their boundaries are handed to queries in UTC.</summary>
public class BusinessClockTests
{
    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // 23:30 UTC: already the next day east of Greenwich, still the same day in UTC.
    private static readonly FixedTime LateEvening = new(new DateTimeOffset(2026, 10, 2, 23, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Today_and_its_start_follow_the_business_time_zone()
    {
        var casablanca = BusinessClock.ResolveTimeZone("Africa/Casablanca");
        var clock = new BusinessClock(LateEvening, casablanca);
        var offset = casablanca.GetUtcOffset(LateEvening.GetUtcNow());

        Assert.Equal(LateEvening.GetUtcNow().ToOffset(offset).Date, clock.Today);
        var start = clock.StartOfDayUtc(clock.Today);
        Assert.Equal(DateTime.SpecifyKind(clock.Today - offset, DateTimeKind.Utc), start);
        Assert.Equal(DateTimeKind.Utc, start.Kind);
        Assert.Equal(clock.Today, clock.ToLocal(start));
    }

    [Fact]
    public void In_UTC_the_business_calendar_is_the_plain_calendar()
    {
        var clock = new BusinessClock(LateEvening, TimeZoneInfo.Utc);

        Assert.Equal(new DateTime(2026, 10, 2), clock.Today);
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), clock.StartOfMonthUtc(2026, 10));
    }

    [Fact]
    public void An_unknown_time_zone_falls_back_to_UTC()
    {
        Assert.Equal(TimeZoneInfo.Utc, BusinessClock.ResolveTimeZone("Not/AZone"));
    }
}
