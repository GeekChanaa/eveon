using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Authorization;
using VoltaXApi.Models;
using Xunit.Abstractions;

namespace VoltaX.Tests.Security;

/// <summary>
/// Calls every API action as an anonymous visitor, a customer and a partner, with ids that belong to nobody, and
/// checks that only the endpoints meant for them answer. A new endpoint without access rules fails here.
/// </summary>
public class AuthorizationMatrixTests : IAsyncLifetime
{
    /// <summary>Route values that match no row: a resource of somebody else.</summary>
    private const string ForeignId = "987654";

    private readonly ApiFactory _api = new();
    private readonly ITestOutputHelper _output;
    private User _customer = null!, _partner = null!;

    public AuthorizationMatrixTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            await Seeds.User(db, "admin@test.ma", role: "Admin");
            _customer = await Seeds.User(db, "customer@test.ma");
            var partner = new Partner { Name = "Matrix", Description = "", Email = "m@matrix.ma", Phone = "0600000009", PartnerIdentificationNumber = "P-M" };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();
            _partner = await Seeds.User(db, "partner@test.ma", role: "Partner", partnerId: partner.ID);
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private sealed record Call(string Method, string Path, string Name, ControllerActionDescriptor Action);

    /// <summary>Every MVC action and HTTP method, with route parameters filled with a foreign id.</summary>
    private IReadOnlyList<Call> Calls()
    {
        var endpoints = _api.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
        var calls = new List<Call>();
        foreach (var endpoint in endpoints)
        {
            var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action == null)
                continue;
            var methods = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods ?? new[] { "GET" };
            var path = "/" + Regex.Replace(endpoint.RoutePattern.RawText ?? "", @"\{\*?([^}:=?]+)[^}]*\}", ForeignId);
            foreach (var method in methods)
                calls.Add(new Call(method, path, $"{action.ControllerName}.{action.ActionName}", action));
        }
        return calls.DistinctBy(c => (c.Method, c.Path)).OrderBy(c => c.Path).ThenBy(c => c.Method).ToList();
    }

    private static async Task<HttpStatusCode> Send(HttpClient client, Call call)
    {
        using var request = new HttpRequestMessage(new HttpMethod(call.Method), call.Path);
        var multipart = call.Action.EndpointMetadata.OfType<Microsoft.AspNetCore.Mvc.ConsumesAttribute>()
            .Any(c => c.ContentTypes.Any(t => t.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase)))
            || call.Action.Parameters.Any(p => p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile)); // [ApiController] infers multipart
        if (multipart)
            request.Content = new MultipartFormDataContent { { new ByteArrayContent(new byte[] { 1 }), "file", "x.png" } };
        else if (call.Method is "POST" or "PUT" or "PATCH")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static bool Answered(HttpStatusCode status) => (int)status is >= 200 and < 300;

    /// <summary>Endpoints that are public by design: sign in / sign up flows, provider callbacks, charger uploads, OCPI.</summary>
    private static bool PublicByDesign(Call call) =>
        call.Action.ControllerTypeInfo.Namespace == "VoltaXApi.Ocpi.Controllers"
        || call.Action.EndpointMetadata.OfType<PublicEndpointAttribute>().Any()
        || call.Action.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any()
        || call.Action.ControllerTypeInfo.Namespace == "VoltaXApi.Controllers" && EndpointPermissions.IsPublic(call.Action.ControllerName, call.Action.ActionName, call.Method)
        || call.Name == "LogUpload.UploadChargerLog";

    /// <summary>What every signed-in account may use on its own data (the queries are scoped to the caller).</summary>
    private static readonly HashSet<string> SelfService = new(StringComparer.OrdinalIgnoreCase)
    {
        "Access.Me", "Access.GetProfile", "Access.UpdateProfile",
        "Auth.ValidateCurrentToken", "Auth.LogoutEverywhere", "Auth.CreateGoogleLinkTicket", "Auth.GoogleLinkWithToken", "Auth.GoogleUnlink",
        "Auth.GetLinkedAccounts", "Auth.ResendVerification", "Auth.GetTwoFactorStatus", "Auth.BeginTwoFactorEnrollment",
        "Auth.ConfirmTwoFactorEnrollment", "Auth.DisableTwoFactor",
        "Mobile.Wallet", "Mobile.Orders", "Mobile.NotificationPreferences", "Mobile.SaveNotificationPreference",
        "Notification.GetMyNotifications", "Notification.MarkAsRead", "Notification.MarkAllAsRead", "Notification.DeleteMyNotification",
        "Notification.DeleteAllMyNotifications", "NotificationType.GetAll", "NotificationType.GetById",
        "ChargingStation.SearchChargingStations", "FileManagement.UploadProfilePicture",
        "ChargingSession.GetMyCurrentChargingSession", "ChargingSession.GetMyChargingSessions",
        "EVDriver.RequestStartTransactionMobile", "EVDriver.RequestStopTransactionMobile",
        "ElectricVehicleModel.GetAllElectricVehicleModelsForSelect"
    };

    /// <summary>Shared endpoints that filter their rows on the caller's partner.</summary>
    private static readonly HashSet<string> PartnerScoped = new(StringComparer.OrdinalIgnoreCase)
    {
        "StationLoadBalancing.GetLoadBalancingStations"
    };

    private static bool SelfServiceAction(Call call) =>
        SelfService.Contains(call.Name)
        || call.Action.EndpointMetadata.OfType<SelfServiceAttribute>().Any()
        || call.Action.ControllerName == "Me"
        || call.Action.ControllerName == "NotificationType" && call.Method == "GET";

    private async Task<List<string>> Violations(HttpClient client, Func<Call, bool> allowed, Func<HttpStatusCode, bool> wrong)
    {
        var violations = new List<string>();
        foreach (var call in Calls().Where(c => !PublicByDesign(c)))
        {
            var status = await Send(client, call);
            if (!allowed(call) && wrong(status))
                violations.Add($"{call.Method} {call.Path} ({call.Name}) -> {(int)status}");
        }
        return violations;
    }

    [Fact]
    public async Task The_matrix_covers_the_api()
    {
        var calls = Calls();
        _output.WriteLine($"{calls.Count} calls, {calls.Count(PublicByDesign)} public by design");
        Assert.True(calls.Count > 300, $"only {calls.Count} endpoints found");
    }

    [Fact]
    public async Task Anonymous_visitors_get_401_everywhere_except_public_endpoints()
    {
        var violations = await Violations(_api.Client(), _ => false, status => status != HttpStatusCode.Unauthorized);
        violations.ForEach(_output.WriteLine);
        Assert.Empty(violations);
    }

    [Fact]
    public async Task Customers_only_reach_their_own_self_service_endpoints()
    {
        var violations = await Violations(_api.Client(_customer), SelfServiceAction, Answered);
        violations.ForEach(_output.WriteLine);
        Assert.Empty(violations);
    }

    [Fact]
    public async Task Partners_only_reach_the_partner_portal_and_self_service_endpoints()
    {
        var violations = await Violations(_api.Client(_partner),
            call => SelfServiceAction(call) || call.Action.EndpointMetadata.OfType<PartnerPortalAttribute>().Any() || PartnerScoped.Contains(call.Name), Answered);
        violations.ForEach(_output.WriteLine);
        Assert.Empty(violations);
    }
}
