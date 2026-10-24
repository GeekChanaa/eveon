using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Authorization;

namespace VoltaXApi.Hubs
{
    /// <summary>
    /// SignalR hub for real-time charging session updates.
    /// 
    /// Clients can subscribe to a specific charging session by calling JoinSession(sessionId).
    /// They will receive "SessionUpdate" messages with ChargingSessionUpdateDto payloads
    /// containing live energy, cost, duration, and balance information.
    /// 
    /// Connection URL: /chargingSessionHub
    /// 
    /// Client methods to call:
    ///   - JoinSession(int sessionId)        — subscribe to a session's updates
    ///   - LeaveSession(int sessionId)        — unsubscribe from a session
    ///   - JoinUserSessions(int userId)       — subscribe to all sessions for a user
    ///   - LeaveUserSessions(int userId)      — unsubscribe from a user's sessions
    /// 
    /// Server-to-client events:
    ///   - SessionUpdate(ChargingSessionUpdateDto) — real-time session data
    ///   - ReceiveMessage(string message)          — informational messages
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ChargingSessionHub : Hub
    {
        private readonly ILogger<ChargingSessionHub> _logger;
        private readonly AccessService _accessService;

        public ChargingSessionHub(ILogger<ChargingSessionHub> logger, AccessService accessService)
        {
            _logger = logger;
            _accessService = accessService;
        }

        /// <summary>
        /// Subscribe to real-time updates for a specific charging session.
        /// </summary>
        public async Task JoinSession(int sessionId)
        {
            string groupName = GetSessionGroupName(sessionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined session group {GroupName}", Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("ReceiveMessage", $"Subscribed to charging session {sessionId}");
        }

        /// <summary>
        /// Unsubscribe from a specific charging session's updates.
        /// </summary>
        public async Task LeaveSession(int sessionId)
        {
            string groupName = GetSessionGroupName(sessionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left session group {GroupName}", Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("ReceiveMessage", $"Unsubscribed from charging session {sessionId}");
        }

        /// <summary>
        /// Subscribe to all charging session updates for a specific user.
        /// </summary>
        public async Task JoinUserSessions(int userId)
        {
            string groupName = GetUserGroupName(userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined user group {GroupName}", Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("ReceiveMessage", $"Subscribed to all sessions for user {userId}");
        }

        /// <summary>
        /// Unsubscribe from a user's charging session updates.
        /// </summary>
        public async Task LeaveUserSessions(int userId)
        {
            string groupName = GetUserGroupName(userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left user group {GroupName}", Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("ReceiveMessage", $"Unsubscribed from user {userId} sessions");
        }

        /// <summary>
        /// Every connection follows its own user's sessions: the customer portal's live widget needs no hub call,
        /// and nobody can receive another user's updates this way (the group comes from the token, not the client).
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("ChargingSessionHub => Client connected: {ConnectionId}", Context.ConnectionId);
            // AuthorizedHubFilter already rejected unauthenticated connections.
            var access = Context.User == null ? null : await _accessService.Resolve(Context.User);
            if (access != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(access.User.ID));
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("ChargingSessionHub => Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Get the SignalR group name for a charging session.
        /// </summary>
        public static string GetSessionGroupName(int sessionId) => $"charging-session-{sessionId}";

        /// <summary>
        /// Get the SignalR group name for a user's sessions.
        /// </summary>
        public static string GetUserGroupName(int userId) => $"user-sessions-{userId}";
    }
}
