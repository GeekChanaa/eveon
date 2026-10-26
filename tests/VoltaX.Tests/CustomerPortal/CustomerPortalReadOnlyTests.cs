using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Authorization;

namespace VoltaX.Tests.CustomerPortal;

/// <summary>
/// The owner's rule for the customer portal: customers see their own data and change nothing, except paying online,
/// managing their saved payment cards and filling in their optional profile details. Checked on the routes themselves,
/// so a write endpoint added later fails here.
/// </summary>
public class CustomerPortalReadOnlyTests : IDisposable
{
    private readonly ApiFactory _api = new();

    public void Dispose() => _api.Dispose();

    private IEnumerable<(string Route, string Method, ControllerActionDescriptor Action)> Endpoints() =>
        _api.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() != null)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? new[] { "GET" })
                .Select(m => (Route: e.RoutePattern.RawText ?? "", Method: m, Action: e.Metadata.GetMetadata<ControllerActionDescriptor>()!)));

    [Fact]
    public void The_customer_portal_api_only_reads_besides_the_profile_details()
    {
        var portal = Endpoints().Where(e => e.Route.Equals("api/my", StringComparison.OrdinalIgnoreCase)
            || e.Route.StartsWith("api/my/", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.True(portal.Count >= 10, $"only {portal.Count} api/my endpoints found");
        Assert.All(portal, e => Assert.True(e.Method == "GET" || $"{e.Method} {e.Route}" == "PUT api/my/profile",
            $"{e.Method} {e.Route} writes in the read-only customer portal"));
    }

    [Fact]
    public void The_only_self_service_writes_are_payments_saved_cards_and_profile_details()
    {
        var writes = Endpoints().Where(e => e.Method != "GET" && e.Action.EndpointMetadata.OfType<SelfServiceAttribute>().Any())
            .Select(e => $"{e.Method} {e.Route}").OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.Equal(new[]
        {
            "DELETE api/payments/methods/{id:int}",
            "POST api/payments/methods/setup",
            "POST api/payments/methods/{id:int}/default",
            "POST api/payments/top-ups",
            "PUT api/my/profile"
        }, writes);
    }
}
