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
    public class ReportRepository : Repository<Report>,IReportRepository
    {
        private readonly IMapper _mapper;
        public ReportRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task CreateReport(CreateReportDto report)
        {
            Report rep = _mapper.Map<CreateReportDto,Report>(report);
            await base.AddAsync(rep);
        }

        public async Task<ReportDisplayDto> GetReportByID(int reportID)
        {
            Report rep = await _context.Reports.Include(u => u.User).Include(u => u.ChargePoint).Include(u => u.Connector).Where(u => u.ID == reportID).FirstOrDefaultAsync();
            var report = _mapper.Map<Report,ReportDisplayDto>(rep);
            return report;
        }

    }
}

