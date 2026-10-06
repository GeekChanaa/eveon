namespace VoltaXApi.Services
{
    /// <summary>
    /// Current time and calendar boundaries. Instants are UTC; "business" dates are calendar dates in the
    /// operator's time zone (config "Business:TimeZone", default Africa/Casablanca), used for statistics
    /// such as "today" or "last 30 days".
    /// </summary>
    public interface IBusinessClock
    {
        TimeZoneInfo TimeZone { get; }
        DateTime UtcNow { get; }
        /// <summary>Current wall-clock time in the business time zone.</summary>
        DateTime LocalNow { get; }
        /// <summary>Current business date (time 00:00, Kind Unspecified).</summary>
        DateTime Today { get; }
        /// <summary>Converts a stored UTC instant (Kind Utc or Unspecified) to business wall-clock time.</summary>
        DateTime ToLocal(DateTime utc);
        /// <summary>UTC instant at which the given business date starts.</summary>
        DateTime StartOfDayUtc(DateTime localDate);
        /// <summary>UTC instant at which the given business month starts.</summary>
        DateTime StartOfMonthUtc(int year, int month);
    }

    public class BusinessClock : IBusinessClock
    {
        public const string ConfigKey = "Business:TimeZone";
        public const string DefaultTimeZoneId = "Africa/Casablanca";

        private readonly TimeProvider _timeProvider;

        public BusinessClock(TimeProvider timeProvider, IConfiguration configuration, ILogger<BusinessClock> logger)
            : this(timeProvider, ResolveTimeZone(configuration[ConfigKey], logger))
        {
        }

        public BusinessClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
        {
            _timeProvider = timeProvider;
            TimeZone = timeZone;
        }

        public TimeZoneInfo TimeZone { get; }

        public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public DateTime LocalNow => ToLocal(UtcNow);

        public DateTime Today => LocalNow.Date;

        public DateTime ToLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);

        public DateTime StartOfDayUtc(DateTime localDate)
        {
            var local = DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified);
            // A day starting inside a DST gap starts at the first valid minute.
            while (TimeZone.IsInvalidTime(local))
                local = local.AddMinutes(30);
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, TimeZone), DateTimeKind.Utc);
        }

        public DateTime StartOfMonthUtc(int year, int month) => StartOfDayUtc(new DateTime(year, month, 1));

        public static TimeZoneInfo ResolveTimeZone(string? id, ILogger? logger = null)
        {
            id = string.IsNullOrWhiteSpace(id) ? DefaultTimeZoneId : id.Trim();
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId) && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone))
                return zone;
            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId) && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out zone))
                return zone;

            logger?.LogError("Business time zone {TimeZone} is unknown on this host; statistics use UTC days", id);
            return TimeZoneInfo.Utc;
        }
    }
}
