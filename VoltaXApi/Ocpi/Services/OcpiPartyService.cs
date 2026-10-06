using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;

namespace VoltaXApi.Ocpi.Services
{
    // Party tokens (protected at rest), declared endpoints and the persisted outbox.
    public class OcpiPartyService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly IDataProtector _protector;
        private readonly OcpiOptions _options;

        public OcpiPartyService(VoltaXApiDbContext db, IDataProtectionProvider dataProtection, IOptions<OcpiOptions> options)
        {
            _db = db;
            _protector = dataProtection.CreateProtector("Ocpi.PartyTokens");
            _options = options.Value;
        }

        public OcpiIdentity Identity(double vatRate) => new(_options.CountryCode, _options.PartyId, _options.BusinessName, vatRate);

        public string Protect(string token) => _protector.Protect(token);

        public string? OutgoingToken(OcpiParty party)
        {
            if (string.IsNullOrEmpty(party.OutgoingTokenProtected)) return null;
            try { return _protector.Unprotect(party.OutgoingTokenProtected); }
            catch (System.Security.Cryptography.CryptographicException) { return null; }
        }

        public static List<EndpointDto> Endpoints(OcpiParty party)
        {
            if (string.IsNullOrEmpty(party.EndpointsJson)) return new();
            try { return JsonSerializer.Deserialize<List<EndpointDto>>(party.EndpointsJson, OcpiJson.Options) ?? new(); }
            catch (JsonException) { return new(); }
        }

        public static string? EndpointUrl(OcpiParty party, string identifier, string role) =>
            Endpoints(party).FirstOrDefault(e => string.Equals(e.Identifier, identifier, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase))?.Url?.TrimEnd('/');

        public static List<CredentialsRoleDto> Roles(OcpiParty party)
        {
            if (string.IsNullOrEmpty(party.RolesJson)) return new();
            try { return JsonSerializer.Deserialize<List<CredentialsRoleDto>>(party.RolesJson, OcpiJson.Options) ?? new(); }
            catch (JsonException) { return new(); }
        }

        // Parties that receive pushes: registered eMSPs/hubs that declared a receiver for the module.
        public async Task<List<(OcpiParty Party, string Url)>> ReceiversAsync(string module, CancellationToken cancellationToken = default)
        {
            var parties = await _db.OcpiParties.AsNoTracking()
                .Where(p => p.Status == OcpiPartyStatus.Registered).ToListAsync(cancellationToken);
            return parties.Select(p => (p, EndpointUrl(p, module, "RECEIVER")))
                .Where(x => x.Item2 != null).Select(x => (x.p, x.Item2!)).ToList();
        }

        // Adds a message without saving. A still pending PATCH/PUT to the same URL is replaced, so a
        // flapping status does not queue dozens of calls.
        public async Task EnqueueAsync(int partyId, string module, HttpMethod method, string url, object payload,
            bool coalesce, DateTime? notBefore = null, CancellationToken cancellationToken = default)
        {
            var json = payload as string ?? OcpiJson.Serialize(payload);
            var now = DateTime.UtcNow;
            if (coalesce)
            {
                var pending = await _db.OcpiOutbox.FirstOrDefaultAsync(m => m.OcpiPartyID == partyId && m.Url == url
                    && m.Method == method.Method && m.Status == OcpiOutboxStatus.Pending && m.Attempts == 0, cancellationToken);
                if (pending != null)
                {
                    pending.PayloadJson = json;
                    return;
                }
            }
            _db.OcpiOutbox.Add(new OcpiOutboxMessage
            {
                OcpiPartyID = partyId,
                Module = module,
                Method = method.Method,
                Url = url,
                PayloadJson = json,
                Status = OcpiOutboxStatus.Pending,
                NextAttemptAt = notBefore ?? now,
                CreatedAt = now
            });
        }
    }
}
