using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Services.Audit;
using VoltaXApi.Services.Gdpr;

namespace VoltaXApi.Controllers
{
    public class DeleteAccountDto
    {
        public string? Password { get; set; }
    }

    // Self-service GDPR endpoints. Every action works on the caller's own account only.
    [Route("api/me")]
    [ApiController]
    public class MeController : ControllerBase
    {
        private readonly IAccountDeletionService _deletion;
        private readonly IUserDataExportService _exports;
        private readonly IAuditLogger _audit;
        private readonly VoltaXApiDbContext _db;

        public MeController(IAccountDeletionService deletion, IUserDataExportService exports, IAuditLogger audit, VoltaXApiDbContext db)
        {
            _deletion = deletion;
            _exports = exports;
            _audit = audit;
            _db = db;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        [HttpDelete]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountDto dto)
        {
            if (CurrentUserId is not int userId) return Unauthorized();
            try
            {
                var request = await _deletion.RequestAsync(userId, dto?.Password, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Accepted(new { scheduledFor = request.ScheduledFor });
            }
            catch (AccountDeletionException ex) when (ex.InvalidCredentials)
            {
                return BadRequest(new { error = "The password is incorrect." });
            }
            catch (AccountDeletionException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        // Needs both the signed-in session and the single-use token from the email.
        [HttpGet("data-export/download")]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> DownloadMyDataExport([FromQuery] string token)
        {
            if (CurrentUserId is not int userId) return Unauthorized();
            if (string.IsNullOrWhiteSpace(token) || token.Length > 128) return NotFound();

            var hash = GdprTokens.Hash(token);
            var now = DateTime.UtcNow;
            var request = await _db.UserInfoDownloadRequests.SingleOrDefaultAsync(r =>
                r.UserID == userId && r.DownloadTokenHash == hash && r.Status == DownloadRequestStatusEnum.Completed &&
                r.DownloadedAt == null && r.ExpiresAt > now);
            var path = request == null ? null : _exports.ResolvePath(request.ExportFileName);
            if (request == null || path == null) return NotFound();

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            request.DownloadedAt = now;
            request.DownloadTokenHash = null;
            request.ExportFileName = null;
            await _db.SaveChangesAsync();
            await _audit.LogAsync("DataExportDownloaded", nameof(UserInfoDownloadRequest), request.ID.ToString());

            return File(stream, "application/zip", $"eveon-data-{now:yyyyMMdd}.zip");
        }
    }
}
