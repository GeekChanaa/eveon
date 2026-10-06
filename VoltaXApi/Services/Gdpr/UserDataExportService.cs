using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Gdpr
{
    public interface IUserDataExportService
    {
        /// <summary>Builds the archive and returns its file name inside <see cref="ExportDirectory"/>.</summary>
        Task<string> BuildArchiveAsync(int userId, CancellationToken cancellationToken = default);
        string ExportDirectory { get; }
        string? ResolvePath(string? fileName);
        void DeleteArchive(string? fileName);
    }

    public static class GdprTokens
    {
        public static string NewToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    public class UserDataExportService : IUserDataExportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly VoltaXApiDbContext _db;

        public UserDataExportService(VoltaXApiDbContext db, IConfiguration configuration, IWebHostEnvironment env)
        {
            _db = db;
            // Private folder outside wwwroot: archives are only served through the token endpoint.
            var configured = configuration["Gdpr:ExportPath"];
            var path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(env.ContentRootPath, "App_Data", "gdpr-exports")
                : Path.GetFullPath(configured);
            if (!string.IsNullOrEmpty(env.WebRootPath) && Path.GetFullPath(path).StartsWith(Path.GetFullPath(env.WebRootPath), StringComparison.OrdinalIgnoreCase))
                path = Path.Combine(env.ContentRootPath, "App_Data", "gdpr-exports");
            ExportDirectory = path;
        }

        public string ExportDirectory { get; }

        public string? ResolvePath(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName)) return null;
            var path = Path.Combine(ExportDirectory, fileName);
            return File.Exists(path) ? path : null;
        }

        public void DeleteArchive(string? fileName)
        {
            var path = ResolvePath(fileName);
            if (path != null) File.Delete(path);
        }

        public async Task<string> BuildArchiveAsync(int userId, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(ExportDirectory);
            var fileName = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant() + ".zip";
            var finalPath = Path.Combine(ExportDirectory, fileName);
            var tempPath = finalPath + ".tmp";

            var files = await CollectAsync(userId, cancellationToken);
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (var (name, data) in files)
                {
                    var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await JsonSerializer.SerializeAsync(entryStream, data, JsonOptions, cancellationToken);
                }
            }
            File.Move(tempPath, finalPath);
            return fileName;
        }

        private async Task<List<(string Name, object Data)>> CollectAsync(int userId, CancellationToken ct)
        {
            var files = new List<(string, object)>();

            var profile = await _db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => u.ID == userId)
                .Select(u => new
                {
                    u.ID, u.FirstName, u.LastName, u.Email, u.Phone, u.Gender, u.City, u.Birthday,
                    ElectricVehicleModel = u.ElectricVehicleModel != null ? u.ElectricVehicleModel.Make + " " + u.ElectricVehicleModel.Model : null,
                    Role = u.Role.Name, u.AuthProvider, GoogleLinked = u.GoogleId != null, u.ExternalPictureUrl,
                    u.IsEmailVerified, u.IsPhoneNumberVerified, u.SuspendedAt, u.SuspensionReason,
                    u.TermsAcceptedAt, u.TermsVersion, u.CreatedAt, u.UpdatedAt
                }).SingleAsync(ct);
            files.Add(("profile.json", profile));

            var cards = await _db.Cards.IgnoreQueryFilters().AsNoTracking().Where(c => c.UserID == userId)
                .Select(c => new { c.ID, c.CardNumber, c.CardType, c.ExpirationDate, c.Status, c.Balance, c.Blocked, c.IsDeleted, c.CreatedAt })
                .ToListAsync(ct);
            var cardIds = cards.Select(c => c.ID).ToList();
            files.Add(("cards.json", cards));

            files.Add(("card-history.json", await _db.CardChangeHistories.AsNoTracking().Where(h => cardIds.Contains(h.CardID))
                .OrderBy(h => h.ChangedAtUtc)
                .Select(h => new { h.CardID, h.ChangedAtUtc, h.PropertyName, h.OldValue, h.NewValue, h.Source })
                .ToListAsync(ct)));

            // Masked: never more than the last four digits, never the CVV.
            var debitCards = await _db.DebitCards.AsNoTracking().Where(d => d.UserID == userId)
                .Select(d => new { d.ID, d.Name, Brand = d.Brand.ToString(), LastFourDigits = d.Last4, d.ExpiryMonth, d.ExpiryYear, d.CreatedAt })
                .ToListAsync(ct);
            files.Add(("debit-cards.json", debitCards));

            files.Add(("charging-sessions.json", await _db.ChargingSessions.IgnoreQueryFilters().AsNoTracking().Where(s => s.UserID == userId)
                .OrderBy(s => s.StartDate)
                .Select(s => new
                {
                    s.ID, s.CardID, s.ConnectorID, ChargePoint = s.Connector != null && s.Connector.ChargePoint != null ? s.Connector.ChargePoint.ChargePointId : null,
                    s.StartDate, s.EndDate, s.EndIdleDate, s.ChargedMinutes, s.IdleMinutes, s.ChargedKwhs,
                    s.PricePerMinute, s.CostPerKwh, s.PricePerIdleMinute, s.StoppedReason, s.ChargingSessionStatus, s.CreatedAt
                }).ToListAsync(ct)));

            files.Add(("transactions.json", await _db.Transactions.IgnoreQueryFilters().AsNoTracking()
                .Where(t => t.ChargingSession != null && t.ChargingSession.UserID == userId)
                .OrderBy(t => t.StartTime)
                .Select(t => new { t.ID, t.ChargingSessionID, t.ConnectorID, t.StartTime, t.StopTime, t.MeterStart, t.MeterStop, t.Amount, t.Status, t.StopReason })
                .ToListAsync(ct)));

            files.Add(("orders.json", await _db.Orders.IgnoreQueryFilters().AsNoTracking().Where(o => cardIds.Contains(o.CardID))
                .OrderBy(o => o.RechargeDate)
                .Select(o => new { o.ID, o.CardID, o.Amount, o.Status, o.RechargeDate, o.CreatedAt })
                .ToListAsync(ct)));

            files.Add(("notifications.json", await _db.Notifications.AsNoTracking().Where(n => n.ReceiverID == userId)
                .OrderBy(n => n.CreatedAt)
                .Select(n => new { n.ID, Type = n.NotificationType != null ? n.NotificationType.Name : null, n.Description, n.Action, n.Url, n.Read, n.Urgent, n.CreatedAt })
                .ToListAsync(ct)));

            files.Add(("notification-settings.json", await _db.NotificationSettings.AsNoTracking().Where(n => n.UserID == userId)
                .Select(n => new { Type = n.NotificationType != null ? n.NotificationType.Name : null, n.Email, n.Active, n.Urgent, n.UpdatedAt })
                .ToListAsync(ct)));

            files.Add(("ratings.json", await _db.Ratings.AsNoTracking().Where(r => r.UserID == userId)
                .Select(r => new { r.ID, r.Score, r.Comment, r.Entity, r.EntityID, r.CreatedAt }).ToListAsync(ct)));

            files.Add(("comments.json", await _db.Comments.AsNoTracking().Where(c => c.UserID == userId)
                .Select(c => new { c.ID, c.Text, c.Rating, c.ChargingStationID, c.ChargePointID, c.ConnectorID, c.CommentTime }).ToListAsync(ct)));

            files.Add(("reports.json", new
            {
                ChargerReports = await _db.Reports.AsNoTracking().Where(r => r.UserID == userId)
                    .Select(r => new { r.ID, r.ReportType, r.ReportCategory, r.IssueDescription, r.Status, r.ChargePointID, r.ConnectorID, r.ReportDate, r.ResolvedDate })
                    .ToListAsync(ct),
                RatingReports = await _db.RatingReports.AsNoTracking().Where(r => r.UserID == userId)
                    .Select(r => new { r.ID, r.RatingID, r.ReportCategory, r.IssueDescription, r.Status, r.CreatedAt })
                    .ToListAsync(ct)
            }));

            // Session metadata only: token hashes are never exported.
            files.Add(("sign-ins.json", new
            {
                RefreshTokens = await _db.RefreshTokens.AsNoTracking().Where(t => t.UserID == userId)
                    .OrderBy(t => t.CreatedAt)
                    .Select(t => new { t.CreatedAt, t.ExpiresAt, t.RevokedAt, t.RevokedReason, t.CreatedByIp, t.UserAgent })
                    .ToListAsync(ct),
                Activity = await _db.AuditLogs.AsNoTracking()
                    .Where(a => a.UserID == userId)
                    .OrderBy(a => a.OccurredAt)
                    .Select(a => new { a.OccurredAt, a.Action, a.EntityType, a.IpAddress })
                    .ToListAsync(ct)
            }));

            files.Add(("data-requests.json", await _db.UserInfoDownloadRequests.AsNoTracking().Where(r => r.UserID == userId)
                .Select(r => new { r.ID, r.RequestTime, r.Status, r.CompletedAt }).ToListAsync(ct)));

            files.Add(("README.json", new
            {
                GeneratedAtUtc = DateTime.UtcNow,
                Description = "Copy of the personal data EVEON holds about this account. Payment card numbers are masked; passwords and tokens are never exported."
            }));

            return files;
        }
    }
}
