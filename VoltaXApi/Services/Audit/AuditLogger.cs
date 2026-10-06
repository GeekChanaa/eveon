using System.Text.Json;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Audit
{
    public interface IAuditLogger
    {
        /// <summary>
        /// Records an action that is not an entity change (remote OCPP command, login,
        /// 2FA change...). Never throws: a failed audit write must not break the caller.
        /// </summary>
        Task LogAsync(string action, string? entityType = null, string? entityId = null, object? details = null);

        /// <summary>Same, for callers that know the user but run outside an authenticated request (e.g. login).</summary>
        Task LogForUserAsync(string action, int? userId, string? userEmail, string? entityType = null, string? entityId = null, object? details = null);
    }

    public sealed class AuditLogger : IAuditLogger
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor, ILogger<AuditLogger> logger)
        {
            _scopeFactory = scopeFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public Task LogAsync(string action, string? entityType = null, string? entityId = null, object? details = null)
            => Write(action, null, null, entityType, entityId, details);

        public Task LogForUserAsync(string action, int? userId, string? userEmail, string? entityType = null, string? entityId = null, object? details = null)
            => Write(action, userId, userEmail, entityType, entityId, details);

        private async Task Write(string action, int? userId, string? userEmail, string? entityType, string? entityId, object? details)
        {
            try
            {
                var actor = AuditActor.From(_httpContextAccessor.HttpContext);
                var log = new AuditLog
                {
                    OccurredAt = DateTime.UtcNow,
                    UserID = userId ?? actor.UserID,
                    UserEmail = AuditActor.Truncate(userEmail ?? actor.Email, 256),
                    Role = actor.Role,
                    Action = AuditActor.Truncate(action, 64)!,
                    EntityType = AuditActor.Truncate(entityType, 128),
                    EntityID = AuditActor.Truncate(entityId, 64),
                    ChangesJson = details == null ? null : details as string ?? JsonSerializer.Serialize(details),
                    IpAddress = actor.IpAddress,
                    CorrelationID = actor.CorrelationID
                };

                // Own scope and context: the caller's pending changes must not be flushed by an audit write.
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                db.AuditLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log write failed for action {Action}", action);
            }
        }
    }
}
