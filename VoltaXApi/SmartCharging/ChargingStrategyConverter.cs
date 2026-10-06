using VoltaXApi.Exceptions;
using VoltaXApi.Models;

namespace VoltaXApi.SmartCharging
{
    /// <summary>Schedule of a charging profile generated from a strategy.</summary>
    public sealed record StrategyProfileShape(
        ChargingProfileKindEnum Kind,
        ChargingProfileRecurrencyEnum? RecurrencyKind,
        DateTime StartSchedule,
        int? Duration,
        List<ChargingProfilePeriod> Periods);

    /// <summary>
    /// Turns a <see cref="ChargingStrategy"/> (business wall-clock times) into a charger schedule. Pure.
    /// Recurring profiles start at the business-local midnight (Daily) or Monday 00:00 (Weekly) of the current
    /// day / week, converted to UTC; period offsets are wall-clock seconds from that start. A charger repeats a
    /// recurring profile every 24 h (168 h) of absolute time, so in a zone with DST the schedule shifts by the DST
    /// offset after a change until the strategy is applied again.
    /// </summary>
    public static class ChargingStrategyConverter
    {
        public const int SecondsPerDay = 86_400;
        public const int SecondsPerWeek = 7 * SecondsPerDay;

        public static void Validate(ChargingStrategy strategy, IReadOnlyList<ChargingStrategyPeriod> periods)
        {
            static void Fail(string message) => throw new ValidationException(message);

            if (string.IsNullOrWhiteSpace(strategy.Name)) Fail("The strategy needs a name.");
            if (strategy.Name.Length > 100) Fail("The strategy name is at most 100 characters.");
            if (strategy.Description?.Length > 500) Fail("The description is at most 500 characters.");
            if (strategy.Purpose is not (ChargingProfilePurposeEnum.TxDefaultProfile or ChargingProfilePurposeEnum.ChargingStationMaxProfile))
                Fail("A strategy is a TxDefaultProfile or a ChargingStationMaxProfile.");
            if (strategy.Kind == ChargingProfileKindEnum.Relative) Fail("A strategy is Absolute or Recurring.");
            if (strategy.Kind == ChargingProfileKindEnum.Recurring && strategy.RecurrencyKind == null) Fail("A recurring strategy needs Daily or Weekly recurrence.");
            if (strategy.Kind == ChargingProfileKindEnum.Absolute && strategy.RecurrencyKind != null) Fail("Only a recurring strategy has a recurrence.");
            if (strategy.StackLevel < 0) Fail("The stack level cannot be negative.");
            if (periods.Count == 0) Fail("A strategy needs at least one period.");
            if (periods.Count > ChargingProfileMessages.MaxPeriods) Fail($"A strategy has at most {ChargingProfileMessages.MaxPeriods} periods.");
            if (periods.Select(p => p.StartSeconds).Distinct().Count() != periods.Count) Fail("Two periods start at the same time.");

            var cycle = Cycle(strategy);
            foreach (var period in periods)
            {
                if (period.StartSeconds < 0) Fail("A period cannot start before the schedule.");
                if (cycle != null && period.StartSeconds >= cycle) Fail("A period starts after the end of the recurrence cycle.");
                if (double.IsNaN(period.Limit) || double.IsInfinity(period.Limit) || period.Limit < 0) Fail("Limits must be zero or positive.");
                if (period.NumberPhases is < 1 or > 3) Fail("The number of phases is 1, 2 or 3.");
            }
            if (strategy.Kind == ChargingProfileKindEnum.Absolute && periods.Min(p => p.StartSeconds) != 0)
                Fail("An absolute strategy starts when it is applied: its first period starts at 0.");
        }

        public static StrategyProfileShape ToProfileShape(ChargingStrategy strategy, IReadOnlyList<ChargingStrategyPeriod> strategyPeriods,
            TimeZoneInfo zone, DateTime utcNow)
        {
            Validate(strategy, strategyPeriods);
            utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            var sorted = strategyPeriods.OrderBy(p => p.StartSeconds).ToList();

            if (strategy.Kind == ChargingProfileKindEnum.Absolute)
            {
                var start = new DateTime(utcNow.Ticks - utcNow.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                return new StrategyProfileShape(ChargingProfileKindEnum.Absolute, null, start, null,
                    sorted.Select(p => new ChargingProfilePeriod(p.StartSeconds, p.Limit, p.NumberPhases)).ToList());
            }

            var localToday = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone).Date;
            var weekly = strategy.RecurrencyKind == ChargingProfileRecurrencyEnum.Weekly;
            var localStart = weekly ? localToday.AddDays(-(((int)localToday.DayOfWeek + 6) % 7)) : localToday;

            // The period covering the cycle start is the last one of the previous cycle (e.g. 22:00-06:00).
            if (sorted[0].StartSeconds != 0)
            {
                var last = sorted[^1];
                sorted.Insert(0, new ChargingStrategyPeriod(0, last.Limit, last.NumberPhases));
            }

            return new StrategyProfileShape(ChargingProfileKindEnum.Recurring,
                weekly ? ChargingProfileRecurrencyEnum.Weekly : ChargingProfileRecurrencyEnum.Daily,
                LocalToUtc(localStart, zone),
                weekly ? SecondsPerWeek : SecondsPerDay,
                sorted.Select(p => new ChargingProfilePeriod(p.StartSeconds, p.Limit, p.NumberPhases)).ToList());
        }

        /// <summary>UTC instant of a business wall-clock time; a time inside a DST gap moves to the first valid minute.</summary>
        public static DateTime LocalToUtc(DateTime local, TimeZoneInfo zone)
        {
            local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            while (zone.IsInvalidTime(local))
                local = local.AddMinutes(30);
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, zone), DateTimeKind.Utc);
        }

        private static int? Cycle(ChargingStrategy strategy) => strategy.Kind != ChargingProfileKindEnum.Recurring
            ? null
            : strategy.RecurrencyKind == ChargingProfileRecurrencyEnum.Weekly ? SecondsPerWeek : SecondsPerDay;
    }
}
