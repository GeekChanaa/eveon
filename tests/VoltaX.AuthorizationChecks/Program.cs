using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VoltaXApi.Authorization;
using VoltaXApi.Controllers;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

var config = new ConfigurationBuilder().SetBasePath(Path.GetFullPath("VoltaXApi")).AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
var options = new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
var httpAccessor = new HttpContextAccessor();
await using var db = new VoltaXApiDbContext(options, httpAccessor);
if (args.Contains("--verify-live-admin"))
{
    // Read-only integration check against the local API, without printing credentials or personal data.
    var admin = await db.Users.AsNoTracking().FirstAsync(u => !u.IsDeleted && !u.Role.IsDeleted &&
        u.Role.Name == "Admin" && (u.SuspendedAt == null || u.SuspendedAt <= DateTime.UtcNow));
    var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(config["AppSettings:Token"]!));
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(claims: new[] { new Claim(ClaimTypes.NameIdentifier, admin.ID.ToString()) },
        expires: DateTime.UtcNow.AddMinutes(1), signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
    using var client = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token));
    using var response = await client.GetAsync("/api/access/me");
    response.EnsureSuccessStatusCode();
    using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    if (!body.RootElement.GetProperty("isAdmin").GetBoolean()) throw new Exception("Admin was not recognized.");
    var permissions = body.RootElement.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToHashSet();
    if (!AccessService.Catalog.All(p => permissions.Contains(p))) throw new Exception("Admin is missing permissions.");
    using var roles = await client.GetAsync("/api/role/GetAllRoles");
    roles.EnsureSuccessStatusCode();
    Console.WriteLine($"PASS: running API grants Admin all {AccessService.Catalog.Length} permissions and access to role management, without relying on JWT role claims.");
    return;
}
await using var transaction = await db.Database.BeginTransactionAsync();
var count = 0;
void Check(bool result, string name) { if (!result) throw new Exception("FAILED: " + name); Console.WriteLine("PASS: " + name); count++; }
var role = new Role { Name = "Authorization-check-" + Guid.NewGuid().ToString("N"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
var user = new User { FirstName = "Access", LastName = "Check", Email = Guid.NewGuid().ToString("N") + "@example.invalid", Role = role, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
db.Users.Add(user); await db.SaveChangesAsync();
var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
    new Claim(ClaimTypes.GivenName, user.FirstName),
    new Claim(ClaimTypes.Surname, user.LastName),
    new Claim(ClaimTypes.Role, "Admin"),
    new Claim("permission", "*")
}, "test"));
httpAccessor.HttpContext = new DefaultHttpContext { User = principal };
var auditedCard = new Card
{
    CardNumber = "audit-check-" + Guid.NewGuid().ToString("N"),
    Note = "audit check",
    Balance = 10,
    ExpirationDate = DateTime.UtcNow.AddYears(1),
    UserID = user.ID
};
db.Cards.Add(auditedCard); await db.SaveChangesAsync();
auditedCard.Balance = 42; await db.SaveChangesAsync();
var balanceHistory = await db.CardChangeHistories.SingleAsync(history =>
    history.CardID == auditedCard.ID && history.PropertyName == nameof(Card.Balance));
Check(balanceHistory.OldValue == "10" && balanceHistory.NewValue == "42" &&
    balanceHistory.Source == "User" && balanceHistory.ChangedByUserID == user.ID && balanceHistory.ChangedByName == "Access Check",
    "card updates create a field-level audit record with the acting user");
balanceHistory.NewValue = "tampered";
var historyWasImmutable = false;
try { await db.SaveChangesAsync(); }
catch (InvalidOperationException) { historyWasImmutable = true; }
Check(historyWasImmutable, "card audit records cannot be modified through the application context");
db.Entry(balanceHistory).State = EntityState.Unchanged;
var service = new AccessService(db);
var snapshot = await service.Resolve(principal);
Check(snapshot != null && !snapshot.IsAdmin && !snapshot.Can("ViewUsers"), "forged/stale role claims do not grant access");
var filter = new DashboardAccessFilter(service, db, new HubConnections(), new NoAuditLogger());
async Task<bool> Allowed(Type type, string action, string method, Dictionary<string, object?>? args = null, ClaimsPrincipal? caller = null)
{
    var info = type.GetMethods().First(m => m.Name == action);
    var http = new DefaultHttpContext { User = caller ?? principal }; http.Request.Method = method;
    var descriptor = new ControllerActionDescriptor { ControllerTypeInfo = type.GetTypeInfo(), ControllerName = type.Name.Replace("Controller", ""), ActionName = action, MethodInfo = info };
    var actionContext = new ActionContext(http, new RouteData(), descriptor, new ModelStateDictionary());
    var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), args ?? new(), new object());
    var executed = false;
    await filter.OnActionExecutionAsync(context, () => { executed = true; return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object())); });
    return executed;
}
Check(!await Allowed(typeof(ChargingStationController), "GetById", "GET"), "no permissions denies protected reads");
Check(!await Allowed(typeof(RoleController), "CreateRole", "POST"), "non-admin cannot create roles");
Check(!await Allowed(typeof(RoleController), "UpdateRolePermissions", "PUT"), "non-admin cannot alter role permissions");
Check(!await Allowed(typeof(AccessController), "AssignRole", "PUT"), "non-admin cannot assign roles");
Check(!await Allowed(typeof(UserController), "Update", "PUT"), "raw user mass assignment is disabled");
Check(!await Allowed(typeof(UserController), "GetById", "GET"), "raw user credentials are not exposed");
Check(await Allowed(typeof(AccessController), "UpdateProfile", "PUT"), "active users may edit their own limited profile");
Check(!await Allowed(typeof(UserController), "UploadUserAvatar", "POST", new() { ["userID"] = user.ID + 1 }), "another user's avatar is protected");
Check(await Allowed(typeof(UserController), "UploadUserAvatar", "POST", new() { ["userID"] = user.ID }), "own avatar upload is permitted");
Check(!await Allowed(typeof(ChargingStationController), "GetById", "GET", caller: new ClaimsPrincipal(new ClaimsIdentity())), "anonymous reads are denied");
Check(await Allowed(typeof(ChargingSessionController), "GetMyChargingSessions", "GET"), "authenticated users may read their own charging-session history");
Check(typeof(ChargingSessionController).GetMethod("GetMyChargingSessions")!.GetParameters().All(parameter => parameter.ParameterType != typeof(int)),
    "own charging-session history does not accept a caller-supplied user ID");
async Task<RolePermission> Grant(string name, PermissionScope scope = PermissionScope.Global)
{
    var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Name == name && !p.IsDeleted);
    if (permission == null) { permission = new Permission { Name = name }; db.Permissions.Add(permission); }
    var grant = new RolePermission { Role = role, Permission = permission, Scope = scope }; db.RolePermissions.Add(grant); await db.SaveChangesAsync(); return grant;
}
await Grant("AccessDashboard"); var readGrant = await Grant("ViewChargingStations");
Check(await Allowed(typeof(ChargingStationController), "GetById", "GET"), "view permission permits the corresponding read");
Check(!await Allowed(typeof(ChargingStationController), "Update", "PUT"), "view permission cannot update");
Check(!await Allowed(typeof(ChargingStationController), "Delete", "DELETE"), "view permission cannot delete");
await Grant("EditGlobalConfigurations");
Check(!await Allowed(typeof(VoltaXApi.OCPP.Controllers.ConfigurationController), "Reset", "POST"), "global configuration access cannot execute OCPP commands");
readGrant.IsDeleted = true; await db.SaveChangesAsync();
Check(!await Allowed(typeof(ChargingStationController), "GetById", "GET"), "permission revocation affects the same token immediately");
await Grant("EditChargingStations", PermissionScope.OwnOnly);
Check(!(await service.Resolve(principal))!.Can("EditChargingStations"), "scoped grants never become global dashboard access");
user.SuspendedAt = DateTime.UtcNow.AddHours(1); await db.SaveChangesAsync();
Check(await service.Resolve(principal) == null, "suspended accounts are denied");
user.SuspendedAt = null; role.IsDeleted = true; await db.SaveChangesAsync();
Check(await service.Resolve(principal) == null, "deleted roles are denied");
role.IsDeleted = false; user.Role = await db.Roles.SingleAsync(r => r.Name == "Admin" && !r.IsDeleted); await db.SaveChangesAsync();
Check((await service.Resolve(principal))!.Can("FuturePermission"), "admin has all current and future permissions");
Check(!await Allowed(typeof(UserController), "Update", "PUT"), "even admins use safe user endpoints");
Check(EndpointPermissions.Required("ChargePoint", "SendMessageToChargePoint", "POST") == "OperateChargePoints", "charger commands require an explicit operation permission");
Check(EndpointPermissions.Required("CardChangeHistory", "GetForCard", "GET") == "ViewChargingCardHistory", "card history requires its dedicated read permission");
Check(EndpointPermissions.Required("FutureController", "GetAll", "GET") == null, "unmapped controllers fail closed");
Check(!EndpointPermissions.IsPublic("Auth", "SendSmsTest", "GET"), "test SMS endpoint is not public");
Check(RoleController.IsBuiltIn("aDmIn"), "reserved role names are case-insensitive");
await transaction.RollbackAsync();
Console.WriteLine($"{count} authorization checks passed; all database test changes rolled back.");

// The checks exercise access decisions only; audit rows are not under test.
sealed class NoAuditLogger : VoltaXApi.Services.Audit.IAuditLogger
{
    public Task LogAsync(string action, string? entityType = null, string? entityId = null, object? details = null) => Task.CompletedTask;
    public Task LogForUserAsync(string action, int? userId, string? userEmail, string? entityType = null, string? entityId = null, object? details = null) => Task.CompletedTask;
}
