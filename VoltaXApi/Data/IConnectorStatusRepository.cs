using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IConnectorStatusRepository : IRepository<ConnectorStatus>
    {
        Task<int> GetNumberOfConnectorsByStatus(string status);
    }
}