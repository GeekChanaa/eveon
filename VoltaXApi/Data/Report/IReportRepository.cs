using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaxApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<int> CreateReport(CreateReportDto report);
        Task<ReportDisplayDto> GetReportByID(int reportID);
        IQueryable<ReportListDto> GetAllReports(GlobalParams globalParams);

    }
}