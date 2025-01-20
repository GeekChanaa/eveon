using OCPP.Core.Server;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IConnectorStatusRepository : IRepository<ConnectorStatus>
    {
        Task<int> GetNumberOfConnectorsByStatus(string status);
        Task<int> GetPartnerNumberOfConnectorsByStatus(int partnerID,string status);
        Task<ConnectorStatus?> GetConnectorStatusByConnectorID(int connectorID, string chargePointID);
        Task<ConnectorStatus?> GetLastConnectorStatus(int connectorID);
        Task<ConnectorStatusesDto?> GetNumberOfConnectorsByAllStatus();
    }
}