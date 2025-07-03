using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface IPermissionRepository : IRepository<Permission>
    {
        Task<List<PermissionListDto>> GetAllPermissions();
        Task<PermissionDisplayDto> GetPermissionByID(int permissionID);
    }
}