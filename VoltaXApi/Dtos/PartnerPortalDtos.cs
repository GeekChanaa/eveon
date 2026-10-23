using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    public sealed record PartnerProfileDto(int Id, string Name, string? Email, string? Phone, string? City, string? Address,
        string? LogoUrl, int StationCount, int ChargePointCount);

    public sealed record PartnerOverviewDto(int Stations, int ChargePoints, int Connectors, IReadOnlyDictionary<string, int> ConnectorsByStatus,
        int ActiveSessions, decimal RevenueToday, decimal RevenueMonth, decimal RevenueTotal, double EnergyMonth, int SessionsMonth, string Currency);

    public sealed record PartnerStationDto(int Id, string Name, string Address, string City, ChargingStationStatusEnum Status,
        int ChargePointCount, int ConnectorCount, string? Latitude, string? Longitude);

    public sealed record PartnerConnectorDto(int Id, int EvseId, int? ConnectorId, ConnectorEnumType? Type, double PowerKw,
        ConnectorStatusEnumType? Status, decimal PricePerKWh, decimal PricePerMinute, decimal PricePerIdleMinute, decimal FlatFee);

    public sealed record PartnerChargePointDto(int Id, string ChargePointId, int StationId, string? StationName, string? Vendor,
        string? Model, ChargePointStatusEnum Status, IReadOnlyList<PartnerConnectorDto> Connectors);

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
