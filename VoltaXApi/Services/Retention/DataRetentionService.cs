using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Daily purge of data that otherwise grows forever: OCPP message logs older than
    /// <c>Retention:MessageLogDays</c> (90) and read notifications older than
    /// <c>Retention:NotificationDays</c> (180). Deletes run in small batches so no single
    /// statement holds long locks on tables the chargers write to constantly.
    /// </summary>
    public class DataRetentionService : BackgroundService
    {
        private const int BatchSize = 5000;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan PauseBetweenBatches = TimeSpan.FromMilliseconds(200);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IClusterJobLease _lease;

        public DataRetentionService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DataRetentionService> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(StartupDelay, stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (await _lease.TryAcquireAsync("data-retention", Interval + TimeSpan.FromHours(1), stoppingToken))
                        await PurgeAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Data retention sweep failed");
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }

        private async Task PurgeAsync(CancellationToken ct)
        {
            var messageLogDays = Math.Max(1, _configuration.GetValue("Retention:MessageLogDays", 90));
            var notificationDays = Math.Max(1, _configuration.GetValue("Retention:NotificationDays", 180));
            var messageCutoff = DateTime.UtcNow.AddDays(-messageLogDays);
            var notificationCutoff = DateTime.UtcNow.AddDays(-notificationDays);

            var logs = await DeleteInBatchesAsync(db => db.MessageLogs.IgnoreQueryFilters()
                .Where(m => m.LogTime < messageCutoff).Take(BatchSize), ct);
            var notifications = await DeleteInBatchesAsync(db => db.Notifications.IgnoreQueryFilters()
                .Where(n => (n.Read || n.Deleted || n.IsDeleted) && n.CreatedAt < notificationCutoff).Take(BatchSize), ct);

            if (logs > 0 || notifications > 0)
                _logger.LogInformation("Retention removed {MessageLogs} message logs and {Notifications} notifications", logs, notifications);
        }

        private async Task<int> DeleteInBatchesAsync<T>(Func<VoltaXApiDbContext, IQueryable<T>> batch, CancellationToken ct) where T : class
        {
            var total = 0;
            while (!ct.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var deleted = await batch(db).ExecuteDeleteAsync(ct);
                total += deleted;
                if (deleted < BatchSize) break;
                await Task.Delay(PauseBetweenBatches, ct);
            }
            return total;
        }
    }
}
