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
using VoltaxApi.Dtos;

namespace VoltaXApi.Data
{
    public class ReportRepository : Repository<Report>,IReportRepository
    {
        private readonly IMapper _mapper;
        public ReportRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<int> CreateReport(CreateReportDto report)
        {
            Report rep = _mapper.Map<CreateReportDto,Report>(report);
            await base.AddAsync(rep);
            return rep.ID;
        }

        public async Task<ReportDisplayDto> GetReportByID(int reportID)
        {
            Report rep = await _context.Reports.Include(u => u.User).Include(u => u.ChargePoint).Include(u => u.Connector).Where(u => u.ID == reportID).FirstOrDefaultAsync();
            var report = _mapper.Map<Report,ReportDisplayDto>(rep);
            return report;
        }
        
        public IQueryable<ReportListDto> GetAllReports(GlobalParams globalParams)
        {
            return GetAllAsync(globalParams).Select(u => new ReportListDto
            {
                ID = u.ID,
                UserName = u.User.FullName,
                ChargePointName = u.ChargePoint.ChargePointId,
                ConnectorName = u.Connector.ConnectorName,
                ReportType = u.ReportType,
                ReportCategory = u.ReportCategory,
                IssueDescription = u.IssueDescription,
                Status = u.Status,
                ReportDate = u.ReportDate,
                ResolvedDate = u.ResolvedDate,
                IsEmail = u.IsEmail,
                IsNotification = u.IsNotification
            }).AsQueryable();
        }


    }
}

