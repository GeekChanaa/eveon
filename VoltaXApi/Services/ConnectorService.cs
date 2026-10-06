using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    public class ConnectorService : IConnectorService
    {
        private readonly IConnectorRepository _connectorRepository;
        private readonly IChargePointRepository _chargePointRepository;

        public ConnectorService(IConnectorRepository connectorRepository,
            IChargePointRepository chargePointRepository, GlobalConfigurations globalConfigurations)
        {
            _connectorRepository = connectorRepository;
            _chargePointRepository = chargePointRepository;
        }

        public async Task<List<ConnectorListDto>?> RefreshChargePointConnectors(
            List<ReportDataType>? connectorsToRefresh, string chargePointID)
        {
            ArgumentNullException.ThrowIfNull(connectorsToRefresh);
            // A configuration-only report is not a connector inventory.
            if (connectorsToRefresh.Count == 0) return null;
            if (connectorsToRefresh.Any(r => r.Component?.Evse == null ||
                r.Component.Evse.Id <= 0 || r.Component.Evse.ConnectorId is not > 0))
                throw new ArgumentException("A connector report must contain positive EVSE and connector IDs.");

            var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID)
                ?? throw new InvalidOperationException("Charge point not found.");
            var keys = connectorsToRefresh.Select(r =>
                (EvseID: r.Component.Evse.Id, ConnectorID: r.Component.Evse.ConnectorId!.Value)).Distinct().ToArray();
            await _connectorRepository.ReconcileChargePointConnectors(chargePoint.ID, keys);
            return await _connectorRepository.GetChargePointConnectors(chargePoint.ID);
        }
    }
}
