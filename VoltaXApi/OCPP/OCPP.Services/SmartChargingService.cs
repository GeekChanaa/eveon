using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;
using VoltaXApi.SmartCharging;
using V16 = VoltaXApi.OCPP.Ocpp16.SmartCharging;

namespace VoltaXApi.OCPP.Services
{
    public class SmartChargingService : ISmartChargingService
    {
        private const string SetChargingProfileAction = "SetChargingProfile";
        private const string ClearChargingProfileAction = "ClearChargingProfile";

        private static readonly ChargingProfileStatusEnum[] ActiveStatuses = { ChargingProfileStatusEnum.Pending, ChargingProfileStatusEnum.Accepted };

        private readonly IOcppCommandSender _commandSender;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly ChargingProfileReportTracker _reportTracker;
        private readonly ILogger<SmartChargingService> _logger;

        public SmartChargingService(
            IOcppCommandSender commandSender,
            VoltaXApiDbContext db,
            IBusinessClock clock,
            ChargingProfileReportTracker reportTracker,
            ILogger<SmartChargingService> logger)
        {
            _commandSender = commandSender;
            _db = db;
            _clock = clock;
            _reportTracker = reportTracker;
            _logger = logger;
        }

        public async Task<ChargingProfileCommandResult> SendChargingProfileAsync(string chargePointId, ChargingProfileInputDto input,
            ChargingProfileSourceEnum source, int? userId, int? chargingStrategyId = null, CancellationToken cancellationToken = default)
        {
            var chargePoint = await FindChargePoint(chargePointId, cancellationToken);
            var protocol = Protocol(chargePointId);
            var periods = (input.Periods ?? new List<ChargingProfilePeriod>()).OrderBy(p => p.StartPeriod).ToList();

            ChargingProfile profile;
            if (input.ChargingProfileID is { } existingId)
            {
                profile = await _db.ChargingProfiles.SingleOrDefaultAsync(p => p.ID == existingId && p.ChargePointID == chargePoint.ID, cancellationToken)
                    ?? throw new NotFoundException($"Charging profile {existingId} does not exist on {chargePointId}.");
            }
            else
            {
                profile = new ChargingProfile { ChargePointID = chargePoint.ID, CreatedByUserID = userId };
            }

            profile.EvseId = input.EvseId;
            profile.StackLevel = input.StackLevel;
            profile.Purpose = input.Purpose;
            profile.Kind = input.Kind;
            profile.RecurrencyKind = input.Kind == ChargingProfileKindEnum.Recurring ? input.RecurrencyKind : null;
            profile.ValidFrom = AsUtc(input.ValidFrom);
            profile.ValidTo = AsUtc(input.ValidTo);
            profile.TransactionId = string.IsNullOrWhiteSpace(input.TransactionId) ? null : input.TransactionId.Trim();
            profile.ChargingRateUnit = input.ChargingRateUnit;
            profile.StartSchedule = AsUtc(input.StartSchedule) ?? DefaultStartSchedule(input.Kind);
            profile.Duration = input.Duration;
            profile.MinChargingRate = input.MinChargingRate;
            profile.PeriodsJson = SmartChargingJson.Serialize(periods);
            profile.Source = source;
            profile.ChargingStrategyID = chargingStrategyId ?? (source == ChargingProfileSourceEnum.Strategy ? profile.ChargingStrategyID : null);

            ValidateOrRevert(profile, periods, protocol);
            return await Send(chargePointId, protocol, profile, periods, cancellationToken);
        }

        public async Task<ChargingProfileCommandResult> SetChargingProfileAsync(string chargePointId, SetChargingProfileRequest request, int? userId,
            CancellationToken cancellationToken = default)
        {
            if (request?.ChargingProfile == null)
                throw new ValidationException("chargingProfile is required.");
            var chargePoint = await FindChargePoint(chargePointId, cancellationToken);
            var protocol = Protocol(chargePointId);
            if (request.ChargingProfile.ChargingSchedule is not { Count: > 0 })
                throw new ValidationException("chargingProfile.chargingSchedule needs one schedule.");
            if (request.ChargingProfile.ChargingSchedule.Count > 1)
                throw new ValidationException("Only single-schedule profiles can be stored and sent.");

            var (reported, periods) = ChargingProfileMessages.FromOcpp201(request.ChargingProfile, request.EvseId);
            // The caller chose the OCPP id: a stored profile with that id on this charger is the one being replaced.
            var profile = await _db.ChargingProfiles
                .Where(p => p.ChargePointID == chargePoint.ID && p.OcppProfileId == reported.OcppProfileId && ActiveStatuses.Contains(p.Status))
                .OrderByDescending(p => p.ID)
                .FirstOrDefaultAsync(cancellationToken)
                ?? new ChargingProfile { ChargePointID = chargePoint.ID, CreatedByUserID = userId };

            profile.OcppProfileId = reported.OcppProfileId;
            profile.EvseId = reported.EvseId;
            profile.StackLevel = reported.StackLevel;
            profile.Purpose = reported.Purpose;
            profile.Kind = reported.Kind;
            profile.RecurrencyKind = reported.RecurrencyKind;
            profile.ValidFrom = reported.ValidFrom;
            profile.ValidTo = reported.ValidTo;
            profile.TransactionId = reported.TransactionId;
            profile.ChargingRateUnit = reported.ChargingRateUnit;
            profile.StartSchedule = reported.StartSchedule ?? DefaultStartSchedule(reported.Kind);
            profile.Duration = reported.Duration;
            profile.MinChargingRate = reported.MinChargingRate;
            profile.PeriodsJson = reported.PeriodsJson;
            profile.Source = ChargingProfileSourceEnum.Csms;

            ValidateOrRevert(profile, periods, protocol);
            return await Send(chargePointId, protocol, profile, periods, cancellationToken);
        }

        /// <summary>An invalid edit must not stay tracked: a later SaveChanges of the same scope (load balancer) would store it.</summary>
        private void ValidateOrRevert(ChargingProfile profile, IReadOnlyList<ChargingProfilePeriod> periods, string protocol)
        {
            try
            {
                ChargingProfileMessages.Validate(profile, periods, protocol);
            }
            catch (ValidationException)
            {
                if (profile.ID != 0)
                {
                    var entry = _db.Entry(profile);
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                }
                throw;
            }
        }

        private async Task<ChargingProfileCommandResult> Send(string chargePointId, string protocol, ChargingProfile profile,
            IReadOnlyList<ChargingProfilePeriod> periods, CancellationToken cancellationToken)
        {
            profile.Status = ChargingProfileStatusEnum.Pending;
            profile.LastError = null;
            profile.LastSentAt = DateTime.UtcNow;
            if (profile.ID == 0)
            {
                _db.ChargingProfiles.Add(profile);
                await _db.SaveChangesAsync(cancellationToken);
                if (profile.OcppProfileId == 0)
                    profile.OcppProfileId = profile.ID;
            }
            await _db.SaveChangesAsync(cancellationToken);

            string status;
            string? reason;
            try
            {
                if (protocol == OcppProtocols.Ocpp16)
                {
                    var response = await _commandSender.SendRequestAsync<V16.SetChargingProfileRequest, V16.SetChargingProfileResponse>(
                        chargePointId, SetChargingProfileAction, ChargingProfileMessages.ToOcpp16(profile, periods), cancellationToken: cancellationToken);
                    status = response.Status.ToString();
                    reason = response.Status == V16.ChargingProfileStatus.NotSupported ? "NotSupported" : null;
                }
                else
                {
                    var response = await _commandSender.SendRequestAsync<SetChargingProfileRequest, SetChargingProfileResponse>(
                        chargePointId, SetChargingProfileAction, ChargingProfileMessages.ToOcpp201(profile, periods), cancellationToken: cancellationToken);
                    status = response.Status.ToString();
                    reason = Reason(response.StatusInfo);
                }
            }
            catch (Exception ex) when (ex is TimeoutException or WebSocketNotFoundException or OcppCallErrorException)
            {
                await RecordFailure(profile, ex);
                throw;
            }

            var accepted = status == nameof(ChargingProfileStatusEnum.Accepted);
            profile.Status = accepted ? ChargingProfileStatusEnum.Accepted : ChargingProfileStatusEnum.Rejected;
            profile.LastError = accepted ? null : Truncate(reason ?? status, 512);
            if (accepted)
                await MarkReplaced(profile, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("SetChargingProfile {OcppProfileId} ({Purpose}, EVSE {EvseId}) answered {Status} by {ChargePointId} ({Protocol})",
                profile.OcppProfileId, profile.Purpose, profile.EvseId, status, chargePointId, protocol);
            return new ChargingProfileCommandResult(status, reason, ChargingProfileDto.From(profile));
        }

        /// <summary>The charger replaces a profile with the same id, and (1.6) one with the same EVSE, purpose and stack level.</summary>
        private async Task MarkReplaced(ChargingProfile accepted, CancellationToken cancellationToken)
        {
            var replaced = await _db.ChargingProfiles
                .Where(p => p.ChargePointID == accepted.ChargePointID && p.ID != accepted.ID && ActiveStatuses.Contains(p.Status)
                    && p.Purpose != ChargingProfilePurposeEnum.ChargingStationExternalConstraints
                    && (p.OcppProfileId == accepted.OcppProfileId
                        || p.EvseId == accepted.EvseId && p.Purpose == accepted.Purpose && p.StackLevel == accepted.StackLevel
                           && p.TransactionId == accepted.TransactionId))
                .ToListAsync(cancellationToken);
            foreach (var profile in replaced)
            {
                profile.Status = ChargingProfileStatusEnum.Cleared;
                profile.LastError = $"Replaced by profile {accepted.OcppProfileId}";
            }
        }

        private async Task RecordFailure(ChargingProfile profile, Exception ex)
        {
            if (ex is OcppCallErrorException callError)
            {
                profile.Status = ChargingProfileStatusEnum.Rejected;
                profile.LastError = Truncate($"CallError {callError.ErrorCode}: {callError.ErrorDescription}", 512);
            }
            else
            {
                profile.LastError = ex is TimeoutException ? "Timeout" : "NotConnected";
            }
            // The send already failed; the outcome is recorded even if the request was cancelled meanwhile.
            await _db.SaveChangesAsync(CancellationToken.None);
            _logger.LogWarning(ex, "SetChargingProfile {OcppProfileId} to charge point {ChargePointID} failed: {Error}",
                profile.OcppProfileId, profile.ChargePointID, profile.LastError);
        }

        public async Task<ClearChargingProfileResult> ClearStoredChargingProfileAsync(string chargePointId, int chargingProfileId,
            CancellationToken cancellationToken = default)
        {
            var chargePoint = await FindChargePoint(chargePointId, cancellationToken);
            var profile = await _db.ChargingProfiles.SingleOrDefaultAsync(p => p.ID == chargingProfileId && p.ChargePointID == chargePoint.ID, cancellationToken)
                ?? throw new NotFoundException($"Charging profile {chargingProfileId} does not exist on {chargePointId}.");
            if (profile.Purpose == ChargingProfilePurposeEnum.ChargingStationExternalConstraints)
                throw new ValidationException("External constraints are cleared by the system that set them.");

            return await ClearChargingProfileAsync(chargePointId, new ClearChargingProfileRequest { ChargingProfileId = profile.OcppProfileId }, cancellationToken);
        }

        public async Task<ClearChargingProfileResult> ClearChargingProfileAsync(string chargePointId, ClearChargingProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var chargePoint = await FindChargePoint(chargePointId, cancellationToken);
            var protocol = Protocol(chargePointId);
            var criteria = request.ChargingProfileCriteria;
            if (criteria?.ChargingProfilePurpose == ChargingProfilePurposeEnumType.ChargingStationExternalConstraints)
                throw new ValidationException("External constraints are cleared by the system that set them.");

            string status;
            if (protocol == OcppProtocols.Ocpp16)
            {
                var request16 = new V16.ClearChargingProfileRequest
                {
                    Id = request.ChargingProfileId,
                    ConnectorId = criteria?.EvseId,
                    ChargingProfilePurpose = criteria?.ChargingProfilePurpose == null
                        ? null
                        : ChargingProfileMessages.ToOcpp16(ChargingProfileMessages.FromOcpp201(criteria.ChargingProfilePurpose.Value)),
                    StackLevel = criteria?.StackLevel
                };
                var response = await _commandSender.SendRequestAsync<V16.ClearChargingProfileRequest, V16.ClearChargingProfileResponse>(
                    chargePointId, ClearChargingProfileAction, request16, cancellationToken: cancellationToken);
                status = response.Status.ToString();
            }
            else
            {
                var response = await _commandSender.SendRequestAsync<ClearChargingProfileRequest, ClearChargingProfileResponse>(
                    chargePointId, ClearChargingProfileAction, request, cancellationToken: cancellationToken);
                status = response.Status.ToString();
            }

            // Accepted: the matching profiles are gone. Unknown: the charger has none of them, so none is active either.
            var query = _db.ChargingProfiles.Where(p => p.ChargePointID == chargePoint.ID && ActiveStatuses.Contains(p.Status)
                && p.Purpose != ChargingProfilePurposeEnum.ChargingStationExternalConstraints);
            if (request.ChargingProfileId is { } ocppId)
                query = query.Where(p => p.OcppProfileId == ocppId);
            else
            {
                if (criteria?.EvseId is { } evseId) query = query.Where(p => p.EvseId == evseId);
                if (criteria?.ChargingProfilePurpose is { } purpose)
                {
                    var stored = ChargingProfileMessages.FromOcpp201(purpose);
                    query = query.Where(p => p.Purpose == stored);
                }
                if (criteria?.StackLevel is { } stackLevel) query = query.Where(p => p.StackLevel == stackLevel);
            }
            var cleared = await query.ToListAsync(cancellationToken);
            foreach (var profile in cleared)
            {
                profile.Status = ChargingProfileStatusEnum.Cleared;
                profile.LastError = status == "Accepted" ? null : "Unknown on the charger";
            }
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("ClearChargingProfile answered {Status} by {ChargePointId}; {Count} stored profile(s) cleared",
                status, chargePointId, cleared.Count);
            return new ClearChargingProfileResult(status, cleared.Count);
        }

        public async Task<GetChargingProfilesResult> GetChargingProfilesAsync(string chargePointId, GetChargingProfilesRequest request,
            CancellationToken cancellationToken = default)
        {
            var chargePoint = await FindChargePoint(chargePointId, cancellationToken);
            if (Protocol(chargePointId) == OcppProtocols.Ocpp16)
                throw new ValidationException("OCPP 1.6 chargers cannot report their profiles; use the composite schedule.");

            request.ChargingProfile ??= new ChargingProfileCriterionType();
            if (request.RequestId <= 0)
                request.RequestId = RandomNumberGenerator.GetInt32(1, int.MaxValue);
            var criterion = request.ChargingProfile;
            var scope = new ChargingProfileReportScope(
                request.EvseId,
                criterion.ChargingProfilePurpose == null ? null : ChargingProfileMessages.FromOcpp201(criterion.ChargingProfilePurpose.Value),
                criterion.StackLevel,
                criterion.ChargingProfileId is { Count: > 0 } ids ? ids : null,
                criterion.ChargingLimitSource is not { Count: > 0 } sources || sources.Contains(ChargingLimitSourceEnumType.CSO));

            // Registered first: the charger may start reporting before its CALLRESULT is processed.
            _reportTracker.Register(chargePointId, request.RequestId, scope);
            GetChargingProfilesResponse response;
            try
            {
                response = await _commandSender.SendRequestAsync<GetChargingProfilesRequest, GetChargingProfilesResponse>(
                    chargePointId, "GetChargingProfiles", request, cancellationToken: cancellationToken);
            }
            catch
            {
                _reportTracker.Forget(chargePointId, request.RequestId);
                throw;
            }

            if (response.Status == GetChargingProfileStatusEnumType.NoProfiles)
            {
                _reportTracker.Forget(chargePointId, request.RequestId);
                await ReconcileReportedProfilesAsync(chargePointId, scope, Array.Empty<ReportChargingProfilesRequest>(), cancellationToken);
            }
            _logger.LogInformation("GetChargingProfiles {RequestId} answered {Status} by {ChargePointId} (charge point {ID})",
                request.RequestId, response.Status, chargePointId, chargePoint.ID);
            return new GetChargingProfilesResult(response.Status.ToString(), request.RequestId);
        }

        public async Task ReconcileReportedProfilesAsync(string chargePointId, ChargingProfileReportScope? scope,
            IReadOnlyList<ReportChargingProfilesRequest> parts, CancellationToken cancellationToken = default)
        {
            var chargePoint = await FindChargePoint(chargePointId, cancellationToken);
            var stored = await _db.ChargingProfiles
                .Where(p => p.ChargePointID == chargePoint.ID && p.Status != ChargingProfileStatusEnum.Cleared)
                .ToListAsync(cancellationToken);
            var seen = new HashSet<int>();
            var added = 0;

            foreach (var part in parts)
            {
                foreach (var reportedProfile in part.ChargingProfile ?? new List<ChargingProfileType>())
                {
                    var (reported, _) = ChargingProfileMessages.FromOcpp201(reportedProfile, part.EvseId);
                    var match = stored.FirstOrDefault(p => p.OcppProfileId == reported.OcppProfileId && p.EvseId == reported.EvseId);
                    if (match != null)
                    {
                        match.Status = ChargingProfileStatusEnum.Accepted;
                        match.LastError = null;
                        seen.Add(match.ID);
                        continue;
                    }
                    reported.ChargePointID = chargePoint.ID;
                    reported.Source = ChargingProfileSourceEnum.ChargerReported;
                    reported.Status = ChargingProfileStatusEnum.Accepted;
                    reported.ChargingLimitSource = part.ChargingLimitSource.ToString();
                    _db.ChargingProfiles.Add(reported);
                    stored.Add(reported);
                    added++;
                }
            }

            var missing = 0;
            if (scope is { IncludesCso: true })
            {
                foreach (var profile in stored.Where(p => p.ID != 0 && !seen.Contains(p.ID) && p.Status == ChargingProfileStatusEnum.Accepted
                             && p.Purpose != ChargingProfilePurposeEnum.ChargingStationExternalConstraints && InScope(p, scope)))
                {
                    profile.Status = ChargingProfileStatusEnum.Cleared;
                    profile.LastError = "Not reported by the charger";
                    missing++;
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("ReportChargingProfiles from {ChargePointId}: {Confirmed} confirmed, {Added} charger-reported added, {Missing} no longer on the charger",
                chargePointId, seen.Count, added, missing);
        }

        private static bool InScope(ChargingProfile profile, ChargingProfileReportScope scope) =>
            (scope.EvseId == null || profile.EvseId == scope.EvseId)
            && (scope.Purpose == null || profile.Purpose == scope.Purpose)
            && (scope.StackLevel == null || profile.StackLevel == scope.StackLevel)
            && (scope.ProfileIds == null || scope.ProfileIds.Contains(profile.OcppProfileId));

        public async Task<CompositeScheduleResult> GetCompositeScheduleAsync(string chargePointId, CompositeScheduleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            await FindChargePoint(chargePointId, cancellationToken);
            if (request.Duration <= 0) throw new ValidationException("The duration must be positive.");

            if (Protocol(chargePointId) == OcppProtocols.Ocpp16)
            {
                var response = await _commandSender.SendRequestAsync<V16.GetCompositeScheduleRequest, V16.GetCompositeScheduleResponse>(
                    chargePointId, "GetCompositeSchedule", new V16.GetCompositeScheduleRequest
                    {
                        ConnectorId = request.EvseId,
                        Duration = request.Duration,
                        ChargingRateUnit = request.ChargingRateUnit == null ? null
                            : request.ChargingRateUnit == ChargingRateUnitEnum.W ? V16.ChargingRateUnitType.W : V16.ChargingRateUnitType.A
                    }, cancellationToken: cancellationToken);
                var schedule = response.ChargingSchedule;
                return new CompositeScheduleResult(
                    response.Status.ToString(),
                    response.ConnectorId ?? request.EvseId,
                    AsUtc(response.ScheduleStart ?? schedule?.StartSchedule),
                    schedule?.Duration,
                    schedule == null ? null : schedule.ChargingRateUnit == V16.ChargingRateUnitType.W ? ChargingRateUnitEnum.W : ChargingRateUnitEnum.A,
                    schedule?.ChargingSchedulePeriod.Select(p => new ChargingProfilePeriod(p.StartPeriod, (double)p.Limit, p.NumberPhases)).ToList()
                        ?? new List<ChargingProfilePeriod>(),
                    null);
            }

            var response201 = await _commandSender.SendRequestAsync<GetCompositeScheduleRequest, GetCompositeScheduleResponse>(
                chargePointId, "GetCompositeSchedule", new GetCompositeScheduleRequest
                {
                    EvseId = request.EvseId,
                    Duration = request.Duration,
                    ChargingRateUnit = request.ChargingRateUnit == null ? null
                        : request.ChargingRateUnit == ChargingRateUnitEnum.W ? ChargingRateUnitEnumType.W : ChargingRateUnitEnumType.A
                }, cancellationToken: cancellationToken);
            var composite = response201.Schedule;
            return new CompositeScheduleResult(
                response201.Status.ToString(),
                composite?.EvseId ?? request.EvseId,
                composite == null ? null : AsUtc(composite.ScheduleStart),
                composite?.Duration,
                composite == null ? null : composite.ChargingRateUnit == ChargingRateUnitEnumType.W ? ChargingRateUnitEnum.W : ChargingRateUnitEnum.A,
                composite?.ChargingSchedulePeriod?.Select(p => new ChargingProfilePeriod(p.StartPeriod, p.Limit, p.NumberPhases, p.PhaseToUse)).ToList()
                    ?? new List<ChargingProfilePeriod>(),
                Reason(response201.StatusInfo));
        }

        public async Task<List<ChargingProfileDto>> GetStoredProfilesAsync(int chargePointDbId, bool includeCleared, CancellationToken cancellationToken = default)
        {
            var query = _db.ChargingProfiles.AsNoTracking().Where(p => p.ChargePointID == chargePointDbId);
            if (!includeCleared)
                query = query.Where(p => p.Status != ChargingProfileStatusEnum.Cleared);
            var profiles = await query.OrderBy(p => p.EvseId).ThenBy(p => p.Purpose).ThenByDescending(p => p.StackLevel).ThenByDescending(p => p.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            return profiles.Select(ChargingProfileDto.From).ToList();
        }

        private async Task<ChargePoint> FindChargePoint(string chargePointId, CancellationToken cancellationToken) =>
            await _db.ChargePoints.AsNoTracking().SingleOrDefaultAsync(c => c.ChargePointId == chargePointId, cancellationToken)
            ?? throw new NotFoundException($"Charge point {chargePointId} does not exist.");

        private string Protocol(string chargePointId) =>
            _commandSender.GetProtocolVersion(chargePointId) ?? throw new WebSocketNotFoundException($"Charge point {chargePointId} is not connected.");

        private DateTime? DefaultStartSchedule(ChargingProfileKindEnum kind) => kind switch
        {
            ChargingProfileKindEnum.Absolute => TruncateToSeconds(_clock.UtcNow),
            ChargingProfileKindEnum.Recurring => _clock.StartOfDayUtc(_clock.Today),
            _ => null
        };

        private static DateTime TruncateToSeconds(DateTime utc) => new(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        private static DateTime? AsUtc(DateTime? value) => value switch
        {
            null => null,
            { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
            var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
        };

        private static string? Reason(StatusInfoType? statusInfo) => statusInfo == null
            ? null
            : string.IsNullOrWhiteSpace(statusInfo.AdditionalInfo) ? statusInfo.ReasonCode : $"{statusInfo.ReasonCode}: {statusInfo.AdditionalInfo}";

        private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
    }
}
