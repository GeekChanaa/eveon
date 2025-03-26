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
    public class OCPPLocalListItemRepository : Repository<OCPPLocalListItem>,IOCPPLocalListItemRepository
    {
        private readonly IMapper _mapper;
        public OCPPLocalListItemRepository(
            VoltaXApiDbContext context,
            IMapper mapper
            ) : base(context)
        {
            _mapper = mapper;
        }
    }
}

