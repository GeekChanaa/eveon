using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Authorization;

public sealed record AccessSnapshot(User User, HashSet<string> Permissions)
{
    public bool IsAdmin => User.Role.Name == "Admin";
    public bool Can(string permission) => IsAdmin || Permissions.Contains(permission);
}

// Read from the database on every request: a still-valid JWT never retains revoked access.
public sealed class AccessService(VoltaXApiDbContext db)
{
    public static readonly string[] Catalog = Enum.GetNames<PermissionEnum>();
    public async Task<AccessSnapshot?> Resolve(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        var user = await db.Users.AsNoTracking().Include(u => u.Role).ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission).Include(u => u.Image).SingleOrDefaultAsync(u => u.ID == id);
        if (user == null || user.IsDeleted || user.IsSuspended(DateTime.UtcNow) || user.Role == null || user.Role.IsDeleted) return null;
        var permissions = user.Role.RolePermissions.Where(rp => !rp.IsDeleted && !rp.Permission.IsDeleted &&
            rp.Scope == PermissionScope.Global && Catalog.Contains(rp.Permission.Name)).Select(rp => rp.Permission.Name).ToHashSet(StringComparer.Ordinal);
        return new AccessSnapshot(user, permissions);
    }
}
