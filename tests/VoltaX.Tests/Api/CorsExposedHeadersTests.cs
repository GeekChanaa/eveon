using System.Net;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Api;

/// <summary>
/// The dashboard runs on another origin: it can only read the response headers the CORS policy exposes.
/// Without Pagination, every list shows its first page and no pager.
/// </summary>
public class CorsExposedHeadersTests : IClassFixture<CorsExposedHeadersTests.Fixture>
{
    private const string Dashboard = "http://dashboard.test";

    public sealed class Fixture : IAsyncLifetime
    {
        public ApiFactory Api { get; } = new(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = Dashboard });
        public User Admin { get; private set; } = null!;

        public async Task InitializeAsync() =>
            await Api.Seed(async db => Admin = await Seeds.User(db, "admin@test.ma", "Admin"));

        public Task DisposeAsync()
        {
            Api.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly Fixture _fixture;
    public CorsExposedHeadersTests(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task A_list_response_exposes_its_pagination_header_to_the_dashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User/GetUsers?pageNumber=1&pageSize=20");
        request.Headers.Add("Origin", Dashboard);

        var response = await _fixture.Api.Client(_fixture.Admin).SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Pagination"));
        var exposed = string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers"))
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("Pagination", exposed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Content-Disposition", exposed, StringComparer.OrdinalIgnoreCase);
    }
}
