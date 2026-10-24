using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    public sealed record PartnerProfileDto(int Id, string Name, string? Email, string? Phone, string? City, string? Address,
        string? LogoUrl, int StationCount, int ChargePointCount);

    public sealed record PartnerOverviewDto(int Stations, int ChargePoints, int Connectors, IReadOnlyDictionary<string, int> ConnectorsByStatus,
        int ActiveSessions, decimal RevenueToday, decimal RevenueMonth, decimal RevenueTotal, double EnergyMonth, int SessionsMonth, string Currency);

    public sealed record PartnerStationDto(int Id, string Name, string Address, string City, ChargingStationStatusEnum Status,
        int ChargePointCount, int ConnectorCount, string? Latitude, string? Longitude, ChargingStationNetworkEnum Access, string? PhotoUrl);

    /// <summary>A connector with its live state, prices and the session running on it (what the partner can stop).</summary>
    public sealed record PartnerConnectorDto(int Id, int EvseId, int? ConnectorId, ConnectorEnumType? Type, double PowerKw,
        ConnectorStatusEnumType? Status, decimal PricePerKWh, decimal PricePerMinute, decimal PricePerIdleMinute, decimal FlatFee,
        int? ActiveSessionId);

    /// <summary>A charge point of the partner; <see cref="Protocol"/> is the OCPP version of its live connection, null when offline.</summary>
    public sealed record PartnerChargePointDto(int Id, string ChargePointId, int StationId, string? StationName, string? Vendor,
        string? Model, ChargePointStatusEnum Status, IReadOnlyList<PartnerConnectorDto> Connectors, bool Online, string? Protocol);

    /// <summary>A session on one of the partner's stations, without the driver's personal data.</summary>
    public sealed record PartnerSessionDto(int Id, string? StationName, string? ChargePointName, string? Connector, DateTime StartDate,
        DateTime? EndDate, double ChargedMinutes, double ChargedKwhs, decimal Total, ReasonEnumType StoppedReason, bool Active);

    public sealed record PartnerStatisticPoint(string Period, int Sessions, double Energy, decimal Revenue);

    public sealed record PartnerStatisticsDto(IReadOnlyList<PartnerStatisticPoint> Daily, IReadOnlyList<PartnerStatisticPoint> Monthly, string Currency);

    /// <summary>Prices of one connector, VAT included (MAD). New sessions use them; started sessions keep theirs.</summary>
    public sealed class UpdateConnectorPricingRequest
    {
        [Range(0, 1000)] public decimal PricePerKWh { get; set; }
        [Range(0, 100)] public decimal PricePerMinute { get; set; }
        [Range(0, 100)] public decimal PricePerIdleMinute { get; set; }
        [Range(0, 1000)] public decimal FlatFee { get; set; }
    }

    /// <summary>
    /// Weekly opening hours of a station, stored as JSON on the station. Same shape as OCPI hours: open around the
    /// clock, or regular periods per weekday (1 = Monday ... 7 = Sunday, "HH:mm" local times, a period may end at 24:00).
    /// </summary>
    public sealed partial class StationOpeningHours : IValidatableObject
    {
        public const int MaxPeriods = 21;

        public bool TwentyFourSeven { get; set; } = true;
        public List<StationOpeningPeriod> Regular { get; set; } = new();

        public static StationOpeningHours AlwaysOpen => new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TwentyFourSeven)
                yield break;
            if (Regular.Count == 0)
                yield return new ValidationResult("Give at least one opening period, or open around the clock.", new[] { nameof(Regular) });
            if (Regular.Count > MaxPeriods)
                yield return new ValidationResult($"At most {MaxPeriods} opening periods.", new[] { nameof(Regular) });
            foreach (var period in Regular)
            {
                if (!Time().IsMatch(period.PeriodBegin ?? "") || !Time().IsMatch(period.PeriodEnd ?? "") || period.PeriodBegin == "24:00")
                    yield return new ValidationResult("Opening times are HH:mm (00:00 to 24:00).", new[] { nameof(Regular) });
                else if (string.CompareOrdinal(period.PeriodEnd, period.PeriodBegin) <= 0)
                    yield return new ValidationResult("An opening period must end after it starts.", new[] { nameof(Regular) });
            }
            foreach (var day in Regular.GroupBy(p => p.Weekday))
            {
                var periods = day.OrderBy(p => p.PeriodBegin, StringComparer.Ordinal).ToList();
                if (periods.Zip(periods.Skip(1)).Any(pair => string.CompareOrdinal(pair.Second.PeriodBegin, pair.First.PeriodEnd) < 0))
                    yield return new ValidationResult("Opening periods of a day must not overlap.", new[] { nameof(Regular) });
            }
        }

        [GeneratedRegex(@"^(([01]\d|2[0-3]):[0-5]\d|24:00)$")]
        private static partial Regex Time();
    }

    public sealed class StationOpeningPeriod
    {
        [Range(1, 7)] public int Weekday { get; set; }
        [Required] public string PeriodBegin { get; set; } = "";
        [Required] public string PeriodEnd { get; set; } = "";
    }

    public sealed record PartnerStationAmenities(bool Wifi, bool Parking, bool Restaurants, bool Washroom, bool SittingArea);

    public sealed record PartnerStationPhotoDto(int Id, string? Url);

    /// <summary>One of the partner's stations with what drivers see of it (the partner can edit it).</summary>
    public sealed record PartnerStationDetailDto(int Id, string Name, string Address, string City, double? Latitude, double? Longitude,
        ChargingStationStatusEnum Status, ChargingStationNetworkEnum Access, ParkingTypeEnum ParkingType, string? Description,
        StationOpeningHours OpeningHours, PartnerStationAmenities Amenities, IReadOnlyList<PartnerStationPhotoDto> Photos,
        int ChargePointCount, int ConnectorCount, DateTime UpdatedAt);

    /// <summary>The public information of a station a partner may change (not its status, charge points or owner).</summary>
    public sealed class UpdatePartnerStationRequest : IValidatableObject
    {
        [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = "";
        [Required, StringLength(200, MinimumLength = 3)] public string Address { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 2)] public string City { get; set; } = "";
        [Range(-90, 90)] public double? Latitude { get; set; }
        [Range(-180, 180)] public double? Longitude { get; set; }
        /// <summary>Public (anyone can charge) or Private (restricted access).</summary>
        [Required] public ChargingStationNetworkEnum? Access { get; set; }
        public ParkingTypeEnum? ParkingType { get; set; }
        [StringLength(1000)] public string? Description { get; set; }
        [Required] public StationOpeningHours? OpeningHours { get; set; }
        public PartnerStationAmenities? Amenities { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Latitude.HasValue != Longitude.HasValue)
                yield return new ValidationResult("Give both the latitude and the longitude, or neither.", new[] { nameof(Latitude), nameof(Longitude) });
        }
    }

    public sealed class PartnerSessionFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? StationId { get; set; }
        public int? ChargePointId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
