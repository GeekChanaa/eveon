using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;

namespace VoltaXApi.Controllers
{
    // Not mapped in EndpointPermissions on purpose: unmapped controllers are admin-only.
    [Route("api/audit-logs")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private const int MaxPageSize = 100;
        private readonly VoltaXApiDbContext _db;

        public AuditLogController(VoltaXApiDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
            [FromQuery] int? userId = null, [FromQuery] string? user = null,
            [FromQuery] string? entityType = null, [FromQuery] string? entityId = null,
            [FromQuery] string? action = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _db.AuditLogs.AsNoTracking();
            if (userId.HasValue) query = query.Where(a => a.UserID == userId);
            if (!string.IsNullOrWhiteSpace(user)) query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(user));
            if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
            if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityID == entityId);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value.ToUniversalTime());
            if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.ID)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return Ok(new { items, totalCount = total, page, pageSize });
        }

        [HttpGet("entity-types")]
        public async Task<ActionResult<List<string>>> GetEntityTypes() =>
            await _db.AuditLogs.AsNoTracking().Where(a => a.EntityType != null)
                .Select(a => a.EntityType!).Distinct().OrderBy(t => t).ToListAsync();
    }
}
