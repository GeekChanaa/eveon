using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IConnectorRepository : IRepository<Connector>
    {
      Task<List<ConnectorListDto>> GetChargePointConnectors(int chargePointID);
      Task<List<ConnectorSelectDto>> GetConnectorsIds();
      Task<bool> UpdateConnectorPricing(int connectorID, UpdateConnectorPricingDto updateConnectorPricingDto);
      Task<bool> UpdateConnectorFlatFee(int connectorID, decimal flatFee);
      Task<Connector?> GetConnectorByConnectorIdEvseId(int? connectorId, int evseId, int chargePointID);
      
    }
}