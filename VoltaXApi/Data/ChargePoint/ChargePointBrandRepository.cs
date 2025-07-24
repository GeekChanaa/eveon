using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;
using VoltaXApi.Helpers;
using AutoMapper.QueryableExtensions;
using Bogus.DataSets;

namespace VoltaXApi.Data
{
    public class ChargePointBrandRepository : Repository<ChargePointBrand>, IChargePointBrandRepository
    {
        public ChargePointBrandRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
        }

        public async Task<List<ChargePointBrandSelectDto>> GetAllChargePointBrands()
        {
            return await _context.ChargePointBrands.Select(u => new ChargePointBrandSelectDto
            {
                Name = u.Name,
                ID = u.ID
            }).ToListAsync();
        }
    }
}