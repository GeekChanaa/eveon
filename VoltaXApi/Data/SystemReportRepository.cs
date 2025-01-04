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
using AutoMapper;

namespace VoltaXApi.Data
{
    public class SystemReportRepository : Repository<SystemReport>,ISystemReportRepository
    {
        private readonly IMapper _mapper;
        public SystemReportRepository(
            VoltaXApiDbContext context,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<SystemReportDisplayDto> GetSystemReport(int systemReportID)
        {
            var systemReport = await _context.SystemReports
                                .Include(sr => sr.User)
                                .Include(sr => sr.Assigned)
                                .Include(sr => sr.Resolved)
                                .Include(sr => sr.Card)
                                .Include(sr => sr.Connector)
                                .Include(sr => sr.ChargePoint)
                                .FirstOrDefaultAsync(u => u.ID == systemReportID);

            return _mapper.Map<SystemReport, SystemReportDisplayDto>(systemReport);
        }

    }
}

