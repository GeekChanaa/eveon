using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Audit
{
    /// <summary>
    /// Writes an AuditLog row for every insert / update / delete of the entities that matter
    /// for security and money. Rows are captured before the save (old values are still known)
    /// and written right after it in the same context, once generated keys exist.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<Type> AuditedTypes = new()
        {
            typeof(User), typeof(Role), typeof(Permission), typeof(RolePermission), typeof(Partner),
            typeof(ChargePoint), typeof(ChargingStation), typeof(Connector), typeof(Card), typeof(Order), typeof(DebitCard)
        };

        // Operational noise that would flood the log (status is pushed by the chargers themselves).
        private static readonly Dictionary<Type, HashSet<string>> IgnoredProperties = new()
        {
            [typeof(ChargePoint)] = new() { nameof(ChargePoint.Status), nameof(ChargePoint.FirmwareVersion) },
            // Payment-provider credential: never copied into the audit trail.
            [typeof(DebitCard)] = new() { nameof(DebitCard.ProviderToken) },
        };

        private static readonly HashSet<string> AlwaysIgnored = new() { nameof(IEntity.CreatedAt), nameof(IEntity.UpdatedAt) };

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;
        private List<PendingAudit>? _pending;
        private bool _writing;

        private sealed record PendingAudit(EntityEntry Entry, string Action, Dictionary<string, object?> Changes);

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor, ILogger<AuditSaveChangesInterceptor> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_writing && eventData.Context != null)
                await CaptureAsync(eventData.Context, cancellationToken);
            return result;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (!_writing && eventData.Context != null)
                CaptureAsync(eventData.Context, CancellationToken.None, sync: true).GetAwaiter().GetResult();
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (!_writing && eventData.Context != null)
                await FlushAsync(eventData.Context, cancellationToken);
            return result;
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (!_writing && eventData.Context != null)
                FlushAsync(eventData.Context, CancellationToken.None, sync: true).GetAwaiter().GetResult();
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            if (!_writing) _pending = null;
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!_writing) _pending = null;
            return Task.CompletedTask;
        }

        private async Task CaptureAsync(DbContext context, CancellationToken cancellationToken, bool sync = false)
        {
            _pending = null;
            try
            {
                var entries = context.ChangeTracker.Entries()
                    .Where(e => AuditedTypes.Contains(e.Metadata.ClrType) &&
                                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                    .ToList();

                foreach (var entry in entries)
                {
                    var ignored = IgnoredProperties.TryGetValue(entry.Metadata.ClrType, out var set) ? set : null;
                    bool Include(PropertyEntry p) =>
                        !AlwaysIgnored.Contains(p.Metadata.Name) && ignored?.Contains(p.Metadata.Name) != true &&
                        !AuditSensitiveProperties.IsSensitive(p.Metadata.Name);

                    var changes = new Dictionary<string, object?>();
                    string action;

                    if (entry.State == EntityState.Added)
                    {
                        action = "Created";
                        foreach (var p in entry.Properties.Where(Include))
                            changes[p.Metadata.Name] = new { @new = Format(p.CurrentValue) };
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        action = "Deleted";
                    }
                    else
                    {
                        // Entities attached through Update() report every column as modified with
                        // original == current, so the real old values come from the database.
                        var modified = entry.Properties.Where(p => p.IsModified).ToList();
                        PropertyValues? databaseValues = null;
                        if (modified.Any(p => Equals(p.OriginalValue, p.CurrentValue)))
                            databaseValues = sync ? entry.GetDatabaseValues() : await entry.GetDatabaseValuesAsync(cancellationToken);

                        foreach (var p in modified)
                        {
                            var oldValue = databaseValues != null ? databaseValues[p.Metadata] : p.OriginalValue;
                            if (Equals(oldValue, p.CurrentValue)) continue;
                            if (p.Metadata.Name == nameof(IEntity.IsDeleted)) continue;
                            if (!Include(p))
                            {
                                // Record that a secret changed, never its value.
                                if (AuditSensitiveProperties.IsSensitive(p.Metadata.Name))
                                    changes[p.Metadata.Name] = new { changed = true };
                                continue;
                            }
                            changes[p.Metadata.Name] = new { old = Format(oldValue), @new = Format(p.CurrentValue) };
                        }

                        var softDeleted = entry.Entity is IEntity { IsDeleted: true } &&
                            (databaseValues != null
                                ? databaseValues[nameof(IEntity.IsDeleted)] is false
                                : entry.Property(nameof(IEntity.IsDeleted)).OriginalValue is false);
                        action = softDeleted ? "Deleted" : "Updated";
                        if (!softDeleted && changes.Count == 0) continue;
                    }

                    (_pending ??= new()).Add(new PendingAudit(entry, action, changes));
                }
            }
            catch (Exception ex)
            {
                _pending = null;
                _logger.LogError(ex, "Audit capture failed");
            }
        }

        private async Task FlushAsync(DbContext context, CancellationToken cancellationToken, bool sync = false)
        {
            var pending = _pending;
            _pending = null;
            if (pending == null || pending.Count == 0) return;

            var actor = AuditActor.From(_httpContextAccessor.HttpContext);
            var now = DateTime.UtcNow;
            var logs = pending.Select(p => new AuditLog
            {
                OccurredAt = now,
                UserID = actor.UserID,
                UserEmail = actor.Email,
                Role = actor.Role,
                Action = p.Action,
                EntityType = p.Entry.Metadata.ClrType.Name,
                EntityID = AuditActor.Truncate(KeyOf(p.Entry), 64),
                ChangesJson = p.Changes.Count == 0 ? null : JsonSerializer.Serialize(p.Changes),
                IpAddress = actor.IpAddress,
                CorrelationID = actor.CorrelationID
            }).ToList();

            _writing = true;
            try
            {
                context.Set<AuditLog>().AddRange(logs);
                if (sync) context.SaveChanges();
                else await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // The business change is already committed; never fail the caller over its audit row.
                foreach (var log in logs) context.Entry(log).State = EntityState.Detached;
                _logger.LogError(ex, "Audit log write failed");
            }
            finally
            {
                _writing = false;
            }
        }

        private static string? KeyOf(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) return null;
            return string.Join(",", key.Properties.Select(p => Convert.ToString(entry.Property(p.Name).CurrentValue, CultureInfo.InvariantCulture)));
        }

        private static object? Format(object? value) => value switch
        {
            null => null,
            DateTime d => d.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Enum e => e.ToString(),
            byte[] => "[binary]",
            _ => value
        };
    }
}
