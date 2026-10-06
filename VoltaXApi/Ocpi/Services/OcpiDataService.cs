using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;

namespace VoltaXApi.Ocpi.Services
{
    // Read side of the Sender interfaces (locations, tariffs, sessions, CDRs).
    public class OcpiDataService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly OcpiPartyService _parties;
        private readonly GlobalConfigurations _globalConfigurations;

        public OcpiDataService(VoltaXApiDbContext db, OcpiPartyService parties, GlobalConfigurations globalConfigurations)
        {
            _db = db;
            _parties = parties;
            _globalConfigurations = globalConfigurations;
        }

        public OcpiIdentity Identity => _parties.Identity(_globalConfigurations.Vat);

        // Soft deleted rows are read too: they are reported as REMOVED to incremental (date_from) syncs.
        private IQueryable<ChargingStation> Stations() => _db.ChargingStations.IgnoreQueryFilters().AsNoTracking()
            .Include(s => s.ChargePoints!).ThenInclude(cp => cp.Connectors);

        public async Task<Dictionary<int, ConnectorStatus>> StatusesAsync(IEnumerable<int>? connectorIds = null, CancellationToken cancellationToken = default)
        {
            var query = _db.ConnectorStatuses.AsNoTracking().Where(s => s.ConnectorID != null);
            if (connectorIds != null)
            {
                var ids = connectorIds.ToList();
                query = query.Where(s => ids.Contains(s.ConnectorID!.Value));
            }
            var rows = await query.ToListAsync(cancellationToken);
            return rows.GroupBy(s => s.ConnectorID!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.UpdatedAt).First());
        }

        public async Task<List<LocationDto>> LocationsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var stations = await Stations().OrderBy(s => s.ID).ToListAsync(cancellationToken);
            var statuses = await StatusesAsync(null, cancellationToken);
            var incremental = from.HasValue || to.HasValue;
            return stations
                .Where(s => incremental || OcpiMapper.IsPublished(s))
                .Select(s => OcpiMapper.ToLocation(Identity, s, statuses))
                .Where(l => l != null).Select(l => l!)
                .Select(l => incremental ? l : WithoutRemovedEvses(l))
                .Where(l => InRange(l.LastUpdated, from, to))
                .ToList();
        }

        public async Task<LocationDto?> LocationAsync(string locationId, CancellationToken cancellationToken = default)
        {
            if (!OcpiMapper.TryParseId(locationId, out var id)) return null;
            var station = await Stations().FirstOrDefaultAsync(s => s.ID == id, cancellationToken);
            if (station == null || station.IsDeleted && !station.ChargePoints!.Any()) return null;
            var connectorIds = station.ChargePoints!.SelectMany(cp => cp.Connectors ?? new List<Connector>()).Select(c => c.ID);
            return OcpiMapper.ToLocation(Identity, station, await StatusesAsync(connectorIds, cancellationToken));
        }

        public async Task<List<TariffDto>> TariffsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var connectors = await _db.Connectors.AsNoTracking()
                .Where(c => c.ChargePoint != null && c.ChargePoint.ShowOnMap != false
                    && c.ChargePoint.ChargingStation!.Network == ChargingStationNetworkEnum.Public
                    && c.ChargePoint.ChargingStation.Category != ChargingStationCategoryEnum.Private)
                .OrderBy(c => c.ID).ToListAsync(cancellationToken);
            return connectors.Select(c => OcpiMapper.ToTariff(Identity, c)).Where(t => InRange(t.LastUpdated, from, to)).ToList();
        }

        public async Task<TariffDto?> TariffAsync(string tariffId, CancellationToken cancellationToken = default)
        {
            if (!OcpiMapper.TryParseId(tariffId, out var id)) return null;
            var connector = await _db.Connectors.AsNoTracking().FirstOrDefaultAsync(c => c.ID == id, cancellationToken);
            return connector == null ? null : OcpiMapper.ToTariff(Identity, connector);
        }

        // Sessions are only visible to the party whose token started them.
        public IQueryable<OcpiSession> SessionsOf(OcpiParty party, DateTime? from, DateTime? to)
        {
            var query = _db.OcpiSessions.AsNoTracking().Where(s => s.OcpiPartyID == party.ID && s.Status != OcpiSessionStatus.Pending);
            if (from.HasValue) query = query.Where(s => s.LastUpdated >= from.Value);
            if (to.HasValue) query = query.Where(s => s.LastUpdated < to.Value);
            return query.OrderBy(s => s.LastUpdated).ThenBy(s => s.ID);
        }

        public async Task<List<SessionDto>> ToSessionsAsync(List<OcpiSession> sessions, CancellationToken cancellationToken = default)
        {
            var connectorIds = sessions.Where(s => s.ConnectorID.HasValue).Select(s => s.ConnectorID!.Value).Distinct().ToList();
            var connectors = await _db.Connectors.IgnoreQueryFilters().AsNoTracking()
                .Where(c => connectorIds.Contains(c.ID)).ToDictionaryAsync(c => c.ID, cancellationToken);
            return sessions.Select(s => OcpiMapper.ToSession(Identity, s,
                s.ConnectorID.HasValue && connectors.TryGetValue(s.ConnectorID.Value, out var c) ? c : null)).ToList();
        }

        public async Task<List<CdrDto>> ToCdrsAsync(List<OcpiSession> sessions, CancellationToken cancellationToken = default)
        {
            var connectorIds = sessions.Where(s => s.ConnectorID.HasValue).Select(s => s.ConnectorID!.Value).Distinct().ToList();
            var connectors = await _db.Connectors.IgnoreQueryFilters().AsNoTracking()
                .Include(c => c.ChargePoint!).ThenInclude(cp => cp.ChargingStation)
                .Where(c => connectorIds.Contains(c.ID)).ToDictionaryAsync(c => c.ID, cancellationToken);
            return sessions
                .Where(s => s.ConnectorID.HasValue && connectors.ContainsKey(s.ConnectorID.Value))
                .Select(s =>
                {
                    var connector = connectors[s.ConnectorID!.Value];
                    return OcpiMapper.ToCdr(Identity, s, connector.ChargePoint!.ChargingStation!, connector.ChargePoint, connector);
                }).ToList();
        }

        // Current OCPI view of one EVSE, used by the status push.
        public async Task<(string LocationId, EvseDto Evse)?> EvseAsync(int chargePointId, int evseId, CancellationToken cancellationToken = default)
        {
            var chargePoint = await _db.ChargePoints.IgnoreQueryFilters().AsNoTracking()
                .Include(cp => cp.ChargingStation).Include(cp => cp.Connectors)
                .FirstOrDefaultAsync(cp => cp.ID == chargePointId, cancellationToken);
            if (chargePoint?.ChargingStation == null) return null;
            var connectors = (chargePoint.Connectors ?? new List<Connector>()).Where(c => c.EvseID == evseId).ToList();
            if (connectors.Count == 0) return null;
            var statuses = await StatusesAsync(connectors.Select(c => c.ID), cancellationToken);
            return (OcpiMapper.LocationId(chargePoint.ChargingStationID),
                OcpiMapper.ToEvse(Identity, chargePoint.ChargingStation, chargePoint, evseId, connectors, statuses));
        }

        private static LocationDto WithoutRemovedEvses(LocationDto location)
        {
            location.Evses = location.Evses?.Where(e => e.Status != "REMOVED").ToList();
            return location;
        }

        private static bool InRange(DateTime value, DateTime? from, DateTime? to) =>
            (!from.HasValue || value >= OcpiJson.ToUtc(from.Value)) && (!to.HasValue || value < OcpiJson.ToUtc(to.Value));
    }
}
