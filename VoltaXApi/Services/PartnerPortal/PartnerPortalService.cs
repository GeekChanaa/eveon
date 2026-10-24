using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Services.PartnerPortal
{
    /// <summary>
    /// The partner portal: one partner's stations, charge points, prices, sessions and revenue. Every query is
    /// filtered on the partner id passed in, which callers take from the authenticated partner account.
    /// Revenue is the amount billed to drivers (VAT included) for sessions on the partner's stations.
    /// </summary>
    public sealed class PartnerPortalService
    {
        public const string Currency = "MAD";
        private const int MaxPageSize = 100;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly GlobalConfigurations _pricing;
        private readonly IAuditLogger _audit;
        private readonly IOcppCommandSender _chargers;

        public PartnerPortalService(VoltaXApiDbContext db, IBusinessClock clock, GlobalConfigurations pricing, IAuditLogger audit,
            IOcppCommandSender chargers)
        {
            _chargers = chargers;
            _db = db;
            _clock = clock;
            _pricing = pricing;
            _audit = audit;
        }

        public Task<PartnerProfileDto?> ProfileAsync(int partnerId, CancellationToken ct = default) =>
            _db.Partners.AsNoTracking().Where(p => p.ID == partnerId)
                .Select(p => new PartnerProfileDto(p.ID, p.Name, p.Email, p.Phone, p.City, p.Address, p.Image != null ? p.Image.Url : null,
                    p.ChargingStations!.Count(s => !s.IsDeleted),
                    p.ChargingStations!.Where(s => !s.IsDeleted).SelectMany(s => s.ChargePoints!).Count(cp => !cp.IsDeleted)))
                .FirstOrDefaultAsync(ct);

        public async Task<PartnerOverviewDto> OverviewAsync(int partnerId, CancellationToken ct = default)
        {
            var stations = await _db.ChargingStations.AsNoTracking().CountAsync(s => s.PartnerID == partnerId, ct);
            var connectors = await _db.Connectors.AsNoTracking()
                .Where(c => c.ChargePoint!.ChargingStation!.PartnerID == partnerId)
                .Select(c => new { c.ID, c.ChargePointID, Status = _db.ConnectorStatuses.Where(s => s.ConnectorID == c.ID).Select(s => (ConnectorStatusEnumType?)s.LastStatus).FirstOrDefault() })
                .ToListAsync(ct);
            var chargePoints = await _db.ChargePoints.AsNoTracking().CountAsync(cp => cp.ChargingStation!.PartnerID == partnerId, ct);
            var sessions = await PricedSessions(partnerId).ToListAsync(ct);
            var todayStart = _clock.StartOfDayUtc(_clock.Today);
            var monthStart = _clock.StartOfMonthUtc(_clock.Today.Year, _clock.Today.Month);
            var month = sessions.Where(s => s.StartDate >= monthStart).ToList();
            return new PartnerOverviewDto(stations, chargePoints, connectors.Count,
                connectors.GroupBy(c => (c.Status ?? ConnectorStatusEnumType.Unavailable).ToString()).ToDictionary(g => g.Key, g => g.Count()),
                sessions.Count(s => s.EndDate == null),
                sessions.Where(s => s.StartDate >= todayStart).Sum(Revenue), month.Sum(Revenue), sessions.Sum(Revenue),
                month.Sum(s => s.ChargedKwhs ?? 0), month.Count, Currency);
        }

        public Task<List<PartnerStationDto>> StationsAsync(int partnerId, CancellationToken ct = default) =>
            _db.ChargingStations.AsNoTracking().Where(s => s.PartnerID == partnerId).OrderBy(s => s.Name)
                .Select(s => new PartnerStationDto(s.ID, s.Name, s.Address, s.City, s.Status,
                    s.ChargePoints!.Count(cp => !cp.IsDeleted),
                    s.ChargePoints!.Where(cp => !cp.IsDeleted).SelectMany(cp => cp.Connectors!).Count(c => !c.IsDeleted),
                    s.Latitude, s.Longitude, s.Network,
                    s.ChargingStationImages!.Where(i => !i.IsDeleted).OrderBy(i => i.ID).Select(i => i.Image!.Url).FirstOrDefault()))
                .ToListAsync(ct);

        public async Task<List<PartnerChargePointDto>> ChargePointsAsync(int partnerId, int? stationId = null, CancellationToken ct = default)
        {
            var query = _db.ChargePoints.AsNoTracking().Where(cp => cp.ChargingStation!.PartnerID == partnerId);
            if (stationId.HasValue)
                query = query.Where(cp => cp.ChargingStationID == stationId.Value);
            var points = await query.OrderBy(cp => cp.ChargePointId)
                .Select(cp => new
                {
                    cp.ID, cp.ChargePointId, cp.ChargingStationID, StationName = cp.ChargingStation!.Name, cp.VendorName,
                    Model = cp.ChargePointModel != null ? cp.ChargePointModel.Name : null, cp.Status,
                    Connectors = cp.Connectors!.Where(c => !c.IsDeleted).OrderBy(c => c.EvseID).ThenBy(c => c.ConnectorID)
                        .Select(c => new PartnerConnectorDto(c.ID, c.EvseID, c.ConnectorID, c.ConnectorType, c.Power,
                            _db.ConnectorStatuses.Where(s => s.ConnectorID == c.ID).Select(s => (ConnectorStatusEnumType?)s.LastStatus).FirstOrDefault(),
                            c.PricePerKWh, c.PricePerMinute, c.PricePerIdleMinute, c.FlatFee,
                            _db.ChargingSessions.Where(s => s.ConnectorID == c.ID && s.EndDate == null && !s.IsDeleted)
                                .OrderByDescending(s => s.StartDate).Select(s => (int?)s.ID).FirstOrDefault())).ToList()
                })
                .ToListAsync(ct);
            return points.Select(p =>
            {
                var protocol = _chargers.GetProtocolVersion(p.ChargePointId);
                return new PartnerChargePointDto(p.ID, p.ChargePointId, p.ChargingStationID, p.StationName, p.VendorName, p.Model, p.Status, p.Connectors,
                    protocol != null, protocol);
            }).ToList();
        }

        public async Task<PartnerChargePointDto?> ChargePointAsync(int partnerId, int chargePointId, CancellationToken ct = default) =>
            (await ChargePointsAsync(partnerId, null, ct)).FirstOrDefault(p => p.Id == chargePointId);

        /// <summary>Changes the prices of one of the partner's connectors; false when it is not theirs. The change is audited.</summary>
        public async Task<PartnerConnectorDto?> UpdatePricingAsync(int partnerId, int connectorId, UpdateConnectorPricingRequest request, CancellationToken ct = default)
        {
            var connector = await _db.Connectors.FirstOrDefaultAsync(c => c.ID == connectorId && c.ChargePoint!.ChargingStation!.PartnerID == partnerId, ct);
            if (connector == null)
                return null;
            var before = new { connector.PricePerKWh, connector.PricePerMinute, connector.PricePerIdleMinute, connector.FlatFee };
            connector.PricePerKWh = Math.Round(request.PricePerKWh, 4);
            connector.PricePerMinute = Math.Round(request.PricePerMinute, 4);
            connector.PricePerIdleMinute = Math.Round(request.PricePerIdleMinute, 4);
            connector.FlatFee = Math.Round(request.FlatFee, 4);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("PartnerPortal.UpdateConnectorPricing", "Connector", connector.ID.ToString(CultureInfo.InvariantCulture),
                new { before, after = request });
            return (await ChargePointsAsync(partnerId, null, ct)).SelectMany(p => p.Connectors).First(c => c.Id == connectorId);
        }

        public async Task<PagedResult<PartnerSessionDto>> SessionsAsync(int partnerId, PartnerSessionFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
            var query = Filtered(PricedSessions(partnerId), filter);
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(s => s.StartDate).ThenByDescending(s => s.ID).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var labels = await Labels(rows.Select(r => r.ConnectorID).Distinct().ToList(), ct);
            return new PagedResult<PartnerSessionDto>(rows.Select(r => ToDto(r, labels)).ToList(), page, pageSize, total);
        }

        /// <summary>Sessions of the period as the rows of a statement (same shape as the customer export).</summary>
        public async Task<IReadOnlyList<MySessionDto>> StatementAsync(int partnerId, PartnerSessionFilter filter, CancellationToken ct = default)
        {
            var rows = await Filtered(PricedSessions(partnerId), filter).OrderBy(s => s.StartDate).ThenBy(s => s.ID).Take(20_000).ToListAsync(ct);
            var labels = await Labels(rows.Select(r => r.ConnectorID).Distinct().ToList(), ct);
            return rows.Select(r =>
            {
                var (station, point, connector) = labels.GetValueOrDefault(r.ConnectorID);
                var billed = r.TotalAmount.HasValue;
                var idle = r.IdleChargingPriceWithVAT((int)_pricing.GracePeriod);
                return new MySessionDto(r.ID, station, null, point, connector, r.StartDate, r.EndDate, r.ChargedMinutes ?? 0, r.IdleMinutes ?? 0,
                    r.ChargedKwhs ?? 0, billed ? r.EnergyAmount ?? 0 : 0, billed ? r.TimeAmount ?? 0 : r.ChargingPriceWithVAT, billed ? r.FlatFeeAmount ?? 0 : 0,
                    idle, Revenue(r), r.StoppedReason, r.ChargingSessionStatus, r.EndDate == null, false);
            }).ToList();
        }

        public async Task<PartnerStatisticsDto> StatisticsAsync(int partnerId, CancellationToken ct = default)
        {
            var from = _clock.StartOfMonthUtc(_clock.Today.AddMonths(-11).Year, _clock.Today.AddMonths(-11).Month);
            var sessions = await PricedSessions(partnerId).Where(s => s.StartDate >= from).ToListAsync(ct);
            var dayFrom = _clock.Today.AddDays(-29);
            var daily = Enumerable.Range(0, 30).Select(i => dayFrom.AddDays(i)).Select(day =>
            {
                var items = sessions.Where(s => _clock.ToLocal(s.StartDate).Date == day).ToList();
                return new PartnerStatisticPoint(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), items.Count, items.Sum(s => s.ChargedKwhs ?? 0), items.Sum(Revenue));
            }).ToList();
            var monthly = Enumerable.Range(0, 12).Select(i => _clock.Today.AddMonths(i - 11)).Select(month =>
            {
                var items = sessions.Where(s => { var local = _clock.ToLocal(s.StartDate); return local.Year == month.Year && local.Month == month.Month; }).ToList();
                return new PartnerStatisticPoint(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), items.Count, items.Sum(s => s.ChargedKwhs ?? 0), items.Sum(Revenue));
            }).ToList();
            return new PartnerStatisticsDto(daily, monthly, Currency);
        }

        // -------------------------------------------------------------------------------- internals

        /// <summary>Sessions on the partner's stations with the columns pricing needs (stations deleted since included).</summary>
        private IQueryable<ChargingSession> PricedSessions(int partnerId) =>
            _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                .Where(s => !s.IsDeleted && s.Connector!.ChargePoint!.ChargingStation!.PartnerID == partnerId)
                .Select(s => new ChargingSession
                {
                    ID = s.ID, ConnectorID = s.ConnectorID, StartDate = s.StartDate, EndDate = s.EndDate, ChargedMinutes = s.ChargedMinutes,
                    IdleMinutes = s.IdleMinutes, ChargedKwhs = s.ChargedKwhs, PricePerMinute = s.PricePerMinute, PricePerIdleMinute = s.PricePerIdleMinute,
                    EnergyAmount = s.EnergyAmount, TimeAmount = s.TimeAmount, FlatFeeAmount = s.FlatFeeAmount, IdleAmount = s.IdleAmount,
                    TotalAmount = s.TotalAmount, VatRate = s.VatRate, StoppedReason = s.StoppedReason, ChargingSessionStatus = s.ChargingSessionStatus
                });

        private IQueryable<ChargingSession> Filtered(IQueryable<ChargingSession> sessions, PartnerSessionFilter filter)
        {
            if (filter.From.HasValue)
            {
                var from = _clock.StartOfDayUtc(filter.From.Value.Date);
                sessions = sessions.Where(s => s.StartDate >= from);
            }
            if (filter.To.HasValue)
            {
                var to = _clock.StartOfDayUtc(filter.To.Value.Date.AddDays(1));
                sessions = sessions.Where(s => s.StartDate < to);
            }
            if (filter.ChargePointId.HasValue)
                sessions = sessions.Where(s => _db.Connectors.Any(c => c.ID == s.ConnectorID && c.ChargePointID == filter.ChargePointId));
            if (filter.StationId.HasValue)
                sessions = sessions.Where(s => _db.Connectors.Any(c => c.ID == s.ConnectorID && c.ChargePoint!.ChargingStationID == filter.StationId));
            if (string.Equals(filter.Status, "active", StringComparison.OrdinalIgnoreCase))
                sessions = sessions.Where(s => s.EndDate == null);
            else if (string.Equals(filter.Status, "completed", StringComparison.OrdinalIgnoreCase))
                sessions = sessions.Where(s => s.EndDate != null);
            return sessions;
        }

        private async Task<Dictionary<int, (string? Station, string? ChargePoint, string? Connector)>> Labels(List<int> connectorIds, CancellationToken ct)
        {
            var rows = await _db.Connectors.AsNoTracking().IgnoreQueryFilters().Where(c => connectorIds.Contains(c.ID))
                .Select(c => new { c.ID, Station = c.ChargePoint!.ChargingStation!.Name, Point = c.ChargePoint!.ChargePointId, c.EvseID, c.ConnectorID })
                .ToListAsync(ct);
            return rows.ToDictionary(r => r.ID, r => ((string?)r.Station, (string?)r.Point,
                (string?)(r.ConnectorID is null or 1 ? $"EVSE {r.EvseID}" : $"EVSE {r.EvseID} / {r.ConnectorID}")));
        }

        private PartnerSessionDto ToDto(ChargingSession s, Dictionary<int, (string? Station, string? ChargePoint, string? Connector)> labels)
        {
            var (station, point, connector) = labels.GetValueOrDefault(s.ConnectorID);
            return new PartnerSessionDto(s.ID, station, point, connector, s.StartDate, s.EndDate, s.ChargedMinutes ?? 0, s.ChargedKwhs ?? 0,
                Revenue(s), s.StoppedReason, s.EndDate == null);
        }

        /// <summary>What the driver was billed for the session (0 while it is running).</summary>
        private decimal Revenue(ChargingSession s) => s.EndDate == null ? 0 : s.TotalPriceWithVAT(_pricing.Vat, (int)_pricing.GracePeriod);
    }
}
