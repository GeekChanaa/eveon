using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Authorization;

namespace VoltaXApi.Hubs
{
    /// <summary>
    /// Push-only SignalR hub for dashboard notifications.
    ///
    /// Connection URL: /notificationHub
    /// Each connection joins its own user group on connect; clients call no hub methods.
    ///
    /// Server-to-client events:
    ///   - NotificationReceived(NotificationDto) — a new notification for the connected user
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class NotificationHub : Hub
    {
        private readonly AccessService _accessService;

        public NotificationHub(AccessService accessService)
        {
            _accessService = accessService;
        }

        public override async Task OnConnectedAsync()
        {
            // AuthorizedHubFilter already rejected unauthenticated connections.
            var access = await _accessService.Resolve(Context.User!);
            if (access != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(access.User.ID));
            await base.OnConnectedAsync();
        }

        public static string GetUserGroupName(int userId) => $"notifications-user-{userId}";
    }
}
