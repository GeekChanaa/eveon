using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaX.E2E;

/// <summary>The API as a signed-in driver sees it: the customer portal (api/my) and the mobile remote start / stop.</summary>
public sealed class VoltaXClient : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly HttpClient _http;

    private VoltaXClient(HttpClient http) => _http = http;

    public static async Task<VoltaXClient> SignInAsync(string baseUrl, string email, string password, CancellationToken ct)
    {
        var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };
        var response = await http.PostAsJsonAsync("/api/Auth/Login", new { email, password }, ct);
        if (!response.IsSuccessStatusCode)
            throw new SetupException($"Login of {email} failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync(ct)}");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var token = body.RootElement.GetProperty("token").GetString();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new VoltaXClient(http);
    }

    public async Task<List<MySessionDto>> SessionsAsync(CancellationToken ct) => await AllPagesAsync<MySessionDto>("/api/my/sessions", ct);

    public async Task<List<MyWalletMovementDto>> WalletAsync(CancellationToken ct) => await AllPagesAsync<MyWalletMovementDto>("/api/my/wallet", ct);

    public async Task<List<MyCardDto>> CardsAsync(CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<MyCardDto>>("/api/my/cards", Json, ct) ?? new();

    public Task<MySessionDetailDto?> SessionAsync(int id, CancellationToken ct) =>
        _http.GetFromJsonAsync<MySessionDetailDto>($"/api/my/sessions/{id}", Json, ct);

    private async Task<List<T>> AllPagesAsync<T>(string path, CancellationToken ct)
    {
        var all = new List<T>();
        for (var page = 1; ; page++)
        {
            var result = await _http.GetFromJsonAsync<PagedResult<T>>($"{path}?page={page}&pageSize=100", Json, ct)
                ?? throw new InvalidOperationException($"{path} answered nothing");
            all.AddRange(result.Items);
            if (all.Count >= result.Total || result.Items.Count == 0)
                return all;
        }
    }

    /// <summary>The mobile app's remote start: the API picks the driver's card and needs the car plugged in (409/4xx before).</summary>
    public async Task<(HttpStatusCode Status, string Body)> RemoteStartAsync(string chargePointId, int remoteStartId, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/ocpp/EVDriver/RequestStartTransactionMobile/{chargePointId}",
            new { evseId = 1, remoteStartId }, ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(HttpStatusCode Status, string Body)> RemoteStopAsync(string chargePointId, string transactionId, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/ocpp/EVDriver/RequestStopTransactionMobile/{chargePointId}", new { transactionId }, ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    /// <summary>
    /// The transaction uid of the driver's running session (what the app and the portal stop), null when none
    /// (api/my/sessions/active answers 204).
    /// </summary>
    public async Task<string?> CurrentTransactionUidAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/my/sessions/active", ct);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;
        return (await response.Content.ReadFromJsonAsync<MyActiveSessionDto>(Json, ct))?.TransactionUid;
    }

    public void Dispose() => _http.Dispose();
}
