using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Ocpi;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.Ocpi.Services;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Controllers
{
    public class OcpiPartnerCreateDto
    {
        public string Name { get; set; } = "";
    }

    public class OcpiPartnerRegisterDto
    {
        public string Name { get; set; } = "";
        public string VersionsUrl { get; set; } = "";
        public string TokenA { get; set; } = "";
    }

    // Roaming partner management (dashboard). Not mapped in EndpointPermissions: admin only.
    [Route("api/ocpi")]
    [ApiController]
    public class OcpiPartnerController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;
        private readonly OcpiCredentialsService _credentials;
        private readonly IAuditLogger _audit;
        private readonly OcpiOptions _options;

        public OcpiPartnerController(VoltaXApiDbContext db, OcpiCredentialsService credentials, IAuditLogger audit, IOptions<OcpiOptions> options)
        {
            _db = db;
            _credentials = credentials;
            _audit = audit;
            _options = options.Value;
        }

        [HttpGet("settings")]
        public IActionResult Settings() => Ok(new
        {
            enabled = _options.Enabled,
            countryCode = _options.CountryCode,
            partyId = _options.PartyId,
            versionsUrl = _options.VersionsUrl
        });

        [HttpGet("partners")]
        public async Task<IActionResult> GetPartners()
        {
            var parties = await _db.OcpiParties.AsNoTracking().OrderByDescending(p => p.ID).ToListAsync();
            var failures = await _db.OcpiOutbox.AsNoTracking().Where(m => m.Status == OcpiOutboxStatus.Failed)
                .GroupBy(m => m.OcpiPartyID).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count);
            var pending = await _db.OcpiOutbox.AsNoTracking().Where(m => m.Status == OcpiOutboxStatus.Pending)
                .GroupBy(m => m.OcpiPartyID).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count);
            var tokens = await _db.OcpiTokens.AsNoTracking().GroupBy(t => t.OcpiPartyID)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count);
            return Ok(parties.Select(p => new
            {
                p.ID,
                p.Name,
                p.CountryCode,
                p.PartyId,
                p.Role,
                Status = p.Status.ToString(),
                p.VersionsUrl,
                p.Version,
                p.LastError,
                p.RegisteredAt,
                p.CreatedAt,
                p.UpdatedAt,
                AwaitingTokenA = p.TokenAHash != null,
                Endpoints = OcpiPartyService.Endpoints(p),
                FailedMessages = failures.GetValueOrDefault(p.ID),
                PendingMessages = pending.GetValueOrDefault(p.ID),
                Tokens = tokens.GetValueOrDefault(p.ID)
            }));
        }

        // The token is shown once; only its hash is stored.
        [HttpPost("partners/token-a")]
        public async Task<IActionResult> CreateTokenA([FromBody] OcpiPartnerCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100) return BadRequest("A partner name is required.");
            var (party, token) = await _credentials.CreateTokenAAsync(dto.Name);
            await _audit.LogAsync("Ocpi.TokenACreated", "OcpiParty", party.ID.ToString());
            return Ok(new { party.ID, tokenA = token, versionsUrl = _options.VersionsUrl });
        }

        [HttpPost("partners/register")]
        public async Task<IActionResult> Register([FromBody] OcpiPartnerRegisterDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100 || string.IsNullOrWhiteSpace(dto.TokenA) || dto.TokenA.Length > 64
                || !Uri.TryCreate(dto.VersionsUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
                return BadRequest("Name, versions URL and token A are required.");
            try
            {
                var party = await _credentials.RegisterWithPartnerAsync(dto.Name, dto.VersionsUrl, dto.TokenA.Trim(), cancellationToken);
                await _audit.LogAsync("Ocpi.Registered", "OcpiParty", party.ID.ToString());
                return Ok(new { party.ID, party.CountryCode, party.PartyId, Status = party.Status.ToString() });
            }
            catch (OcpiRegistrationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("partners/{id:int}/suspend")]
        public Task<IActionResult> Suspend(int id) => SetStatus(id, OcpiPartyStatus.Registered, OcpiPartyStatus.Suspended, "Ocpi.Suspended");

        [HttpPost("partners/{id:int}/resume")]
        public Task<IActionResult> Resume(int id) => SetStatus(id, OcpiPartyStatus.Suspended, OcpiPartyStatus.Registered, "Ocpi.Resumed");

        [HttpPost("partners/{id:int}/refresh-endpoints")]
        public async Task<IActionResult> RefreshEndpoints(int id, CancellationToken cancellationToken)
        {
            var party = await _db.OcpiParties.FirstOrDefaultAsync(p => p.ID == id);
            if (party == null) return NotFound();
            try
            {
                await _credentials.RefreshEndpointsAsync(party, cancellationToken);
                return Ok();
            }
            catch (OcpiRegistrationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Tells the partner (DELETE on its credentials endpoint) and revokes both tokens.
        [HttpDelete("partners/{id:int}")]
        public async Task<IActionResult> Unregister(int id, CancellationToken cancellationToken)
        {
            var party = await _db.OcpiParties.FirstOrDefaultAsync(p => p.ID == id);
            if (party == null) return NotFound();
            await _credentials.UnregisterAsync(party, notifyPartner: true, cancellationToken);
            await _audit.LogAsync("Ocpi.Unregistered", "OcpiParty", id.ToString());
            return Ok();
        }

        [HttpGet("outbox")]
        public async Task<IActionResult> Outbox([FromQuery] int? partyId, [FromQuery] string status = "Failed", [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            if (!Enum.TryParse<OcpiOutboxStatus>(status, true, out var parsed)) return BadRequest("Unknown status.");
            var query = _db.OcpiOutbox.AsNoTracking().Where(m => m.Status == parsed);
            if (partyId.HasValue) query = query.Where(m => m.OcpiPartyID == partyId);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(m => m.ID).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new { m.ID, m.OcpiPartyID, PartyName = m.OcpiParty!.Name, m.Module, m.Method, m.Url, m.Attempts, m.LastError, m.CreatedAt, m.NextAttemptAt, m.SentAt })
                .ToListAsync();
            return Ok(new { items, totalCount = total, page, pageSize });
        }

        [HttpPost("outbox/{id:long}/retry")]
        public async Task<IActionResult> Retry(long id)
        {
            var message = await _db.OcpiOutbox.FirstOrDefaultAsync(m => m.ID == id && m.Status == OcpiOutboxStatus.Failed);
            if (message == null) return NotFound();
            message.Status = OcpiOutboxStatus.Pending;
            message.Attempts = 0;
            message.NextAttemptAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }

        private async Task<IActionResult> SetStatus(int id, OcpiPartyStatus from, OcpiPartyStatus to, string action)
        {
            var party = await _db.OcpiParties.FirstOrDefaultAsync(p => p.ID == id);
            if (party == null) return NotFound();
            if (party.Status != from) return BadRequest($"Partner is {party.Status}.");
            party.Status = to;
            party.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(action, "OcpiParty", id.ToString());
            return Ok();
        }
    }
}
