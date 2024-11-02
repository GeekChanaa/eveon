using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IMessageLogRepository : IRepository<MessageLog>
    {
        Task<bool> SaveLogMessage(string chargePointId, int? connectorId, string message, string result, string errorCode);
    }
}