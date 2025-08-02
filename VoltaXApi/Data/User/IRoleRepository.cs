using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task CreateRole(CreateRoleDto role);
        Task<List<RoleListDto>> GetRolesByPermissionID(int permissionID);
        Task<List<RoleListDto>> GetAllRoles();
        Task UpdateRolePermission(int roleId, List<int> permissionIds);
    }
}