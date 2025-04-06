using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IReportService
    {
        Task HandleReport(CreateReportDto report);
    }
}