using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.SmartCharging
{
    /// <summary>Persists the smart charging notifications of 2.0.1 chargers (charging limits, EV charging needs and schedules).</summary>
    public class SmartChargingInboundStore
    {
        private readonly VoltaXApiDbContext _db;
        private readonly ILogger<SmartChargingInboundStore> _logger;

        public SmartChargingInboundStore(VoltaXApiDbContext db, ILogger<SmartChargingInboundStore> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>NotifyChargingLimit: the external limit replaces the previous one of the same source on the EVSE.</summary>
        public async Task SaveChargingLimitAsync(string chargePointId, NotifyChargingLimitRequest request, CancellationToken cancellationToken = default)
        {
            var chargePointDbId = await ChargePointDbId(chargePointId, cancellationToken);
            if (chargePointDbId == null) return;
            var source = request.ChargingLimit?.ChargingLimitSource.ToString() ?? "Other";

            await ClearExternal(chargePointDbId.Value, source, request.EvseId, "Superseded by a new limit", cancellationToken);
            foreach (var schedule in request.ChargingSchedule ?? new List<ChargingScheduleType>())
            {
                var periods = (schedule.ChargingSchedulePeriod ?? new List<ChargingSchedulePeriodType>())
                    .Select(p => new ChargingProfilePeriod(p.StartPeriod, p.Limit, p.NumberPhases, p.PhaseToUse))
                    .OrderBy(p => p.StartPeriod)
                    .ToList();
                _db.ChargingProfiles.Add(new ChargingProfile
                {
                    ChargePointID = chargePointDbId.Value,
                    EvseId = request.EvseId,
                    OcppProfileId = schedule.Id,
                    Purpose = ChargingProfilePurposeEnum.ChargingStationExternalConstraints,
                    Kind = schedule.StartSchedule == null ? ChargingProfileKindEnum.Relative : ChargingProfileKindEnum.Absolute,
                    ChargingRateUnit = schedule.ChargingRateUnit == ChargingRateUnitEnumType.W ? ChargingRateUnitEnum.W : ChargingRateUnitEnum.A,
                    StartSchedule = schedule.StartSchedule == null ? null : DateTime.SpecifyKind(schedule.StartSchedule.Value, DateTimeKind.Utc),
                    Duration = schedule.Duration,
                    MinChargingRate = schedule.MinChargingRate,
                    PeriodsJson = SmartChargingJson.Serialize(periods),
                    Source = ChargingProfileSourceEnum.ChargerReported,
                    Status = ChargingProfileStatusEnum.Accepted,
                    ChargingLimitSource = source,
                    LastError = request.ChargingLimit?.IsGridCritical == true ? "Grid critical" : null
                });
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>ClearedChargingLimit: the external limits of that source (on the EVSE when given) are gone.</summary>
        public async Task ClearChargingLimitAsync(string chargePointId, string source, int? evseId, CancellationToken cancellationToken = default)
        {
            var chargePointDbId = await ChargePointDbId(chargePointId, cancellationToken);
            if (chargePointDbId == null) return;
            await ClearExternal(chargePointDbId.Value, source, evseId, null, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Stores NotifyEVChargingNeeds; returns the charging station of the charger (null when unknown).</summary>
        public async Task<int?> SaveEvChargingNeedsAsync(string chargePointId, NotifyEVChargingNeedsRequest request, CancellationToken cancellationToken = default)
        {
            var chargePoint = await _db.ChargePoints.AsNoTracking()
                .Where(c => c.ChargePointId == chargePointId)
                .Select(c => new { c.ID, c.ChargingStationID })
                .FirstOrDefaultAsync(cancellationToken);
            if (chargePoint == null) return null;

            var needs = request.ChargingNeeds;
            var ac = needs?.ACChargingParameters;
            var dc = needs?.DCChargingParameters;
            _db.EvChargingNeeds.Add(new EvChargingNeeds
            {
                ChargePointID = chargePoint.ID,
                EvseId = request.EvseId,
                RequestedEnergyTransfer = needs?.RequestedEnergyTransfer.ToString(),
                DepartureTime = needs?.DepartureTime == null ? null : DateTime.SpecifyKind(needs.DepartureTime.Value, DateTimeKind.Utc),
                MaxScheduleTuples = request.MaxScheduleTuples,
                EnergyAmount = ac?.EnergyAmount ?? dc?.EnergyAmount,
                EvMinCurrent = ac?.EvMinCurrent,
                EvMaxCurrent = ac?.EvMaxCurrent ?? dc?.EvMaxCurrent,
                EvMaxVoltage = ac?.EvMaxVoltage ?? dc?.EvMaxVoltage,
                EvMaxPower = dc?.EvMaxPower,
                StateOfCharge = dc?.StateOfCharge,
                EvEnergyCapacity = dc?.EvEnergyCapacity,
                FullSoC = dc?.FullSoC,
                BulkSoC = dc?.BulkSoC,
                ReceivedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return chargePoint.ChargingStationID;
        }

        /// <summary>NotifyEVChargingSchedule: kept with the EVSE's latest charging needs (within a day), else on its own row.</summary>
        public async Task SaveEvChargingScheduleAsync(string chargePointId, NotifyEVChargingScheduleRequest request, CancellationToken cancellationToken = default)
        {
            var chargePointDbId = await ChargePointDbId(chargePointId, cancellationToken);
            if (chargePointDbId == null) return;

            var since = DateTime.UtcNow.AddDays(-1);
            var needs = await _db.EvChargingNeeds
                .Where(n => n.ChargePointID == chargePointDbId && n.EvseId == request.EvseId && n.ReceivedAt >= since)
                .OrderByDescending(n => n.ReceivedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (needs == null)
            {
                needs = new EvChargingNeeds { ChargePointID = chargePointDbId.Value, EvseId = request.EvseId, ReceivedAt = DateTime.UtcNow };
                _db.EvChargingNeeds.Add(needs);
            }

            needs.EvScheduleTimeBase = DateTimeOffset.TryParse(request.TimeBase, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timeBase)
                ? timeBase.UtcDateTime
                : null;
            var schedule = request.ChargingSchedule;
            needs.EvScheduleJson = schedule == null
                ? null
                : System.Text.Json.JsonSerializer.Serialize(new
                {
                    chargingRateUnit = schedule.ChargingRateUnit.ToString(),
                    duration = schedule.Duration,
                    periods = (schedule.ChargingSchedulePeriod ?? new List<ChargingSchedulePeriodType>())
                        .Select(p => new ChargingProfilePeriod(p.StartPeriod, p.Limit, p.NumberPhases, p.PhaseToUse))
                }, SmartChargingJson.Options);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task ClearExternal(int chargePointDbId, string source, int? evseId, string? note, CancellationToken cancellationToken)
        {
            var query = _db.ChargingProfiles.Where(p => p.ChargePointID == chargePointDbId
                && p.Purpose == ChargingProfilePurposeEnum.ChargingStationExternalConstraints
                && p.ChargingLimitSource == source
                && p.Status != ChargingProfileStatusEnum.Cleared);
            if (evseId != null) query = query.Where(p => p.EvseId == evseId);
            var cleared = await query.ToListAsync(cancellationToken);
            foreach (var profile in cleared)
            {
                profile.Status = ChargingProfileStatusEnum.Cleared;
                profile.LastError = note;
            }
            if (cleared.Count > 0)
                _logger.LogInformation("Charge point {ChargePointID}: {Count} {Source} charging limit(s) cleared", chargePointDbId, cleared.Count, source);
        }

        private async Task<int?> ChargePointDbId(string chargePointId, CancellationToken cancellationToken)
        {
            var id = await _db.ChargePoints.AsNoTracking().Where(c => c.ChargePointId == chargePointId).Select(c => (int?)c.ID).FirstOrDefaultAsync(cancellationToken);
            if (id == null)
                _logger.LogWarning("Smart charging notification from unknown charge point {ChargePointId} not stored", chargePointId);
            return id;
        }
    }
}
