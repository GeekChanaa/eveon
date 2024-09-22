using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IConnectorRepository : IRepository<Connector>
    {
      Task<List<ConnectorListDto>> GetChargePointConnectors(int chargePointID);
      Task<List<ConnectorSelectDto>> GetConnectorsIds();
    }
}