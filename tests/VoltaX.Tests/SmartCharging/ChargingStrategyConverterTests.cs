using VoltaXApi.Models;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;
using VoltaXApi.SmartCharging;
using ValidationException = VoltaXApi.Exceptions.ValidationException;

namespace VoltaX.Tests.SmartCharging;

/// <summary>Charging strategies written in local wall-clock time, turned into OCPP profile schedules in UTC.</summary>
public class ChargingStrategyConverterTests
{
    private static readonly DateTime T0 = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);

    // Casablanca is UTC+1 without DST outside Ramadan. Host time zone data differs (Windows "Morocco Standard Time" may
    // say UTC+0 in October), so DST-free cases use an explicit +01:00 zone; the host zone is only checked for consistency.
    private static readonly TimeZoneInfo Casablanca = TimeZoneInfo.CreateCustomTimeZone("Africa/Casablanca+01", TimeSpan.FromHours(1), "Casablanca (+01)", "Casablanca (+01)");
    private static readonly TimeZoneInfo Paris = BusinessClock.ResolveTimeZone("Europe/Paris");

    private static readonly ChargingStrategy OffPeak = new() { Name = "Off-peak night boost", Kind = ChargingProfileKindEnum.Recurring, RecurrencyKind = ChargingProfileRecurrencyEnum.Daily };

    private static List<ChargingStrategyPeriod> SeededOffPeak() =>
        SmartChargingJson.Deserialize<ChargingStrategyPeriod>("[{\"startSeconds\":0,\"limit\":32},{\"startSeconds\":21600,\"limit\":10},{\"startSeconds\":79200,\"limit\":32}]");

    private static readonly (int, double)[] FullNightTenAmpsDay = { (0, 32.0), (21600, 10.0), (79200, 32.0) };

    [Fact]
    public void The_host_resolves_the_Casablanca_and_Paris_time_zones()
    {
        Assert.NotEqual(TimeZoneInfo.Utc, BusinessClock.ResolveTimeZone("Africa/Casablanca"));
        Assert.NotEqual(TimeZoneInfo.Utc, Paris);
    }

    [Fact]
    public void The_seeded_strategy_JSON_parses()
    {
        var periods = SeededOffPeak();
        Assert.Equal(3, periods.Count);
        Assert.Equal(new ChargingStrategyPeriod(21600, 10), periods[1]);
    }

    [Fact]
    public void A_daily_strategy_starts_at_local_midnight_in_UTC()
    {
        var shape = ChargingStrategyConverter.ToProfileShape(OffPeak, SeededOffPeak(), Casablanca, new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc), shape.StartSchedule); // 14 Oct 00:00 local
        Assert.Equal(DateTimeKind.Utc, shape.StartSchedule.Kind);
        Assert.Equal(86_400, shape.Duration);
        Assert.Equal(ChargingProfileKindEnum.Recurring, shape.Kind);
        Assert.Equal(ChargingProfileRecurrencyEnum.Daily, shape.RecurrencyKind);
        Assert.Equal(FullNightTenAmpsDay, shape.Periods.Select(p => (p.StartPeriod, p.Limit)));
    }

    [Fact]
    public void Late_evening_UTC_is_already_the_next_local_day()
    {
        var shape = ChargingStrategyConverter.ToProfileShape(OffPeak, SeededOffPeak(), Casablanca, new DateTime(2026, 10, 14, 23, 30, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 10, 14, 23, 0, 0, DateTimeKind.Utc), shape.StartSchedule);
    }

    [Fact]
    public void With_the_host_Casablanca_zone_the_start_is_its_local_midnight_less_its_offset()
    {
        var host = BusinessClock.ResolveTimeZone("Africa/Casablanca");
        var now = new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);

        var shape = ChargingStrategyConverter.ToProfileShape(OffPeak, SeededOffPeak(), host, now);

        Assert.Equal(TimeZoneInfo.ConvertTimeFromUtc(now, host).Date - host.GetUtcOffset(shape.StartSchedule), shape.StartSchedule);
        Assert.Equal(DateTimeKind.Utc, shape.StartSchedule.Kind);
    }

    [Fact]
    public void A_period_crossing_midnight_is_wrapped_to_start_the_day()
    {
        var shape = ChargingStrategyConverter.ToProfileShape(OffPeak, new List<ChargingStrategyPeriod> { new(21600, 10), new(79200, 32) }, Casablanca, T0);
        Assert.Equal(FullNightTenAmpsDay, shape.Periods.Select(p => (p.StartPeriod, p.Limit)));
    }

    [Theory]
    [InlineData("2026-07-01T12:00:00Z", "2026-06-30T22:00:00Z")] // summer, UTC+2
    [InlineData("2026-12-01T12:00:00Z", "2026-11-30T23:00:00Z")] // winter, UTC+1
    [InlineData("2026-03-29T12:00:00Z", "2026-03-28T23:00:00Z")] // DST change day: midnight is still UTC+1
    public void Paris_local_midnight_follows_daylight_saving_time(string now, string expectedStart)
    {
        var shape = ChargingStrategyConverter.ToProfileShape(OffPeak, SeededOffPeak(), Paris, DateTime.Parse(now).ToUniversalTime());
        Assert.Equal(DateTime.Parse(expectedStart).ToUniversalTime(), shape.StartSchedule);
    }

    [Fact]
    public void A_wall_clock_time_in_the_DST_gap_moves_to_the_first_valid_time()
    {
        // 02:30 does not exist in Paris on 29 Mar 2026: it becomes 03:00 local = 01:00 UTC.
        Assert.Equal(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc), ChargingStrategyConverter.LocalToUtc(new DateTime(2026, 3, 29, 2, 30, 0), Paris));
    }

    [Fact]
    public void A_weekly_strategy_starts_on_local_Monday_of_the_current_week()
    {
        var weekly = new ChargingStrategy { Name = "Weekend", Kind = ChargingProfileKindEnum.Recurring, RecurrencyKind = ChargingProfileRecurrencyEnum.Weekly };

        var shape = ChargingStrategyConverter.ToProfileShape(weekly, new List<ChargingStrategyPeriod> { new(0, 16), new(5 * 86_400, 32) }, Casablanca,
            new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc)); // Wednesday

        Assert.Equal(new DateTime(2026, 10, 11, 23, 0, 0, DateTimeKind.Utc), shape.StartSchedule); // Mon 12 Oct 00:00 local
        Assert.Equal(7 * 86_400, shape.Duration);
    }

    [Fact]
    public void An_absolute_strategy_starts_now_in_whole_seconds_without_duration()
    {
        var fairShare = new ChargingStrategy { Name = "Fair share 16A", Kind = ChargingProfileKindEnum.Absolute };

        var shape = ChargingStrategyConverter.ToProfileShape(fairShare, new List<ChargingStrategyPeriod> { new(0, 16) }, Casablanca, T0.AddTicks(1234567));

        Assert.Equal(ChargingProfileKindEnum.Absolute, shape.Kind);
        Assert.Equal(T0, shape.StartSchedule);
        Assert.Null(shape.Duration);
        Assert.Equal(16, Assert.Single(shape.Periods).Limit);
        Assert.Throws<ValidationException>(() => ChargingStrategyConverter.ToProfileShape(fairShare, new List<ChargingStrategyPeriod> { new(60, 16) }, Casablanca, T0));
    }

    [Fact]
    public void Invalid_strategies_are_refused()
    {
        Assert.Throws<ValidationException>(() => ChargingStrategyConverter.Validate(OffPeak, new List<ChargingStrategyPeriod> { new(86_400, 16) })); // beyond 24 h
        Assert.Throws<ValidationException>(() => ChargingStrategyConverter.Validate(
            new ChargingStrategy { Name = "x", Purpose = ChargingProfilePurposeEnum.TxProfile, Kind = ChargingProfileKindEnum.Absolute },
            new List<ChargingStrategyPeriod> { new(0, 16) }));
    }

    [Fact]
    public void A_generated_profile_passes_the_profile_rules_of_both_versions()
    {
        var generated = ChargingStrategyConverter.ToProfileShape(OffPeak, SeededOffPeak(), Casablanca, T0);
        var profile = new ChargingProfile
        {
            OcppProfileId = 1, Purpose = ChargingProfilePurposeEnum.TxDefaultProfile, Kind = generated.Kind, RecurrencyKind = generated.RecurrencyKind,
            StartSchedule = generated.StartSchedule, Duration = generated.Duration
        };

        ChargingProfileMessages.Validate(profile, generated.Periods, OcppProtocols.Ocpp16);
        ChargingProfileMessages.Validate(profile, generated.Periods, OcppProtocols.Ocpp201);
    }
}
