using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Authorization;

/// <summary>A user's account and global permissions, shared read-only between requests while cached.</summary>
public sealed record AccessSnapshot(User User, HashSet<string> Permissions)
{
    public bool IsAdmin => User.Role.Name == "Admin";
    public bool Can(string permission) => IsAdmin || Permissions.Contains(permission);
}

/// <summary>
/// Resolves the caller's access from the database, so a still-valid JWT never keeps revoked access. Granted access is
/// kept a few seconds in <see cref="AccessSnapshotCache"/>, which drops it as soon as the user, a role or a permission
/// changes.
/// </summary>
public sealed class AccessService(VoltaXApiDbContext db, AccessSnapshotCache cache)
{
    public static readonly string[] Catalog = Enum.GetNames<PermissionEnum>();
    public async Task<AccessSnapshot?> Resolve(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        if (cache.TryGet(id, out var cached)) return cached;
        var stamp = cache.Current(id);
        var user = await db.Users.AsNoTracking().Include(u => u.Role).ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission).Include(u => u.Image).SingleOrDefaultAsync(u => u.ID == id);
        if (user == null || user.IsDeleted || user.IsSuspended(DateTime.UtcNow) || user.Role == null || user.Role.IsDeleted) return null;
        var permissions = user.Role.RolePermissions.Where(rp => !rp.IsDeleted && !rp.Permission.IsDeleted &&
            rp.Scope == PermissionScope.Global && Catalog.Contains(rp.Permission.Name)).Select(rp => rp.Permission.Name).ToHashSet(StringComparer.Ordinal);
        var snapshot = new AccessSnapshot(user, permissions);
        cache.Set(id, snapshot, stamp);
        return snapshot;
    }
}
