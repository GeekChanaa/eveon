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
    }
}