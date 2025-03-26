using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper.QueryableExtensions;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class OCPPLocalListVersionRepository : Repository<OCPPLocalListVersion>,IOCPPLocalListVersionRepository
    {
        private readonly IMapper _mapper;
        public OCPPLocalListVersionRepository(
            VoltaXApiDbContext context,
            IMapper mapper
            ) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<OCPPLocalListDashboardDto> GetChargePointLocalList(int chargePointID)
        {
            var localList = await _context.OCPPLocalListVersions.Where(u => u.ChargePointID == chargePointID).Include(u => u.OCPPLocalListItems).Select(u  => new OCPPLocalListDashboardDto{
                ID = u.ID,
                Version = u.Version,
                OCPPLocalListItems = u.OCPPLocalListItems.Select(s => new OCPPLocalListItemDashboardDto{
                    Token = s.Token,
                    TokenType = s.TokenType,
                    TokenStatus = s.TokenStatus
                }).ToList()
            }).FirstOrDefaultAsync();
            return localList;
        }
    }
}

