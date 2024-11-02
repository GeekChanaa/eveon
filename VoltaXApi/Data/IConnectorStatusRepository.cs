using OCPP.Core.Server;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IConnectorStatusRepository : IRepository<ConnectorStatus>
    {
        Task<int> GetNumberOfConnectorsByStatus(string status);
        Task<int> GetPartnerNumberOfConnectorsByStatus(int partnerID,string status);
        Task<bool> UpdateConnectorStatus(int connectorId, string? status, DateTimeOffset? statusTime, ChargePointStatus chargePointStatus);
    }
}