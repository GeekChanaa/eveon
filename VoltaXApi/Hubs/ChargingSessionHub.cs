using Microsoft.AspNetCore.SignalR;

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
    public class ChargingSessionHub : Hub
    {
        private readonly ILogger<ChargingSessionHub> _logger;

        public ChargingSessionHub(ILogger<ChargingSessionHub> logger)
        {
            _logger = logger;
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

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("ChargingSessionHub => Client connected: {ConnectionId}", Context.ConnectionId);
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
