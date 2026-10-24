using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VoltaXApi.Authorization;

/// <summary>
/// Access markers read by <see cref="DashboardAccessFilter"/> for endpoints that are not dashboard features.
/// Put on a controller (all actions) or an action. Unmarked endpoints keep the permission rules.
/// </summary>
public abstract class AccessMarkerAttribute : Attribute;

/// <summary>Callable without signing in (payment provider callbacks, return pages). The action authenticates the caller itself.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PublicEndpointAttribute : AccessMarkerAttribute;

/// <summary>Any signed-in, active account. Every query in the action must be scoped to the caller (<see cref="AccessContext.User"/>).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SelfServiceAttribute : AccessMarkerAttribute;

/// <summary>A partner account (role Partner with a partner). Every query must be scoped to <see cref="AccessContext.PartnerId"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PartnerPortalAttribute : AccessMarkerAttribute;

/// <summary>The resolved caller of the current request, set by <see cref="DashboardAccessFilter"/>.</summary>
public static class AccessContext
{
    public static AccessSnapshot Access(this ControllerBase controller) =>
        controller.HttpContext.Items[typeof(AccessSnapshot)] as AccessSnapshot
        ?? throw new InvalidOperationException("No access snapshot: the endpoint is not behind DashboardAccessFilter or is public.");

    public static Models.User User(this ControllerBase controller) => controller.Access().User;

    public static int PartnerId(this ControllerBase controller) =>
        controller.User().PartnerID ?? throw new InvalidOperationException("The caller is not a partner.");

    internal static T? Marker<T>(ActionExecutingContext context) where T : AccessMarkerAttribute =>
        context.ActionDescriptor.EndpointMetadata.OfType<T>().FirstOrDefault();
}
