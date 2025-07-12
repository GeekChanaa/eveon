using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Data
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        private readonly IMapper _mapper;
        public RoleRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
        }

        public async Task CreateRole(CreateRoleDto dto)
        {
            // Create the role entity
            var role = new Role
            {
                Name = dto.Name,
                RolePermissions = dto.Permissions
                    .Select(permissionId => new RolePermission
                    {
                        PermissionID = permissionId,
                    })
                    .ToList()
            };

            // Add to database
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RoleListDto>> GetRolesByPermissionID(int permissionID)
        {
            var roles = await _context.RolePermissions
                .Where(rp => rp.PermissionID == permissionID)
                .Select(rp => new RoleListDto
                {
                    ID = rp.Role.ID,
                    Name = rp.Role.Name
                })
                .Distinct()
                .ToListAsync();

            return roles;
        }

        public async Task<List<RoleListDto>> GetAllRoles()
        {
            var roles = await _context.Roles
                .Select(rp => new RoleListDto
                {
                    ID = rp.ID,
                    Name = rp.Name
                })
                .ToListAsync();

            return roles;
        }

    }
}