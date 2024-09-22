using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IReportRepository : IRepository<Report>
    {
        Task CreateReport(CreateReportDto report);
        Task<ReportDisplayDto> GetReportByID(int reportID);

    }
}