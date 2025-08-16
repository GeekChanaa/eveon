using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IConnectorRepository : IRepository<Connector>
    {
      Task<List<ConnectorListDto>> GetChargePointConnectors(int chargePointID);
      Task<List<ConnectorSelectDto>> GetConnectorsIds();
      Task<List<ConnectorSelectDto>> GetChargePointConnectorIds(int connectorID);
      Task<bool> UpdateConnectorPricing(int connectorID, UpdateConnectorPricingDto updateConnectorPricingDto);
      Task<bool> UpdateConnectorFlatFee(int connectorID, double flatFee);
      Task<Connector?> GetConnectorByConnectorIdEvseId(int? connectorId, int evseId, int chargePointID);
      Task ResetPricingChargePointConnectors(int chargePointID);
      Task ResetPricingConnector(int connectorID);
      Task<List<int>> GetChargePointEvsesIds(int chargePointID);
      
    }
}