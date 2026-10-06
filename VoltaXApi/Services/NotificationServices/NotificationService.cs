using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Hubs;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class NotificationService : INotificationService
    {
        public const string PushEvent = "NotificationReceived";

        // Own scope: saving here must never flush the caller's pending changes.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IServiceScopeFactory scopeFactory, IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyDashboardAsync(DashboardNotification notification, string permission, string url, int? partnerId = null, string? partnerUrl = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var type = await GetOrCreateType(db, notification.Type, partnerId != null);
                var now = DateTime.UtcNow;

                var recipients = await db.Users.AsNoTracking()
                    .Where(u => !u.IsDeleted && (u.SuspendedAt == null || u.SuspendedAt <= now) && u.Role != null && !u.Role.IsDeleted)
                    .Where(u => u.Role.Name == "Admin"
                        || u.Role.RolePermissions.Any(rp => !rp.IsDeleted && !rp.Permission.IsDeleted && rp.Scope == PermissionScope.Global && rp.Permission.Name == "AccessDashboard")
                           && u.Role.RolePermissions.Any(rp => !rp.IsDeleted && !rp.Permission.IsDeleted && rp.Scope == PermissionScope.Global && rp.Permission.Name == permission)
                        || partnerId != null && u.Role.Name == "Partner" && u.PartnerID == partnerId)
                    // Users who switched this type off in their notification settings.
                    .Where(u => !db.NotificationSettings.Any(s => s.UserID == u.ID && s.NotificationTypeID == type.ID && !s.IsDeleted && !s.Active))
                    .Select(u => new { u.ID, IsPartner = u.Role.Name == "Partner" })
                    .ToListAsync();
                if (recipients.Count == 0) return;

                var created = recipients.Select(r => new Notification
                {
                    NotificationTypeID = type.ID,
                    ReceiverID = r.ID,
                    Read = false,
                    Url = r.IsPartner ? partnerUrl ?? "/partner-dashboard" : url,
                    Action = notification.Action,
                    ActionOn = notification.ActionOn,
                    Description = notification.Description,
                    Urgent = notification.Urgent
                }).ToList();
                db.Notifications.AddRange(created);
                await db.SaveChangesAsync();

                foreach (var n in created)
                {
                    var dto = new NotificationDto
                    {
                        ID = n.ID, Type = type.Name, Action = n.Action, Description = n.Description,
                        Url = n.Url, Urgent = n.Urgent, Read = false, CreatedAt = n.CreatedAt
                    };
                    await _hubContext.Clients.Group(NotificationHub.GetUserGroupName(n.ReceiverID!.Value)).SendAsync(PushEvent, dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotifyDashboardAsync => Failed to send {Action} ({ActionOn})", notification.Action, notification.ActionOn);
            }
        }

        private static async Task<NotificationType> GetOrCreateType(VoltaXApiDbContext db, string name, bool forPartners)
        {
            var type = await db.NotificationTypes.FirstOrDefaultAsync(t => t.Name == name && !t.IsDeleted);
            if (type != null) return type;
            type = new NotificationType { Name = name, Description = name + " Notifications", ForAdmins = true, ForPartners = forPartners, ForCustomers = false };
            db.NotificationTypes.Add(type);
            await db.SaveChangesAsync();
            return type;
        }
    }
}
