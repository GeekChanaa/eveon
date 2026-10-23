namespace VoltaXApi.Services
{
    /// <summary>A half-open UTC interval [Start, End) covering business days or months.</summary>
    public readonly record struct BusinessPeriod(DateTime Start, DateTime End)
    {
        public bool Contains(DateTime utc) => utc >= Start && utc < End;
    }

    /// <summary>Consecutive business days or months ending with the current one, oldest first.</summary>
    public static class BusinessPeriods
    {
        public static IReadOnlyList<BusinessPeriod> LastDays(IBusinessClock clock, int count) =>
            Enumerable.Range(0, count)
                .Select(i => clock.Today.AddDays(i - count + 1))
                .Select(day => new BusinessPeriod(clock.StartOfDayUtc(day), clock.StartOfDayUtc(day.AddDays(1))))
                .ToList();

        public static IReadOnlyList<BusinessPeriod> LastMonths(IBusinessClock clock, int count)
        {
            var current = new DateTime(clock.Today.Year, clock.Today.Month, 1);
            return Enumerable.Range(0, count)
                .Select(i => current.AddMonths(i - count + 1))
                .Select(month => new BusinessPeriod(clock.StartOfMonthUtc(month.Year, month.Month), clock.StartOfDayUtc(month.AddMonths(1))))
                .ToList();
        }

        /// <summary>Index of the period holding <paramref name="utc"/>, or -1.</summary>
        public static int IndexOf(IReadOnlyList<BusinessPeriod> periods, DateTime utc)
        {
            for (var i = 0; i < periods.Count; i++)
                if (periods[i].Contains(utc))
                    return i;
            return -1;
        }
    }
}
