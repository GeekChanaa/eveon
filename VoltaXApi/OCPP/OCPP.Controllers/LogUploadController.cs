using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using VoltaXApi.Data;
using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers;

/// <summary>
/// Charger log files. <see cref="UploadChargerLog"/> is called by chargers (no JWT; authorized by the one-time ticket
/// token in the URL, see <see cref="ILogUploadUrlFactory"/>); every other action is admin-only (DashboardAccessFilter).
/// </summary>
[ApiController]
[Route("api/logs")]
public class LogUploadController : ControllerBase
{
    private readonly VoltaXApiDbContext _db;
    private readonly ChargerLogStorage _storage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogUploadController> _logger;

    public LogUploadController(VoltaXApiDbContext db, ChargerLogStorage storage, IConfiguration configuration, ILogger<LogUploadController> logger)
    {
        _db = db;
        _storage = storage;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>HTTP(S) upload of a GetLog / GetDiagnostics file: multipart/form-data (first file part) or the raw body (PUT or POST).</summary>
    [HttpPost("/ocpp/logs/upload/{token}")]
    [HttpPut("/ocpp/logs/upload/{token}")]
    [EnableRateLimiting("public")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadChargerLog(string token, CancellationToken cancellationToken)
    {
        var maxBytes = _configuration.GetValue<long>("Ocpp:LogUploadMaxBytes", 50L * 1024 * 1024);
        var now = DateTime.UtcNow;
        var hash = LogUploadTickets.Hash(token ?? "");
        var ticket = await _db.LogUploadTickets.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        var rejection = LogUploadTickets.Validate(ticket, now);
        if (rejection != LogUploadRejection.None)
        {
            _logger.LogWarning("Charger log upload refused ({Reason}) from {RemoteIp}, ticket {TicketId}", rejection, HttpContext.Connection.RemoteIpAddress, ticket?.ID);
            return rejection == LogUploadRejection.UnknownTicket ? NotFound() : StatusCode(StatusCodes.Status410Gone);
        }
        if (Request.ContentLength > maxBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = $"The file exceeds {maxBytes} bytes." });

        // Claim the ticket first: two concurrent uploads with the same URL cannot both succeed.
        var claimed = await _db.LogUploadTickets
            .Where(t => t.ID == ticket!.ID && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), cancellationToken);
        if (claimed == 0) return StatusCode(StatusCodes.Status410Gone);

        try
        {
            var (content, fileName, contentType) = await OpenUpload(cancellationToken);
            if (content == null) { await Release(ticket!.ID); return BadRequest(new { message = "No file in the request." }); }
            var fallbackName = $"{ticket!.ChargePointID}-{ticket.Purpose}-{ticket.RequestId}.log";
            fileName = LogUploadTickets.SafeFileName(fileName ?? ticket.AnnouncedFileName, fallbackName);
            var stored = await _storage.SaveAsync(content, ticket.ChargePointID, LogUploadTickets.SafeExtension(fileName), maxBytes, cancellationToken);
            if (stored.SizeBytes == 0)
            {
                System.IO.File.Delete(_storage.FullPath(stored.RelativePath));
                await Release(ticket.ID);
                return BadRequest(new { message = "The file is empty." });
            }
            var log = new UploadedChargerLog
            {
                LogUploadTicketID = ticket.ID,
                ChargePointID = ticket.ChargePointID,
                FileName = fileName,
                StoragePath = stored.RelativePath,
                ContentType = contentType?.Length > 100 ? contentType[..100] : contentType,
                SizeBytes = stored.SizeBytes,
                Sha256 = stored.Sha256,
                UploadedAt = DateTime.UtcNow
            };
            _db.UploadedChargerLogs.Add(log);
            await _db.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation("Charger log {LogId} uploaded by {ChargePointId} (ticket {TicketId}, {Size} bytes, sha256 {Sha256})",
                log.ID, log.ChargePointID, ticket.ID, log.SizeBytes, log.Sha256);
            return Ok(new { message = "Uploaded.", size = log.SizeBytes, sha256 = log.Sha256 });
        }
        catch (LogTooLargeException ex)
        {
            await Release(ticket!.ID);
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // A failed transfer leaves the URL usable for the charger's retries (until it expires).
            _logger.LogError(ex, "Charger log upload failed for ticket {TicketId}", ticket!.ID);
            await Release(ticket.ID);
            if (ex is OperationCanceledException or IOException or InvalidDataException) return BadRequest(new { message = "The upload was interrupted or malformed." });
            throw;
        }
    }

    /// <summary>Upload tickets of a charger (most recent first) with their uploaded files.</summary>
    [HttpGet("{chargePointID}")]
    public async Task<IActionResult> GetChargerLogs(string chargePointID, CancellationToken cancellationToken, int take = 50)
    {
        take = Math.Clamp(take, 1, 200);
        var tickets = await _db.LogUploadTickets.AsNoTracking()
            .Where(t => t.ChargePointID == chargePointID)
            .OrderByDescending(t => t.CreatedAt).Take(take)
            .Select(t => new
            {
                t.ID, t.RequestId, t.Purpose, t.CreatedAt, t.ExpiresAt, t.UsedAt, t.Status, t.StatusAt, t.AnnouncedFileName,
                Files = _db.UploadedChargerLogs.Where(l => l.LogUploadTicketID == t.ID)
                    .Select(l => new { l.ID, l.FileName, l.SizeBytes, l.Sha256, l.ContentType, l.UploadedAt }).ToList()
            })
            .ToListAsync(cancellationToken);
        return Ok(tickets);
    }

    [HttpGet("file/{id:int}")]
    public async Task<IActionResult> DownloadChargerLog(int id, CancellationToken cancellationToken)
    {
        var log = await _db.UploadedChargerLogs.AsNoTracking().SingleOrDefaultAsync(l => l.ID == id, cancellationToken);
        if (log == null) return NotFound();
        var path = _storage.FullPath(log.StoragePath);
        if (!System.IO.File.Exists(path))
        {
            _logger.LogWarning("Charger log {LogId} is missing from storage ({Path})", id, log.StoragePath);
            return NotFound();
        }
        return PhysicalFile(path, "application/octet-stream", log.FileName);
    }

    private async Task<(Stream? Content, string? FileName, string? ContentType)> OpenUpload(CancellationToken cancellationToken)
    {
        if (MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaType) &&
            mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if (string.IsNullOrEmpty(boundary)) throw new InvalidDataException("Missing multipart boundary.");
            var reader = new MultipartReader(boundary, Request.Body);
            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) && disposition.IsFileDisposition())
                {
                    var name = HeaderUtilities.RemoveQuotes(disposition.FileNameStar.HasValue ? disposition.FileNameStar : disposition.FileName).Value;
                    return (section.Body, name, section.ContentType);
                }
            }
            return (null, null, null);
        }

        string? rawName = null;
        if (ContentDispositionHeaderValue.TryParse(Request.Headers.ContentDisposition.ToString(), out var raw))
            rawName = HeaderUtilities.RemoveQuotes(raw.FileNameStar.HasValue ? raw.FileNameStar : raw.FileName).Value;
        return (Request.Body, rawName, Request.ContentType);
    }

    private Task Release(int ticketId) =>
        _db.LogUploadTickets.Where(t => t.ID == ticketId).ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, (DateTime?)null), CancellationToken.None);
}
