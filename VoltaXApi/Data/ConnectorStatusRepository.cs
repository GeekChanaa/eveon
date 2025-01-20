using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
  public class ConnectorStatusRepository : Repository<ConnectorStatus>, IConnectorStatusRepository
  {

    public ConnectorStatusRepository(VoltaXApiDbContext context) : base(context)
    {
    }

    public async Task<int> GetNumberOfConnectorsByStatus(string status)
    {
      return await this._context.ConnectorStatuses.Where(u => u.LastStatus.ToString().ToLower() == status.ToLower()).CountAsync();
    }

    public async Task<int> GetPartnerNumberOfConnectorsByStatus(int partnerID, string status)
    {
      return await this._context.ConnectorStatuses.Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID).Where(u => u.LastStatus.ToString().ToLower() == status.ToLower()).CountAsync();
    }


    public async Task<ConnectorStatus?> GetConnectorStatusByConnectorID(int connectorID, string chargePointID)
    {
      return await _context.ConnectorStatuses
                      .Where(u => u.ChargePointID == chargePointID
                                  && connectorID == u.ConnectorID).FirstOrDefaultAsync();
    }


    public async Task<ConnectorStatus?> GetLastConnectorStatus(int connectorID)
    {
      return await _context.ConnectorStatuses
                      .FirstOrDefaultAsync(u => u.ConnectorID == connectorID);
    }

    public async Task<ConnectorStatusesDto?> GetNumberOfConnectorsByAllStatus()
    {
      var statusCounts = await _context.ConnectorStatuses
                .GroupBy(c => c.LastStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var result = new ConnectorStatusesDto
            {
                NbrAvailableConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Available)?.Count ?? 0,
                NbrOccupiedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Occupied)?.Count ?? 0,
                NbrReservedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Reserved)?.Count ?? 0,
                NbrUnavailableConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Unavailable)?.Count ?? 0,
                NbrFaultedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Faulted)?.Count ?? 0
            };

            return result;
    }
  }
}