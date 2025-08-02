using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Data
{
    public class PermissionRepository : Repository<Permission>, IPermissionRepository
    {
        private readonly IMapper _mapper;
        public PermissionRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
        }

        public async Task<List<PermissionListDto>> GetAllPermissions()
        {
            return await _context.Permissions.Select(u => new PermissionListDto
            {
                Name = u.Name,
                ID = u.ID
            }).ToListAsync();
        }

        public async Task<PermissionDisplayDto> GetPermissionByID(int permissionID)
        {
            var permission = await _context.Permissions.FirstOrDefaultAsync(u => u.ID == permissionID);
            return new PermissionDisplayDto
            {
                Name = permission.Name,
                Description = permission.Description
            };
        }

        public async Task<List<PermissionListDto>> GetRolePermissions(int roleID)
        {
            var rolePermissions = await _context.Roles
            .Where(r => r.ID == roleID)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => new PermissionListDto
            {
                ID = rp.Permission.ID,
                Name = rp.Permission.Name
            })
            .ToListAsync();

            return rolePermissions;
        }

    }
}