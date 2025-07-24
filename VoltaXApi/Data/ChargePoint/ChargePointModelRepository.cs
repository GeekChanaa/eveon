
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AutoMapper;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public class ChargePointModelRepository : Repository<ChargePointModel>, IChargePointModelRepository
    {

        private readonly IMapper _mapper;
        public ChargePointModelRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<List<ChargePointModelSelectDto>> GetAllChargePointModels()
        {
            return await _context.ChargePointModels.Select(u => new ChargePointModelSelectDto
            {
                Name = u.Name,
                ID = u.ID
            }).ToListAsync();
        }

        public async Task<int?> GetChargePointModelIDByIdentifier(string identifier)
        {
            var cpm = await _context.ChargePointModels.FirstOrDefaultAsync(u => u.Identifier == identifier);
            if(cpm != null)
              return cpm.ID;
            return null;
        }

    }
}