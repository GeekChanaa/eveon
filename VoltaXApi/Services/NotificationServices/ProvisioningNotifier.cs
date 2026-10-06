using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Tells dashboard operators that a charge point connected for the first time and waits for its
    /// configuration. A pending charger repeats its BootNotification every minute, so the same charge
    /// point is announced at most once per <see cref="Cooldown"/>.
    /// </summary>
    public class ProvisioningNotifier
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(15);
        public const string Action = "ChargePointAwaitingProvisioning";

        private readonly ConcurrentDictionary<string, DateTime> _lastNotified = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProvisioningNotifier> _logger;

        public ProvisioningNotifier(IServiceScopeFactory scopeFactory, ILogger<ProvisioningNotifier> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void AwaitingProvisioning(string chargePointId)
        {
            var now = DateTime.UtcNow;
            if (_lastNotified.TryGetValue(chargePointId, out var last) && now - last < Cooldown) return;
            _lastNotified[chargePointId] = now;
            _ = NotifyAsync(chargePointId);
        }

        public void Provisioned(string chargePointId) => _lastNotified.TryRemove(chargePointId, out _);

        private async Task NotifyAsync(string chargePointId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var chargePoint = await db.ChargePoints.AsNoTracking()
                    .Where(cp => cp.ChargePointId == chargePointId)
                    .Select(cp => new { cp.ID, cp.ChargePointId, StationName = cp.ChargingStation != null ? cp.ChargingStation.Name : null })
                    .FirstOrDefaultAsync();
                if (chargePoint == null) return;

                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notifications.NotifyDashboardAsync(
                    new DashboardNotification("Charge Point", Action,
                        $"Charge point {chargePoint.ChargePointId} ({chargePoint.StationName}) connected for the first time and is waiting to be configured.",
                        chargePoint.ID.ToString(), Urgent: true),
                    "OperateChargePoints", $"/dashboard/charging-points/{chargePoint.ID}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ProvisioningNotifier => Failed to notify {ChargePointId}", chargePointId);
            }
        }
    }
}
