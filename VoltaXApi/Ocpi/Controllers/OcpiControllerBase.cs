using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.Ocpi.Services;

namespace VoltaXApi.Ocpi.Controllers
{
    public abstract class OcpiControllerBase : ControllerBase
    {
        public const string PartyItem = "OcpiParty";
        public const string TokenItem = "OcpiToken";
        public const string TokenAItem = "OcpiTokenA";

        protected OcpiParty Party => (OcpiParty)HttpContext.Items[PartyItem]!;
        protected string CallerToken => (string)HttpContext.Items[TokenItem]!;
        protected bool CalledWithTokenA => HttpContext.Items[TokenAItem] is true;

        protected IActionResult Ocpi<T>(T data, int httpStatus = 200) => Envelope(data, OcpiStatusCodes.Success, null, httpStatus);

        protected IActionResult OcpiError(int httpStatus, int statusCode, string message) => Envelope<object>(null, statusCode, message, httpStatus);

        public static IActionResult Envelope<T>(T? data, int statusCode, string? message, int httpStatus) =>
            new JsonResult(new OcpiResponse<T>
            {
                Data = data,
                StatusCode = statusCode,
                StatusMessage = message ?? (statusCode == OcpiStatusCodes.Success ? "Success" : null),
                Timestamp = DateTime.UtcNow
            }, OcpiJson.Options) { StatusCode = httpStatus };

        // Pagination of a Sender GET list: offset/limit query, X-Total-Count, X-Limit and Link headers.
        protected IActionResult Paged<T>(IReadOnlyCollection<T> all, int? offset, int? limit)
        {
            var page = OcpiPagination.Apply(Request, Response, all.Count, offset, limit);
            return Ocpi(all.Skip(page.Offset).Take(page.Limit).ToList());
        }

        protected static bool TryDates(string? from, string? to, out DateTime? dateFrom, out DateTime? dateTo)
        {
            dateFrom = dateTo = null;
            if (!string.IsNullOrEmpty(from))
            {
                if (!DateTime.TryParse(from, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)) return false;
                dateFrom = parsed;
            }
            if (!string.IsNullOrEmpty(to))
            {
                if (!DateTime.TryParse(to, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)) return false;
                dateTo = parsed;
            }
            return true;
        }
    }

    public readonly record struct OcpiPage(int Offset, int Limit);

    public static class OcpiPagination
    {
        public const int DefaultLimit = 50;
        public const int MaxLimit = 100;

        public static OcpiPage Apply(HttpRequest request, HttpResponse response, int total, int? offset, int? limit)
        {
            var start = Math.Max(0, offset ?? 0);
            var size = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
            response.Headers["X-Total-Count"] = total.ToString(CultureInfo.InvariantCulture);
            response.Headers["X-Limit"] = MaxLimit.ToString(CultureInfo.InvariantCulture);
            if (start + size < total)
            {
                var query = request.Query.Where(q => q.Key is not ("offset" or "limit"))
                    .ToDictionary(q => q.Key, q => (string?)q.Value.ToString());
                query["offset"] = (start + size).ToString(CultureInfo.InvariantCulture);
                query["limit"] = size.ToString(CultureInfo.InvariantCulture);
                var url = QueryHelpers.AddQueryString($"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}", query);
                response.Headers["Link"] = $"<{url}>; rel=\"next\"";
            }
            return new OcpiPage(start, size);
        }
    }

    // Token authentication of OCPI calls ("Authorization: Token <base64 token>").
    // Registered: only registered parties with their credentials token (modules).
    // Handshake: also token A and parties still pending (versions, version details, credentials).
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class OcpiAuthorizeAttribute : TypeFilterAttribute
    {
        public OcpiAuthorizeAttribute(bool handshake = false) : base(typeof(OcpiAuthFilter))
        {
            Arguments = new object[] { handshake };
        }
    }

    public sealed class OcpiAuthFilter : IAsyncActionFilter
    {
        private readonly bool _handshake;
        private readonly VoltaXApiDbContext _db;
        private readonly OcpiOptions _options;

        public OcpiAuthFilter(bool handshake, VoltaXApiDbContext db, IOptions<OcpiOptions> options)
        {
            _handshake = handshake;
            _db = db;
            _options = options.Value;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!_options.Enabled) { context.Result = new NotFoundResult(); return; }
            var http = context.HttpContext;
            foreach (var header in new[] { "X-Request-ID", "X-Correlation-ID" })
                if (http.Request.Headers.TryGetValue(header, out var value) && value.ToString().Length <= 64)
                    http.Response.Headers[header] = value.ToString();

            var candidates = OcpiTokens.FromHeader(http.Request.Headers.Authorization);
            var hashes = candidates.Select(OcpiTokens.Hash).ToList();
            OcpiParty? party = null;
            if (hashes.Count > 0)
                party = await _db.OcpiParties.FirstOrDefaultAsync(p => p.Status != OcpiPartyStatus.Unregistered
                    && (hashes.Contains(p.IncomingTokenHash!) || hashes.Contains(p.TokenAHash!)));
            var usedTokenA = party?.TokenAHash != null && hashes.Contains(party.TokenAHash);
            var allowed = party != null && (party.Status == OcpiPartyStatus.Registered && !usedTokenA
                || _handshake && party.Status is OcpiPartyStatus.Pending or OcpiPartyStatus.Registered);
            if (!allowed)
            {
                context.Result = OcpiControllerBase.Envelope<object>(null, OcpiStatusCodes.ClientError, "Invalid or missing token.", StatusCodes.Status401Unauthorized);
                return;
            }

            // Messages routed by a hub to another platform do not belong here.
            var toCountry = http.Request.Headers["OCPI-to-country-code"].ToString();
            var toParty = http.Request.Headers["OCPI-to-party-id"].ToString();
            if (toCountry.Length > 0 && toParty.Length > 0 && (!toCountry.Equals(_options.CountryCode, StringComparison.OrdinalIgnoreCase)
                || !toParty.Equals(_options.PartyId, StringComparison.OrdinalIgnoreCase)))
            {
                context.Result = OcpiControllerBase.Envelope<object>(null, OcpiStatusCodes.ClientError, "Unknown recipient party.", StatusCodes.Status400BadRequest);
                return;
            }

            http.Response.Headers["OCPI-from-country-code"] = _options.CountryCode;
            http.Response.Headers["OCPI-from-party-id"] = _options.PartyId;
            if (party!.CountryCode != null && party.PartyId != null)
            {
                http.Response.Headers["OCPI-to-country-code"] = party.CountryCode;
                http.Response.Headers["OCPI-to-party-id"] = party.PartyId;
            }
            http.Items[OcpiControllerBase.PartyItem] = party;
            http.Items[OcpiControllerBase.TokenItem] = usedTokenA ? candidates.First(c => OcpiTokens.Hash(c) == party.TokenAHash)
                : candidates.First(c => OcpiTokens.Hash(c) == party.IncomingTokenHash);
            http.Items[OcpiControllerBase.TokenAItem] = usedTokenA;
            await next();
        }
    }
}
