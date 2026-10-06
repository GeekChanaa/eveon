using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RoleController(VoltaXApiDbContext db, HubConnections connections) : ControllerBase
{
    public static bool IsBuiltIn(string name) => new[] { "Admin", "Customer", "Partner" }.Contains(name, StringComparer.OrdinalIgnoreCase);

    [HttpGet("GetAllRoles")]
    [HttpGet]
    public async Task<IActionResult> GetAllRoles() => Ok(await db.Roles.Where(r => !r.IsDeleted && (((AccessSnapshot)HttpContext.Items[typeof(AccessSnapshot)]!).IsAdmin || r.Name == "Customer"))
        .Select(r => new { id = r.ID, name = r.Name, permissionIds = r.RolePermissions.Where(p => !p.IsDeleted && !p.Permission.IsDeleted && p.Scope == PermissionScope.Global).Select(p => p.PermissionID).ToArray() }).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => Ok(await db.Roles.Where(r => r.ID == id && !r.IsDeleted).Select(r => new { id = r.ID, name = r.Name }).SingleOrDefaultAsync());

    [HttpPost("CreateRole")]
    public async Task<IActionResult> CreateRole(CreateRoleDto dto)
    {
        var name = dto.Name?.Trim() ?? "";
        if (name.Length < 2 || name.Length > 80 || IsBuiltIn(name)) return BadRequest(new { message = "Choose a unique role name between 2 and 80 characters. Built-in names are reserved." });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.Roles.AnyAsync(r => r.Name.ToUpper() == name.ToUpper())) return Conflict(new { message = "This role name already exists." });
        var permissions = await ValidPermissions(dto.Permissions ?? new List<int>());
        if (permissions == null) return BadRequest(new { message = "One or more permissions are invalid." });
        var role = new Role { Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            RolePermissions = permissions.Select(id => new RolePermission { PermissionID = id, Scope = PermissionScope.Global }).ToList() };
        db.Roles.Add(role); await db.SaveChangesAsync(); await transaction.CommitAsync();
        return Ok(new { id = role.ID, name = role.Name });
    }

    [HttpPut("UpdateRolePermissions/{roleId:int}")]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] List<int> permissionIds)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var role = await db.Roles.Include(r => r.RolePermissions).SingleOrDefaultAsync(r => r.ID == roleId && !r.IsDeleted);
        if (role == null) return NotFound();
        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Admin always has full access. Its permissions cannot be reduced." });
        var permissions = await ValidPermissions(permissionIds);
        if (permissions == null) return BadRequest(new { message = "One or more permissions are invalid." });
        db.RolePermissions.RemoveRange(role.RolePermissions);
        role.RolePermissions = permissions.Select(id => new RolePermission { PermissionID = id, Scope = PermissionScope.Global }).ToList();
        role.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        connections.Revoke(await db.Users.Where(u => u.RoleID == roleId).Select(u => u.ID).ToListAsync());
        return NoContent();
    }

    private async Task<int[]?> ValidPermissions(IEnumerable<int> ids)
    {
        var distinct = ids.Distinct().ToArray();
        var permissions = await db.Permissions.Where(p => distinct.Contains(p.ID) && !p.IsDeleted).ToListAsync();
        if (permissions.Count != distinct.Length || permissions.Any(p => !AccessService.Catalog.Contains(p.Name))) return null;
        // Editing a resource also requires viewing it; dashboard access is an explicit prerequisite.
        var names = permissions.Select(p => p.Name).ToHashSet();
        if (names.Count > 0 && !names.Contains("AccessDashboard")) return null;
        if (names.Contains("OperateChargePoints") && !names.Contains("ViewChargePoints")) return null;
        foreach (var name in names.Where(n => n.StartsWith("Edit") || n.StartsWith("Create") || n.StartsWith("Delete")))
        {
            var view = "View" + System.Text.RegularExpressions.Regex.Replace(name, "^(Edit|Create|Delete)", "");
            if (AccessService.Catalog.Contains(view) && !names.Contains(view)) return null;
        }
        return distinct;
    }
}
