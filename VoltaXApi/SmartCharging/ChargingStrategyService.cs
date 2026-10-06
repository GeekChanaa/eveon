using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;
using ValidationException = VoltaXApi.Exceptions.ValidationException;

namespace VoltaXApi.SmartCharging
{
    public sealed record ChargingStrategyDto(
        int ID,
        string Name,
        string? Description,
        ChargingProfilePurposeEnum Purpose,
        ChargingProfileKindEnum Kind,
        ChargingProfileRecurrencyEnum? RecurrencyKind,
        ChargingRateUnitEnum ChargingRateUnit,
        int StackLevel,
        List<ChargingStrategyPeriod> Periods,
        bool IsPredefined,
        DateTime UpdatedAt)
    {
        public static ChargingStrategyDto From(ChargingStrategy s) => new(s.ID, s.Name, s.Description, s.Purpose, s.Kind, s.RecurrencyKind,
            s.ChargingRateUnit, s.StackLevel, SmartChargingJson.Deserialize<ChargingStrategyPeriod>(s.PeriodsJson), s.IsPredefined, s.UpdatedAt);
    }

    public class ChargingStrategyInputDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ChargingProfilePurposeEnum Purpose { get; set; } = ChargingProfilePurposeEnum.TxDefaultProfile;
        public ChargingProfileKindEnum Kind { get; set; } = ChargingProfileKindEnum.Recurring;
        public ChargingProfileRecurrencyEnum? RecurrencyKind { get; set; }
        public ChargingRateUnitEnum ChargingRateUnit { get; set; } = ChargingRateUnitEnum.A;

        [Range(0, 100)]
        public int StackLevel { get; set; }

        [Required]
        public List<ChargingStrategyPeriod> Periods { get; set; } = new();
    }

    public class ApplyChargingStrategyDto
    {
        public List<int> ChargePointIDs { get; set; } = new();
        public List<int> ChargingStationIDs { get; set; } = new();
    }

    /// <summary>The real answer of one charger to the generated profile.</summary>
    public sealed record StrategyApplyResult(int ChargePointID, string ChargePointIdentity, string Status, string? Reason, int? ChargingProfileID);

    public interface IChargingStrategyService
    {
        Task<List<ChargingStrategyDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ChargingStrategyDto> GetAsync(int id, CancellationToken cancellationToken = default);
        Task<ChargingStrategyDto> CreateAsync(ChargingStrategyInputDto input, int? userId, CancellationToken cancellationToken = default);
        Task<ChargingStrategyDto> UpdateAsync(int id, ChargingStrategyInputDto input, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Generates the strategy's profile for every selected charger (and every charger of the selected stations) and sends it.</summary>
        Task<List<StrategyApplyResult>> ApplyAsync(int id, ApplyChargingStrategyDto targets, int? userId, CancellationToken cancellationToken = default);
    }

    public class ChargingStrategyService : IChargingStrategyService
    {
        public const int MaxChargersPerApply = 200;
        private const int Parallelism = 8;

        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChargingStrategyService> _logger;

        public ChargingStrategyService(VoltaXApiDbContext db, IBusinessClock clock, IServiceScopeFactory scopeFactory, ILogger<ChargingStrategyService> logger)
        {
            _db = db;
            _clock = clock;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<ChargingStrategyDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            (await _db.ChargingStrategies.AsNoTracking().OrderByDescending(s => s.IsPredefined).ThenBy(s => s.Name).ToListAsync(cancellationToken))
            .Select(ChargingStrategyDto.From).ToList();

        public async Task<ChargingStrategyDto> GetAsync(int id, CancellationToken cancellationToken = default) =>
            ChargingStrategyDto.From(await Find(id, cancellationToken));

        public async Task<ChargingStrategyDto> CreateAsync(ChargingStrategyInputDto input, int? userId, CancellationToken cancellationToken = default)
        {
            var strategy = new ChargingStrategy { CreatedByUserID = userId };
            Apply(strategy, input);
            _db.ChargingStrategies.Add(strategy);
            await _db.SaveChangesAsync(cancellationToken);
            return ChargingStrategyDto.From(strategy);
        }

        public async Task<ChargingStrategyDto> UpdateAsync(int id, ChargingStrategyInputDto input, CancellationToken cancellationToken = default)
        {
            var strategy = await Find(id, cancellationToken);
            Apply(strategy, input);
            await _db.SaveChangesAsync(cancellationToken);
            return ChargingStrategyDto.From(strategy);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var strategy = await Find(id, cancellationToken);
            if (strategy.IsPredefined)
                throw new ValidationException("Predefined strategies cannot be deleted.");
            _db.ChargingStrategies.Remove(strategy);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<StrategyApplyResult>> ApplyAsync(int id, ApplyChargingStrategyDto targets, int? userId, CancellationToken cancellationToken = default)
        {
            var strategy = await Find(id, cancellationToken);
            var periods = SmartChargingJson.Deserialize<ChargingStrategyPeriod>(strategy.PeriodsJson);
            var shape = ChargingStrategyConverter.ToProfileShape(strategy, periods, _clock.TimeZone, _clock.UtcNow);

            var pointIds = targets.ChargePointIDs ?? new List<int>();
            var stationIds = targets.ChargingStationIDs ?? new List<int>();
            var chargers = await _db.ChargePoints.AsNoTracking()
                .Where(c => pointIds.Contains(c.ID) || stationIds.Contains(c.ChargingStationID))
                .Select(c => new { c.ID, c.ChargePointId })
                .Distinct()
                .ToListAsync(cancellationToken);
            if (chargers.Count == 0) throw new ValidationException("Select at least one charge point or station.");
            if (chargers.Count > MaxChargersPerApply) throw new ValidationException($"A strategy is applied to at most {MaxChargersPerApply} chargers at once.");

            using var gate = new SemaphoreSlim(Parallelism);
            var results = await Task.WhenAll(chargers.Select(async charger =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return await ApplyToCharger(strategy, shape, charger.ID, charger.ChargePointId, userId, cancellationToken);
                }
                finally
                {
                    gate.Release();
                }
            }));
            _logger.LogInformation("Strategy {StrategyId} ({Name}) applied to {Count} charger(s): {Accepted} accepted",
                strategy.ID, strategy.Name, results.Length, results.Count(r => r.Status == "Accepted"));
            return results.OrderBy(r => r.ChargePointIdentity).ToList();
        }

        /// <summary>One scope per charger: the sends run in parallel and a DbContext is not thread safe.</summary>
        private async Task<StrategyApplyResult> ApplyToCharger(ChargingStrategy strategy, StrategyProfileShape shape, int chargePointDbId,
            string identity, int? userId, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var smartCharging = scope.ServiceProvider.GetRequiredService<ISmartChargingService>();
            try
            {
                // Applying a strategy again (this one or another of the same purpose) replaces the previous strategy profile.
                var existing = await db.ChargingProfiles.AsNoTracking()
                    .Where(p => p.ChargePointID == chargePointDbId && p.EvseId == 0 && p.Purpose == strategy.Purpose
                        && p.Source == ChargingProfileSourceEnum.Strategy && p.Status != ChargingProfileStatusEnum.Cleared)
                    .OrderByDescending(p => p.ID)
                    .Select(p => (int?)p.ID)
                    .FirstOrDefaultAsync(cancellationToken);
                var input = new ChargingProfileInputDto
                {
                    ChargingProfileID = existing,
                    EvseId = 0,
                    StackLevel = strategy.StackLevel,
                    Purpose = strategy.Purpose,
                    Kind = shape.Kind,
                    RecurrencyKind = shape.RecurrencyKind,
                    StartSchedule = shape.StartSchedule,
                    Duration = shape.Duration,
                    ChargingRateUnit = strategy.ChargingRateUnit,
                    Periods = shape.Periods
                };
                var result = await smartCharging.SendChargingProfileAsync(identity, input, ChargingProfileSourceEnum.Strategy, userId, strategy.ID, cancellationToken);
                return new StrategyApplyResult(chargePointDbId, identity, result.Status, result.Reason, result.Profile.ID);
            }
            catch (Exception ex) when (ex is TimeoutException or WebSocketNotFoundException or OcppCallErrorException or ValidationException or NotFoundException)
            {
                var status = ex switch
                {
                    TimeoutException => "Timeout",
                    WebSocketNotFoundException => "NotConnected",
                    OcppCallErrorException => "CallError",
                    _ => "Invalid"
                };
                _logger.LogWarning(ex, "Strategy {StrategyId} not applied to {ChargePointId}: {Status}", strategy.ID, identity, status);
                return new StrategyApplyResult(chargePointDbId, identity, status, ex.Message, null);
            }
        }

        private static void Apply(ChargingStrategy strategy, ChargingStrategyInputDto input)
        {
            strategy.Name = input.Name?.Trim() ?? string.Empty;
            strategy.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            strategy.Purpose = input.Purpose;
            strategy.Kind = input.Kind;
            strategy.RecurrencyKind = input.Kind == ChargingProfileKindEnum.Recurring ? input.RecurrencyKind : null;
            strategy.ChargingRateUnit = input.ChargingRateUnit;
            strategy.StackLevel = input.StackLevel;
            var periods = (input.Periods ?? new List<ChargingStrategyPeriod>()).OrderBy(p => p.StartSeconds).ToList();
            ChargingStrategyConverter.Validate(strategy, periods);
            strategy.PeriodsJson = SmartChargingJson.Serialize(periods);
        }

        private async Task<ChargingStrategy> Find(int id, CancellationToken cancellationToken) =>
            await _db.ChargingStrategies.SingleOrDefaultAsync(s => s.ID == id, cancellationToken)
            ?? throw new NotFoundException($"Charging strategy {id} does not exist.");
    }
}
