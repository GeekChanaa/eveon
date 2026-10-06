using VoltaXApi.ScaleOut;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Sends CostUpdated (OCPP 2.0.1) to chargers. A charger that answers NotImplemented / NotSupported is not asked
    /// again for the lifetime of the process. Singleton.
    /// </summary>
    public interface ICostUpdatedSender
    {
        bool IsSuppressed(string chargePointId);
        /// <summary>Sends the cost; false when the charger is skipped (offline, not 2.0.1, rejected before) or rejects it now.</summary>
        Task<bool> SendAsync(string chargePointId, string transactionId, double totalCost, CancellationToken cancellationToken = default);
    }

    public sealed class CostUpdatedSender : ICostUpdatedSender
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
        private readonly IOcppCommandSender _commandSender;
        private readonly ILogger<CostUpdatedSender> _logger;
        private readonly ConcurrentDictionary<string, byte> _rejected = new(StringComparer.Ordinal);

        public CostUpdatedSender(IOcppCommandSender commandSender, ILogger<CostUpdatedSender> logger)
        {
            _commandSender = commandSender;
            _logger = logger;
        }

        public bool IsSuppressed(string chargePointId) => _rejected.ContainsKey(chargePointId);

        /// <summary>The 2.0.1 totalCost is a decimal amount; billing amounts are rounded to cents.</summary>
        public static double RoundCost(double cost) => Math.Round(Math.Max(0, cost), 2, MidpointRounding.AwayFromZero);

        public async Task<bool> SendAsync(string chargePointId, string transactionId, double totalCost, CancellationToken cancellationToken = default)
        {
            if (IsSuppressed(chargePointId) || _commandSender.GetProtocolVersion(chargePointId) != OcppProtocols.Ocpp201) return false;
            var request = new CostUpdatedRequest { TotalCost = RoundCost(totalCost), TransactionId = transactionId };
            try
            {
                await _commandSender.SendRequestAsync<CostUpdatedRequest, CostUpdatedResponse>(chargePointId, "CostUpdated", request, Timeout, cancellationToken);
                _logger.LogDebug("CostUpdated {TotalCost} sent to {ChargePointId} for transaction {TransactionId}", request.TotalCost, chargePointId, transactionId);
                return true;
            }
            catch (OcppCallErrorException ex) when (ex.ErrorCode is "NotImplemented" or "NotSupported")
            {
                _rejected.TryAdd(chargePointId, 0);
                _logger.LogInformation("{ChargePointId} does not support CostUpdated ({ErrorCode}); it will not be sent again", chargePointId, ex.ErrorCode);
                return false;
            }
        }

        /// <summary>Sends the final cost of a transaction that just ended without blocking the caller (e.g. a TransactionEvent handler).</summary>
        public static void SendFinalInBackground(IServiceScopeFactory scopeFactory, ILogger logger, string chargePointId, string? transactionId, double finalCost)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            OcppBackgroundCommand.Run(scopeFactory, logger, "CostUpdated (final)", chargePointId,
                services => services.GetRequiredService<ICostUpdatedSender>().SendAsync(chargePointId, transactionId, finalCost));
        }
    }

    /// <summary>
    /// Every Ocpp:CostUpdatedIntervalSeconds (default 60, 0 disables) sends the running cost of every active transaction
    /// on a connected OCPP 2.0.1 charger that can show it. Each run uses a fresh DI scope.
    /// </summary>
    public sealed class CostUpdatedHostedService : BackgroundService
    {
        private const int MaxParallelSends = 16;
        // Device-model components through which a 2.0.1 charger shows a running cost.
        private static readonly string[] CostDisplayComponents = { "TariffCostCtrlr", "DisplayMessageCtrlr" };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICostUpdatedSender _sender;
        private readonly IOcppCommandSender _commandSender;
        private readonly ILogger<CostUpdatedHostedService> _logger;
        private readonly IClusterJobLease _lease;
        private readonly TimeSpan _interval;

        public CostUpdatedHostedService(IServiceScopeFactory scopeFactory, ICostUpdatedSender sender, IOcppCommandSender commandSender,
            IConfiguration configuration, ILogger<CostUpdatedHostedService> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _sender = sender;
            _commandSender = commandSender;
            _logger = logger;
            _interval = TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("Ocpp:CostUpdatedIntervalSeconds", 60)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_interval <= TimeSpan.Zero)
            {
                _logger.LogInformation("CostUpdated sender disabled (Ocpp:CostUpdatedIntervalSeconds = 0)");
                return;
            }
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // One replica sends for every charger; commands are routed to the owning instance.
                    if (await _lease.TryAcquireAsync("ocpp-cost-updated", _interval * 3, stoppingToken))
                        await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CostUpdated run failed");
                }
            }
        }

        public sealed record ActiveTransaction(string ChargePointId, int ChargePointDbId, string TransactionId, DateTime StartTime,
            double MeterStart, double? MeterStop, Connector Connector);

        /// <summary>Running cost of an active transaction, with the billing formula (see <see cref="ICostCalculator.RunningCost"/>).</summary>
        public static double RunningCost(ICostCalculator calculator, ActiveTransaction t, DateTime nowUtc) =>
            calculator.RunningCost(t.Connector, Math.Max(0, (t.MeterStop ?? t.MeterStart) - t.MeterStart), Math.Max(0, (nowUtc - t.StartTime).TotalMinutes));

        public async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var calculator = scope.ServiceProvider.GetRequiredService<ICostCalculator>();

            var active = await db.Transactions.AsNoTracking()
                .Where(t => t.Status == TransactionStatusEnum.Current && t.Uid != null && t.Connector != null && t.Connector.ChargePoint != null)
                .Select(t => new ActiveTransaction(t.Connector!.ChargePoint!.ChargePointId, t.Connector.ChargePoint.ID, t.Uid!, t.StartTime,
                    t.MeterStart, t.MeterStop, t.Connector))
                .ToListAsync(cancellationToken);
            var candidates = active.Where(t => !_sender.IsSuppressed(t.ChargePointId) &&
                                               _commandSender.GetProtocolVersion(t.ChargePointId) == OcppProtocols.Ocpp201).ToList();
            if (candidates.Count == 0) return;

            // Chargers whose device model is known but has no cost / display component cannot show the cost.
            var ids = candidates.Select(t => t.ChargePointDbId).Distinct().ToList();
            var components = await db.OCPPConfigurationItems.AsNoTracking()
                .Where(i => ids.Contains(i.ChargePointID) && i.OCPPConfigurationComponent != null)
                .Select(i => new { i.ChargePointID, i.OCPPConfigurationComponent!.Name })
                .Distinct()
                .ToListAsync(cancellationToken);
            var known = components.Select(c => c.ChargePointID).ToHashSet();
            var canShow = components.Where(c => CostDisplayComponents.Contains(c.Name)).Select(c => c.ChargePointID).ToHashSet();
            candidates = candidates.Where(t => !known.Contains(t.ChargePointDbId) || canShow.Contains(t.ChargePointDbId)).ToList();

            var now = DateTime.UtcNow;
            await Parallel.ForEachAsync(candidates, new ParallelOptions { MaxDegreeOfParallelism = MaxParallelSends, CancellationToken = cancellationToken },
                async (t, ct) =>
                {
                    try
                    {
                        await _sender.SendAsync(t.ChargePointId, t.TransactionId, RunningCost(calculator, t, now), ct);
                    }
                    catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException or WebSocketNotFoundException)
                    {
                        _logger.LogWarning(ex, "CostUpdated to {ChargePointId} for transaction {TransactionId} failed", t.ChargePointId, t.TransactionId);
                    }
                });
        }
    }
}
