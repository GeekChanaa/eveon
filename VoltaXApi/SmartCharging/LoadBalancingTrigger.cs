using VoltaXApi.Exceptions;
using VoltaXApi.ScaleOut;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;

namespace VoltaXApi.SmartCharging
{
    /// <summary>
    /// Asks for a station's load to be rebalanced. Safe to call from anywhere (OCPP handlers included): it returns
    /// immediately, never throws, and coalesces the requests of a station arriving within <see cref="LoadBalancingTrigger.Debounce"/>.
    /// </summary>
    public interface ILoadBalancingTrigger
    {
        void RequestRebalance(int chargingStationId);

        /// <summary>Same, for the station of a charger (OCPP identity), e.g. after a transaction started, stopped or metered.</summary>
        void RequestRebalanceForChargePoint(string chargePointId);

        /// <summary>Rebalances now (serialized with the debounced runs of the same station) and returns the outcome.</summary>
        Task<RebalanceSummary> RebalanceNowAsync(int chargingStationId, string reason, CancellationToken cancellationToken = default);
    }

    /// <summary>Singleton. Each run gets its own DI scope; runs of one station never overlap.</summary>
    public sealed class LoadBalancingTrigger : ILoadBalancingTrigger
    {
        public static readonly TimeSpan Debounce = TimeSpan.FromSeconds(5);
        // Longer than two safety passes, so the owning replica keeps it while it is alive.
        private static readonly TimeSpan StationLeaseTtl = TimeSpan.FromMinutes(3);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LoadBalancingTrigger> _logger;
        private readonly CancellationToken _stopping;
        private readonly ConcurrentDictionary<int, byte> _scheduled = new();
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

        public LoadBalancingTrigger(IServiceScopeFactory scopeFactory, ILogger<LoadBalancingTrigger> logger, IHostApplicationLifetime lifetime)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _stopping = lifetime.ApplicationStopping;
        }

        public void RequestRebalance(int chargingStationId)
        {
            if (chargingStationId <= 0 || _stopping.IsCancellationRequested || !_scheduled.TryAdd(chargingStationId, 0))
                return;

            OcppBackgroundCommand.Run(_scopeFactory, _logger, "LoadBalancing", $"station {chargingStationId}", async services =>
            {
                try
                {
                    await Task.Delay(Debounce, _stopping);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    // Requests arriving from now on schedule another run: this one may already have read the state.
                    _scheduled.TryRemove(chargingStationId, out _);
                }
                await RunLocked(services, chargingStationId, "event", _stopping);
            });
        }

        public void RequestRebalanceForChargePoint(string chargePointId)
        {
            if (string.IsNullOrWhiteSpace(chargePointId) || _stopping.IsCancellationRequested)
                return;

            OcppBackgroundCommand.Run(_scopeFactory, _logger, "LoadBalancing", chargePointId, async services =>
            {
                var db = services.GetRequiredService<VoltaXApiDbContext>();
                var stationId = await db.ChargePoints.AsNoTracking()
                    .Where(c => c.ChargePointId == chargePointId)
                    .Select(c => (int?)c.ChargingStationID)
                    .FirstOrDefaultAsync(_stopping);
                // Stations without settings have nothing to balance (nor load balancer profiles to release).
                if (stationId != null && await db.StationLoadLimits.AnyAsync(l => l.ChargingStationID == stationId, _stopping))
                    RequestRebalance(stationId.Value);
            });
        }

        public async Task<RebalanceSummary> RebalanceNowAsync(int chargingStationId, string reason, CancellationToken cancellationToken = default)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            return await RunLocked(scope.ServiceProvider, chargingStationId, reason, cancellationToken);
        }

        private async Task<RebalanceSummary> RunLocked(IServiceProvider services, int chargingStationId, string reason, CancellationToken cancellationToken)
        {
            // Scale-out: a station's chargers can sit on different replicas. Only the replica holding the
            // station's lease balances it, so two replicas never hand out the same capacity at once.
            var lease = services.GetRequiredService<IClusterJobLease>();
            if (!await lease.TryAcquireAsync($"load-balancing:{chargingStationId}", StationLeaseTtl, cancellationToken))
            {
                if (reason == "manual")
                    throw new ValidationException("Another API instance is balancing this station; it applies the current settings within a minute.");
                _logger.LogDebug("Load balancing of station {StationId} is owned by another instance; {Reason} run skipped", chargingStationId, reason);
                return new RebalanceSummary(chargingStationId, true, 0, 0, 0, 0, 0, Array.Empty<SessionAllocationView>());
            }

            var gate = _locks.GetOrAdd(chargingStationId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await services.GetRequiredService<ILoadBalancingService>().RebalanceStationAsync(chargingStationId, reason, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }
    }

    /// <summary>Periodic safety pass: rebalances every station with load balancing settings, catching missed events and failed sends.</summary>
    public sealed class LoadBalancingSafetyService : BackgroundService
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoadBalancingTrigger _trigger;
        private readonly ILogger<LoadBalancingSafetyService> _logger;

        public LoadBalancingSafetyService(IServiceScopeFactory scopeFactory, ILoadBalancingTrigger trigger, ILogger<LoadBalancingSafetyService> logger)
        {
            _scopeFactory = scopeFactory;
            _trigger = trigger;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                List<int> stationIds;
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                    // Disabled stations are visited while they still hold load balancer profiles to release.
                    stationIds = await db.StationLoadLimits.AsNoTracking()
                        .Where(l => l.Enabled || db.ChargingProfiles.Any(p => p.Source == Models.ChargingProfileSourceEnum.LoadBalancer
                            && p.Status != Models.ChargingProfileStatusEnum.Cleared && p.ChargePoint!.ChargingStationID == l.ChargingStationID))
                        .Select(l => l.ChargingStationID)
                        .ToListAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Load balancing safety pass: stations could not be read");
                    continue;
                }

                foreach (var stationId in stationIds)
                {
                    try
                    {
                        await _trigger.RebalanceNowAsync(stationId, "periodic", stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Load balancing safety pass failed for station {StationId}", stationId);
                    }
                }
            }
        }
    }
}
