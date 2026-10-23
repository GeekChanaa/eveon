using System.Net;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Api;

public class ApiSmokeTests : IClassFixture<ApiSmokeTests.Fixture>
{
    public sealed class Fixture : IAsyncLifetime
    {
        public ApiFactory Api { get; } = new();
        public User Customer { get; private set; } = null!;

        public async Task InitializeAsync() =>
            await Api.Seed(async db => Customer = await Seeds.User(db, "customer@test.ma"));

        public Task DisposeAsync()
        {
            Api.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly Fixture _fixture;
    public ApiSmokeTests(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_is_up()
    {
        var response = await _fixture.Api.Client().GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_calls_to_protected_endpoints_are_401()
    {
        var response = await _fixture.Api.Client().GetAsync("/api/access/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_reads_their_own_access_but_not_the_user_list()
    {
        var client = _fixture.Api.Client(_fixture.Customer);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/access/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/User")).StatusCode);
    }
}
