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
    public class PartnerRepository : Repository<Partner>,IPartnerRepository
    {
        private readonly IMapper _mapper;
        public PartnerRepository(
            VoltaXApiDbContext context,
            IMapper mapper
            ) : base(context)
        {
            _mapper = mapper;
        }

        public IQueryable<PartnerListDto> GetPartners()
        {
            var partners = _context.Partners.AsQueryable().ProjectTo<PartnerListDto>(_mapper.ConfigurationProvider);
            return partners;
        }

    }
}

