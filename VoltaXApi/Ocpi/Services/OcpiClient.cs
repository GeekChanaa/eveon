using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VoltaXApi.Ocpi.Dtos;

namespace VoltaXApi.Ocpi.Services
{
    public class OcpiClientException : Exception
    {
        public OcpiClientException(string message, bool permanent = false) : base(message) { Permanent = permanent; }
        // 4xx answers other than 408/429: retrying the same payload will not help.
        public bool Permanent { get; }
    }

    // Outgoing OCPI calls: token header, routing headers, envelope unwrapping.
    public class OcpiClient
    {
        public const string HttpClientName = "ocpi";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OcpiOptions _options;
        private readonly ILogger<OcpiClient> _logger;

        public OcpiClient(IHttpClientFactory httpClientFactory, IOptions<OcpiOptions> options, ILogger<OcpiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<T?> SendAsync<T>(HttpMethod method, string url, string token, string? jsonBody,
            string? toCountryCode = null, string? toPartyId = null, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
                throw new OcpiClientException($"Invalid partner URL '{url}'", permanent: true);

            using var request = new HttpRequestMessage(method, uri);
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(OcpiTokens.ToHeader(token));
            var requestId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Request-ID", requestId);
            request.Headers.Add("X-Correlation-ID", requestId);
            request.Headers.Add("OCPI-from-country-code", _options.CountryCode);
            request.Headers.Add("OCPI-from-party-id", _options.PartyId);
            if (!string.IsNullOrEmpty(toCountryCode) && !string.IsNullOrEmpty(toPartyId))
            {
                request.Headers.Add("OCPI-to-country-code", toCountryCode);
                request.Headers.Add("OCPI-to-party-id", toPartyId);
            }
            if (jsonBody != null)
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var code = (int)response.StatusCode;
                _logger.LogWarning("OCPI {Method} {Host} answered HTTP {Status}", method, uri.Host, code);
                throw new OcpiClientException($"HTTP {code} from {uri.Host}",
                    permanent: code is >= 400 and < 500 and not 408 and not 429);
            }

            OcpiResponse<T>? envelope;
            try
            {
                envelope = string.IsNullOrWhiteSpace(text) ? null : JsonSerializer.Deserialize<OcpiResponse<T>>(text, OcpiJson.Options);
            }
            catch (JsonException)
            {
                throw new OcpiClientException($"Invalid OCPI response from {uri.Host}");
            }
            if (envelope == null) return default;
            if (envelope.StatusCode is < 1000 or >= 2000)
                throw new OcpiClientException($"OCPI status {envelope.StatusCode}: {envelope.StatusMessage}",
                    permanent: envelope.StatusCode is >= 2000 and < 3000);
            return envelope.Data;
        }
    }
}
