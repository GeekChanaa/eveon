using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;

namespace VoltaXApi.Ocpi.Services
{
    public class OcpiValidationException : Exception
    {
        public OcpiValidationException(string message) : base(message) { }
    }

    // Tokens receiver: tokens pushed by eMSPs.
    public class OcpiTokenStore
    {
        public static readonly string[] TokenTypes = { "AD_HOC_USER", "APP_USER", "OTHER", "RFID" };
        public static readonly string[] WhitelistTypes = { "ALWAYS", "ALLOWED", "ALLOWED_OFFLINE", "NEVER" };
        private readonly VoltaXApiDbContext _db;

        public OcpiTokenStore(VoltaXApiDbContext db)
        {
            _db = db;
        }

        // A party may only manage tokens of the roles it registered with (a hub declares several).
        public static bool Owns(OcpiParty party, string countryCode, string partyId) =>
            string.Equals(party.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase) && string.Equals(party.PartyId, partyId, StringComparison.OrdinalIgnoreCase)
            || OcpiPartyService.Roles(party).Any(r => string.Equals(r.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.PartyId, partyId, StringComparison.OrdinalIgnoreCase));

        public Task<OcpiToken?> FindAsync(OcpiParty party, string countryCode, string partyId, string uid, string type, CancellationToken cancellationToken = default) =>
            _db.OcpiTokens.FirstOrDefaultAsync(t => t.OcpiPartyID == party.ID && t.CountryCode == countryCode
                && t.PartyId == partyId && t.Uid == uid && t.Type == type, cancellationToken);

        public static TokenDto ToDto(OcpiToken token) => new()
        {
            CountryCode = token.CountryCode,
            PartyId = token.PartyId,
            Uid = token.Uid,
            Type = token.Type,
            ContractId = token.ContractId,
            VisualNumber = token.VisualNumber,
            Issuer = token.Issuer,
            GroupId = token.GroupId,
            Valid = token.Valid,
            Whitelist = token.Whitelist,
            Language = token.Language,
            DefaultProfileType = token.DefaultProfileType,
            LastUpdated = OcpiJson.ToUtc(token.LastUpdated)
        };

        // PUT replaces the whole token; PATCH (partial) only changes the fields present.
        public async Task<OcpiToken> UpsertAsync(OcpiParty party, string countryCode, string partyId, string uid, string type,
            TokenDto dto, bool partial, CancellationToken cancellationToken = default)
        {
            countryCode = countryCode.ToUpperInvariant();
            partyId = partyId.ToUpperInvariant();
            if (countryCode.Length != 2 || partyId.Length != 3 || string.IsNullOrWhiteSpace(uid) || uid.Length > 36)
                throw new OcpiValidationException("Invalid token identification.");
            if (!TokenTypes.Contains(type)) throw new OcpiValidationException("Invalid token type.");
            if (!Owns(party, countryCode, partyId)) throw new OcpiValidationException("Token does not belong to the calling party.");
            if (dto.Uid != null && dto.Uid != uid || dto.Type != null && dto.Type != type
                || dto.CountryCode != null && !dto.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase)
                || dto.PartyId != null && !dto.PartyId.Equals(partyId, StringComparison.OrdinalIgnoreCase))
                throw new OcpiValidationException("Token body does not match the URL.");
            if (dto.Whitelist != null && !WhitelistTypes.Contains(dto.Whitelist)) throw new OcpiValidationException("Invalid whitelist value.");
            if (dto.LastUpdated == null) throw new OcpiValidationException("last_updated is required.");

            var token = await FindAsync(party, countryCode, partyId, uid, type, cancellationToken);
            if (token == null)
            {
                if (partial) throw new KeyNotFoundException();
                token = new OcpiToken { OcpiPartyID = party.ID, CountryCode = countryCode, PartyId = partyId, Uid = uid, Type = type, CreatedAt = DateTime.UtcNow };
                _db.OcpiTokens.Add(token);
            }
            if (!partial && (string.IsNullOrWhiteSpace(dto.ContractId) || string.IsNullOrWhiteSpace(dto.Issuer) || dto.Valid == null || dto.Whitelist == null))
                throw new OcpiValidationException("contract_id, issuer, valid and whitelist are required.");

            if (!partial || dto.ContractId != null) token.ContractId = Limit(dto.ContractId, 36) ?? "";
            if (!partial || dto.VisualNumber != null) token.VisualNumber = Limit(dto.VisualNumber, 64);
            if (!partial || dto.Issuer != null) token.Issuer = Limit(dto.Issuer, 64) ?? "";
            if (!partial || dto.GroupId != null) token.GroupId = Limit(dto.GroupId, 36);
            if (!partial || dto.Valid != null) token.Valid = dto.Valid ?? false;
            if (!partial || dto.Whitelist != null) token.Whitelist = dto.Whitelist ?? "ALLOWED";
            if (!partial || dto.Language != null) token.Language = Limit(dto.Language, 2);
            if (!partial || dto.DefaultProfileType != null) token.DefaultProfileType = Limit(dto.DefaultProfileType, 16);
            token.LastUpdated = OcpiJson.ToUtc(dto.LastUpdated.Value);
            await _db.SaveChangesAsync(cancellationToken);
            return token;
        }

        private static string? Limit(string? value, int length) => value == null ? null : value.Length <= length ? value : value[..length];
    }
}
