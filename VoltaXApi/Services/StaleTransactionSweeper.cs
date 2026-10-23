using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Closes transactions a charger will never end: the connector reported Faulted / Unavailable and nothing
    /// came for <c>Ocpp:FaultedTransactionMinutes</c> (default 60), or nothing at all came for
    /// <c>Ocpp:StaleTransactionHours</c> (default 12; a power cut that made the charger forget the
    /// transaction). They are ended like an Ended event at their last meter reading (charger clock), on their
    /// last meter value, so
    /// the customer pays what was measured and the connector is released. A charger that sends the real Ended
    /// event later is acknowledged without billing again.
    /// </summary>
    public sealed class StaleTransactionSweeper
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StaleTransactionSweeper> _logger;
        private readonly TimeSpan _staleAfter;
        private readonly TimeSpan _faultedAfter;

        public StaleTransactionSweeper(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<StaleTransactionSweeper> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _staleAfter = TimeSpan.FromHours(Math.Max(1, configuration.GetValue("Ocpp:StaleTransactionHours", 12)));
            _faultedAfter = TimeSpan.FromMinutes(Math.Max(5, configuration.GetValue("Ocpp:FaultedTransactionMinutes", 60)));
        }

        /// <summary>Closes what is due at <paramref name="nowUtc"/>; returns the number of transactions closed.</summary>
        public async Task<int> CloseDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var faultedBefore = nowUtc - _faultedAfter;
            var staleBefore = nowUtc - _staleAfter;
            List<Candidate> candidates;
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                candidates = await db.Transactions.AsNoTracking()
                    .Where(t => t.Status == TransactionStatusEnum.Current && t.StopTime == null && t.Uid != null
                                && t.ConnectorID != null && t.UpdatedAt < faultedBefore)
                    .Select(t => new Candidate(t.Uid!, t.ConnectorID!.Value, t.Connector!.ChargePoint!.ChargePointId, t.UpdatedAt,
                        t.StartTime, t.ChargingSession!.ChargedMinutes,
                        db.ConnectorStatuses.Where(s => s.ConnectorID == t.ConnectorID).Select(s => (ConnectorStatusEnumType?)s.LastStatus).FirstOrDefault()))
                    .ToListAsync(cancellationToken);
            }

            var closed = 0;
            foreach (var candidate in candidates)
            {
                var faulted = candidate.ConnectorStatus is ConnectorStatusEnumType.Faulted or ConnectorStatusEnumType.Unavailable;
                if (!faulted && candidate.LastActivity >= staleBefore)
                    continue;
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                    var connector = await db.Connectors.FirstAsync(c => c.ID == candidate.ConnectorID, cancellationToken);
                    await scope.ServiceProvider.GetRequiredService<ITransactionService>().EndTransaction(new TransactionEventData
                    {
                        ChargePointId = candidate.ChargePointId,
                        TransactionUid = candidate.Uid,
                        EventType = TransactionEventEnumType.Ended,
                        Timestamp = DateTime.SpecifyKind(candidate.LastReading, DateTimeKind.Utc).ToString("o"),
                        MeterKWh = null,
                        TriggerReason = faulted ? "ConnectorFaulted" : "StaleTransaction",
                        AbnormalStop = true,
                        ReasonSuffix = faulted ? " (charger fault)" : " (no news from the charge point)"
                    }, connector);
                    closed++;
                    _logger.LogWarning("Transaction {TransactionUid} on {ChargePointId} closed by the server ({Reason}), last activity {LastActivity:o}",
                        candidate.Uid, candidate.ChargePointId, faulted ? "connector faulted" : "stale", candidate.LastActivity);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Could not close stale transaction {TransactionUid} on {ChargePointId}", candidate.Uid, candidate.ChargePointId);
                }
            }
            return closed;
        }

        private sealed record Candidate(string Uid, int ConnectorID, string ChargePointId, DateTime LastActivity, DateTime StartTime,
            double? ChargedMinutes, ConnectorStatusEnumType? ConnectorStatus)
        {
            /// <summary>Time of the last meter reading in the charger's clock (the session follows each update).</summary>
            public DateTime LastReading => StartTime.AddMinutes(Math.Max(0, ChargedMinutes ?? 0));
        }
    }

    public sealed class StaleTransactionHostedService : BackgroundService
    {
        private readonly StaleTransactionSweeper _sweeper;
        private readonly ILogger<StaleTransactionHostedService> _logger;
        private readonly TimeSpan _interval;

        public StaleTransactionHostedService(StaleTransactionSweeper sweeper, IConfiguration configuration, ILogger<StaleTransactionHostedService> logger)
        {
            _sweeper = sweeper;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Ocpp:StaleTransactionSweepMinutes", 10)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _sweeper.CloseDueAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Stale transaction sweep failed");
                }
            }
        }
    }
}
