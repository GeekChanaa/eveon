using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models.Ocpp201;

namespace VoltaXApi.OCPP.Services
{
    public static class LogUploadPurposes
    {
        public const string DiagnosticsLog = "DiagnosticsLog";
        public const string SecurityLog = "SecurityLog";
        /// <summary>OCPP 1.6 GetDiagnostics.</summary>
        public const string Diagnostics = "Diagnostics";
    }

    public sealed record LogUploadUrl(string Url, int TicketId, DateTime ExpiresAt);

    /// <summary>
    /// Creates the one-time URL a charger uploads a log / diagnostics file to (GetLog remoteLocation, 1.6 GetDiagnostics location):
    /// {Ocpp:PublicBaseUrl}/ocpp/logs/upload/{token}. Only the SHA-256 of the 256-bit token is stored.
    /// </summary>
    public interface ILogUploadUrlFactory
    {
        Task<LogUploadUrl> CreateAsync(string chargePointId, int requestId, string purpose, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records the upload status shown to the admin (command answer, LogStatusNotification, 1.6 DiagnosticsStatusNotification)
        /// on the ticket of <paramref name="requestId"/>, or on the charger's latest ticket when it is null. No-op without a ticket.
        /// </summary>
        Task RecordStatusAsync(string chargePointId, int? requestId, string status, string? announcedFileName = null, CancellationToken cancellationToken = default);
    }

    public enum LogUploadRejection { None, UnknownTicket, AlreadyUsed, Expired, TooLarge, Empty }

    /// <summary>Ticket rules, pure.</summary>
    public static class LogUploadTickets
    {
        public static string NewToken() => Base64Url(RandomNumberGenerator.GetBytes(32));

        public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

        public static LogUploadRejection Validate(LogUploadTicket? ticket, DateTime nowUtc) =>
            ticket == null ? LogUploadRejection.UnknownTicket
            : ticket.UsedAt != null ? LogUploadRejection.AlreadyUsed
            : ticket.ExpiresAt <= nowUtc ? LogUploadRejection.Expired
            : LogUploadRejection.None;

        /// <summary>A path segment safe on every file system (charger ids are free text).</summary>
        public static string SafeSegment(string value)
        {
            var safe = Regex.Replace(value ?? "", "[^A-Za-z0-9._-]", "_").Trim('.');
            return string.IsNullOrEmpty(safe) ? "_" : safe.Length > 64 ? safe[..64] : safe;
        }

        public static string SafeExtension(string? fileName)
        {
            var ext = Path.GetExtension(fileName ?? "").TrimStart('.').ToLowerInvariant();
            return Regex.IsMatch(ext, "^[a-z0-9]{1,10}$") ? ext : "log";
        }

        public static string SafeFileName(string? fileName, string fallback)
        {
            var name = Path.GetFileName(fileName ?? "").Trim();
            name = Regex.Replace(name, @"[^\w.\- ()\[\]]", "_");
            return string.IsNullOrWhiteSpace(name) ? fallback : name.Length > 255 ? name[^255..] : name;
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public sealed class LogTooLargeException : Exception
    {
        public LogTooLargeException(long maxBytes) : base($"The upload exceeds {maxBytes} bytes.") { }
    }

    public sealed record StoredLogFile(string RelativePath, long SizeBytes, string Sha256);

    /// <summary>Writes uploaded logs under the charger-logs root (outside wwwroot) with a size limit and a SHA-256.</summary>
    public sealed class ChargerLogStorage
    {
        public ChargerLogStorage(string root) => Root = Path.GetFullPath(root);

        public string Root { get; }

        public static ChargerLogStorage FromConfiguration(IConfiguration configuration, IWebHostEnvironment environment) =>
            new(configuration["Ocpp:LogUploadRoot"] is { Length: > 0 } root
                ? Path.Combine(environment.ContentRootPath, root)
                : Path.Combine(environment.ContentRootPath, "App_Data", "charger-logs"));

        public string FullPath(string relativePath)
        {
            var full = Path.GetFullPath(Path.Combine(Root, relativePath));
            if (!full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Log path escapes the storage root.");
            return full;
        }

        public async Task<StoredLogFile> SaveAsync(Stream content, string chargePointId, string extension, long maxBytes, CancellationToken cancellationToken)
        {
            var relative = Path.Combine(LogUploadTickets.SafeSegment(chargePointId), $"{Guid.NewGuid():N}.{extension}");
            var full = FullPath(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long size = 0;
            try
            {
                await using (var file = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        size += read;
                        if (size > maxBytes) throw new LogTooLargeException(maxBytes);
                        sha.AppendData(buffer, 0, read);
                        await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                }
                return new StoredLogFile(relative, size, Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant());
            }
            catch
            {
                File.Delete(full);
                throw;
            }
        }
    }

    public class LogUploadUrlFactory : ILogUploadUrlFactory
    {
        private readonly VoltaXApiDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LogUploadUrlFactory> _logger;

        public LogUploadUrlFactory(VoltaXApiDbContext db, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<LogUploadUrlFactory> logger)
        {
            _db = db;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<LogUploadUrl> CreateAsync(string chargePointId, int requestId, string purpose, CancellationToken cancellationToken = default)
        {
            var baseUrl = PublicBaseUrl();
            var token = LogUploadTickets.NewToken();
            var now = DateTime.UtcNow;
            var lifetime = TimeSpan.FromMinutes(Math.Max(1, _configuration.GetValue("Ocpp:LogUploadTicketMinutes", 120)));
            var user = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ticket = new LogUploadTicket
            {
                TokenHash = LogUploadTickets.Hash(token),
                ChargePointID = chargePointId,
                RequestId = requestId,
                Purpose = purpose,
                CreatedAt = now,
                ExpiresAt = now + lifetime,
                CreatedByUserID = int.TryParse(user, out var userId) ? userId : null
            };
            _db.LogUploadTickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Log upload ticket {TicketId} created for {ChargePointId} request {RequestId} ({Purpose}), expires {ExpiresAt:o}",
                ticket.ID, chargePointId, requestId, purpose, ticket.ExpiresAt);
            return new LogUploadUrl($"{baseUrl}/ocpp/logs/upload/{token}", ticket.ID, ticket.ExpiresAt);
        }

        public async Task RecordStatusAsync(string chargePointId, int? requestId, string status, string? announcedFileName = null, CancellationToken cancellationToken = default)
        {
            var query = _db.LogUploadTickets.Where(t => t.ChargePointID == chargePointId);
            if (requestId.HasValue) query = query.Where(t => t.RequestId == requestId.Value);
            var ticket = await query.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (ticket == null)
            {
                _logger.LogDebug("No log upload ticket of {ChargePointId} for request {RequestId}; status {Status} not recorded", chargePointId, requestId, status);
                return;
            }
            ticket.Status = status.Length > 32 ? status[..32] : status;
            ticket.StatusAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(announcedFileName))
                ticket.AnnouncedFileName = announcedFileName.Length > 255 ? announcedFileName[..255] : announcedFileName;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private string PublicBaseUrl()
        {
            var configured = _configuration["Ocpp:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null || !request.Host.HasValue)
                throw new InvalidOperationException("Ocpp:PublicBaseUrl is not configured and there is no request to derive it from.");
            return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
        }
    }
}
