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
      return await this._context.ConnectorStatuses.Where(u => u.Connector.ChargePoint.ChargingStation.PartnerID == partnerID).Where(u => u.LastStatus.ToString().ToLower() == status.ToLower()).CountAsync();
    }


    public async Task<ConnectorStatus?> GetConnectorStatusByConnectorID(int connectorID)
    {
      var css = await _context.ConnectorStatuses
        .Where(u => connectorID == u.ConnectorID)
        .Select(u => new { u.LastStatus, u.ConnectorID })
        .FirstOrDefaultAsync();
      var cs = await _context.ConnectorStatuses
                      .Where(u => connectorID == u.ConnectorID).FirstOrDefaultAsync();
      return cs;
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

      var connectorsCount = await _context.Connectors.CountAsync();

      int disconnectedCount = connectorsCount - statusCounts.Count();

      var result = new ConnectorStatusesDto
      {
        NbrAvailableConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Available)?.Count ?? 0,
        NbrOccupiedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Occupied)?.Count ?? 0,
        NbrReservedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Reserved)?.Count ?? 0,
        NbrUnavailableConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Unavailable)?.Count ?? 0,
        NbrFaultedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Faulted)?.Count ?? 0,
        NbrDisconnectedConnectors = disconnectedCount + statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Disconnected)?.Count ?? 0
      };

      return result;
    }
    
    public async Task<ConnectorStatusesDto?> GetNumberOfPartnerConnectorsByAllStatus(int partnerID)
    {
      var statusCounts = await _context.ConnectorStatuses.Where(u => u.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .GroupBy(c => c.LastStatus)
                .Select(g => new
                {
                  Status = g.Key,
                  Count = g.Count()
                })
                .ToListAsync();

      var connectorsCount = await _context.Connectors.Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID).CountAsync();

      int disconnectedCount = connectorsCount - statusCounts.Count() ;

      var result = new ConnectorStatusesDto
      {
        NbrAvailableConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Available)?.Count ?? 0,
        NbrOccupiedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Occupied)?.Count ?? 0,
        NbrReservedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Reserved)?.Count ?? 0,
        NbrUnavailableConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Unavailable)?.Count ?? 0,
        NbrFaultedConnectors = statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Faulted)?.Count ?? 0,
        NbrDisconnectedConnectors = disconnectedCount + statusCounts.FirstOrDefault(s => s.Status == ConnectorStatusEnumType.Disconnected)?.Count ?? 0
      };

      return result;
    }
  }
}