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
    }
}