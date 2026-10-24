using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Data;
using VoltaXApi.Hubs;
using VoltaXApi.Models;
using VoltaXApi.Models.Alarms;

namespace VoltaXApi.Services.Alarms
{
    public enum AlarmChange { Raised, Updated, Cleared, Acknowledged, Assigned, Resolved }

    /// <summary>
    /// Tells the dashboard about alarms (singleton): every change is pushed on the charger hub to the
    /// <see cref="HubGroup"/> group (joined with ChargerHub.JoinChargerGroup, requires ViewAlarms); a new critical
    /// alarm also notifies the users who can see alarms and emails the alarm recipients, in the background so the
    /// charger's message is answered at once.
    /// </summary>
    public sealed class AlarmNotifier
    {
        public const string HubGroup = "#alarms";
        public const string HubMethod = "AlarmChanged";

        private readonly IHubContext<ChargerHub> _hub;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AlarmOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AlarmNotifier> _logger;

        public AlarmNotifier(IHubContext<ChargerHub> hub, IServiceScopeFactory scopeFactory, AlarmOptions options, IConfiguration configuration, ILogger<AlarmNotifier> logger)
        {
            _hub = hub;
            _scopeFactory = scopeFactory;
            _options = options;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task PublishAsync(AlarmChange change, AlarmDto? alarm)
        {
            if (alarm == null) return;
            try
            {
                await _hub.Clients.Group(HubGroup).SendAsync(HubMethod, new { change = change.ToString(), alarm });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not push alarm {AlarmId} ({Change}) to the dashboard", alarm.ID, change);
            }
        }

        /// <summary>Notifies and emails a new critical alarm in the background; never throws.</summary>
        public Task NotifyCritical(AlarmDto alarm) => Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var where = alarm.StationName == null ? alarm.ChargePointIdentity : $"{alarm.ChargePointIdentity} ({alarm.StationName})";
                var description = $"Critical alarm on {where}: {alarm.Description}";
                await scope.ServiceProvider.GetRequiredService<INotificationService>().NotifyDashboardAsync(
                    new DashboardNotification("Alarm", "CriticalAlarm", description, alarm.ID.ToString(), Urgent: true),
                    nameof(PermissionEnum.ViewAlarms), $"/dashboard/alarm-management?alarm={alarm.ID}");

                if (_options.EmailRecipients.Count == 0) return;
                var link = (_configuration["SpaLink"] ?? "").TrimEnd('/');
                await scope.ServiceProvider.GetRequiredService<IMailService>().SendAlarmEmail(
                    new MailRequest { Name = "EVEON", ToEmails = _options.EmailRecipients.ToList(), Subject = $"[EVEON] Critical alarm on {where}" },
                    new AlarmMail(alarm.ID, alarm.Severity.ToString(), alarm.Type.ToString(), alarm.Code, alarm.Description, alarm.Details,
                        alarm.ChargePointIdentity, alarm.StationName, alarm.EvseId, alarm.FirstSeenAt,
                        link.Length == 0 ? null : $"{link}/dashboard/alarm-management?alarm={alarm.ID}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not notify critical alarm {AlarmId}", alarm.ID);
            }
        });
    }

    /// <summary>What the critical alarm email shows.</summary>
    public sealed record AlarmMail(long Id, string Severity, string Type, string Code, string Description, string? Details,
        string ChargePoint, string? Station, int? EvseId, DateTime FirstSeenAt, string? Link);
}
