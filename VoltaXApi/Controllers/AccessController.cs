using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Controllers;

[ApiController]
[Route("api/access")]
public sealed class AccessController(VoltaXApiDbContext db, HubConnections connections) : ControllerBase
{
    private AccessSnapshot Access => (AccessSnapshot)HttpContext.Items[typeof(AccessSnapshot)]!;

    [HttpGet("me")]
    public IActionResult Me() => Ok(new {
        userId = Access.User.ID, roleId = Access.User.RoleID, role = Access.User.Role.Name, isAdmin = Access.IsAdmin,
        permissions = Access.IsAdmin ? AccessService.Catalog : Access.Permissions.OrderBy(p => p).ToArray(),
        firstName = Access.User.FirstName, lastName = Access.User.LastName, email = Access.User.Email,
        imageUrl = Access.User.Image?.Url ?? Access.User.ExternalPictureUrl, partnerId = Access.User.PartnerID
    });

    [HttpGet("profile")]
    public IActionResult GetProfile() => Ok(new {
        id = Access.User.ID, firstName = Access.User.FirstName, lastName = Access.User.LastName,
        email = Access.User.Email, phone = Access.User.Phone, birthday = Access.User.Birthday,
        imageUrl = Access.User.Image?.Url ?? Access.User.ExternalPictureUrl,
        isEmailVerified = Access.User.IsEmailVerified, isPhoneNumberVerified = Access.User.IsPhoneNumberVerified
    });

    public sealed record ProfileEdit([Required, MaxLength(100)] string FirstName, [Required, MaxLength(100)] string LastName, DateTime? Birthday);
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileEdit dto)
    {
        var user = await db.Users.SingleAsync(u => u.ID == Access.User.ID);
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName)) return BadRequest(new { message = "A first and last name are required." });
        user.FirstName = dto.FirstName.Trim(); user.LastName = dto.LastName.Trim(); user.Birthday = dto.Birthday;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    public sealed record AssignRoleDto(int RoleId);
    [HttpPut("users/{userId:int}/role")]
    public async Task<IActionResult> AssignRole(int userId, AssignRoleDto dto)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.ID == userId && !u.IsDeleted);
        var role = await db.Roles.SingleOrDefaultAsync(r => r.ID == dto.RoleId && !r.IsDeleted);
        if (user == null || role == null) return NotFound();
        if (user.ID == Access.User.ID && role.ID != user.RoleID) return BadRequest(new { message = "You cannot change your own role. Ask another administrator." });
        if (user.Role.Name == "Admin" && role.Name != "Admin" &&
            await db.Users.Where(Models.User.NotSuspendedAt(DateTime.UtcNow)).CountAsync(u => !u.IsDeleted && u.Role.Name == "Admin" && !u.Role.IsDeleted) <= 1)
            return BadRequest(new { message = "At least one active administrator must remain." });
        user.RoleID = role.ID; user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        connections.Revoke(new[] { user.ID });
        return Ok(new { roleId = role.ID, role = role.Name });
    }
}
