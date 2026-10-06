using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;

namespace VoltaXApi.Controllers;
[ApiController]
[Route("api/[controller]")]
public sealed class PermissionController(VoltaXApiDbContext db) : ControllerBase
{
    [HttpGet("GetAllPermissions")]
    [HttpGet]
    public async Task<IActionResult> GetAllPermissions() => Ok(await db.Permissions.Where(p => !p.IsDeleted && AccessService.Catalog.Contains(p.Name))
        .Select(p => new { id = p.ID, name = p.Name, description = p.Description }).ToListAsync());

    [HttpGet("GetRolePermissions/{roleID:int}")]
    public async Task<IActionResult> GetRolePermissions(int roleID) => Ok(await db.RolePermissions.Where(p => p.RoleID == roleID && !p.IsDeleted && !p.Permission.IsDeleted)
        .Select(p => new { id = p.PermissionID, name = p.Permission.Name, description = p.Permission.Description }).ToListAsync());
}
