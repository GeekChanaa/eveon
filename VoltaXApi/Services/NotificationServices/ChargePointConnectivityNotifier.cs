using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Notifies on every charge point connection to and disconnection from the CMS, and escalates
    /// with an urgent "offline" notification when a charge point stays disconnected past a grace period.
    /// </summary>
    public class ChargePointConnectivityNotifier
    {
        private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(2);

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingOffline = new();
        // Charge points announced as offline; their next connection is reported as "back online".
        private readonly ConcurrentDictionary<string, byte> _reportedOffline = new();
        private readonly IServiceScopeFactory _scopeFactory;
        // Open on any instance (scale-out): a charger that moved to another replica is not offline.
        private readonly ChargePointStatusManagerService _chargePoints;
        private readonly ILogger<ChargePointConnectivityNotifier> _logger;

        public ChargePointConnectivityNotifier(IServiceScopeFactory scopeFactory, ChargePointStatusManagerService chargePoints, ILogger<ChargePointConnectivityNotifier> logger)
        {
            _scopeFactory = scopeFactory;
            _chargePoints = chargePoints;
            _logger = logger;
        }

        public void Connected(string chargePointId)
        {
            if (_pendingOffline.TryRemove(chargePointId, out var pending)) pending.Cancel();
            _ = NotifyAsync(chargePointId, _reportedOffline.TryRemove(chargePointId, out _) ? ConnectivityEvent.BackOnline : ConnectivityEvent.Connected);
        }

        public void Disconnected(string chargePointId)
        {
            // A new connection can open before the old socket's close is processed: nothing was lost.
            if (_chargePoints.ChargePointExists(chargePointId)) return;
            _ = NotifyAsync(chargePointId, ConnectivityEvent.Disconnected);
            var cts = new CancellationTokenSource();
            _pendingOffline.AddOrUpdate(chargePointId, cts, (_, previous) => { previous.Cancel(); return cts; });
            _ = NotifyOfflineAfterGracePeriod(chargePointId, cts);
        }

        private async Task NotifyOfflineAfterGracePeriod(string chargePointId, CancellationTokenSource cts)
        {
            try { await Task.Delay(GracePeriod, cts.Token); }
            catch (OperationCanceledException) { return; }

            if (!_pendingOffline.TryRemove(new KeyValuePair<string, CancellationTokenSource>(chargePointId, cts))) return;
            // A new connection can open before the old socket's close is processed.
            if (_chargePoints.ChargePointExists(chargePointId)) return;
            _reportedOffline[chargePointId] = 0;
            await NotifyAsync(chargePointId, ConnectivityEvent.Offline);
        }

        private enum ConnectivityEvent { Connected, Disconnected, Offline, BackOnline }

        private async Task NotifyAsync(string chargePointId, ConnectivityEvent connectivityEvent)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var chargePoint = await db.ChargePoints.AsNoTracking()
                    .Where(cp => cp.ChargePointId == chargePointId && !cp.IsDeleted)
                    .Select(cp => new { cp.ID, cp.ChargePointId, StationName = cp.ChargingStation.Name, cp.ChargingStation.PartnerID })
                    .FirstOrDefaultAsync();
                if (chargePoint == null) return;

                var name = $"Charge point {chargePoint.ChargePointId} ({chargePoint.StationName})";
                var (action, description) = connectivityEvent switch
                {
                    ConnectivityEvent.Offline => ("ChargePointOffline", $"{name} has been offline for more than {GracePeriod.TotalMinutes:0} minutes."),
                    ConnectivityEvent.Disconnected => ("ChargePointDisconnected", $"{name} disconnected from the CMS."),
                    ConnectivityEvent.BackOnline => ("ChargePointOnline", $"{name} is back online."),
                    _ => ("ChargePointConnected", $"{name} connected to the CMS.")
                };
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notifications.NotifyDashboardAsync(
                    new DashboardNotification("Charge Point", action, description, chargePoint.ID.ToString(), Urgent: connectivityEvent == ConnectivityEvent.Offline),
                    "ViewChargePoints", $"/dashboard/charging-points/{chargePoint.ID}", chargePoint.PartnerID, "/partner-dashboard/connector-realtime");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChargePointConnectivityNotifier => Failed to notify {ChargePointId} {Event}", chargePointId, connectivityEvent);
            }
        }
    }
}
