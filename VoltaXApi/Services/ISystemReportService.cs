using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using OCPP.Core.Server;

namespace VoltaXApi.Services
{
    public interface ISystemReportService
    {
      Task HandleReport(SystemReport report);
    }
}