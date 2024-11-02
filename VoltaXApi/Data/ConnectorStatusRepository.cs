using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;

namespace VoltaXApi.Data
{
    public class ConnectorStatusRepository : Repository<ConnectorStatus>, IConnectorStatusRepository
    {

        public ConnectorStatusRepository(VoltaXApiDbContext context) : base(context)
        {
        }
        
        public async Task<int> GetNumberOfConnectorsByStatus(string status)
        {
            return await this._context.ConnectorStatuses.Where(u => u.LastStatus == status).CountAsync();
        }

        public async Task<int> GetPartnerNumberOfConnectorsByStatus(int partnerID,string status)
        {
            return await this._context.ConnectorStatuses.Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID).Where(u => u.LastStatus == status).CountAsync();
        }

        public async Task<bool> UpdateConnectorStatus(int connectorId, string? status, DateTimeOffset? statusTime, ChargePointStatus chargePointStatus)
        {
            try
            {
                    ConnectorStatus? connectorStatus = _context.ConnectorStatuses.Where(u=> u.ChargePointID == chargePointStatus.Id && connectorId == u.ConnectorID).FirstOrDefault();
                    if (connectorStatus == null)
                    {
                        // no matching entry => create connector status
                        connectorStatus = new ConnectorStatus();
                        connectorStatus.ChargePointID = chargePointStatus.Id;
                        connectorStatus.ConnectorID = connectorId;
                        Console.WriteLine("UpdateConnectorStatus => Creating new DB-ConnectorStatus: ID={0} / Connector={1}", connectorStatus.ChargePointID, connectorStatus.ConnectorID);
                        _context.ConnectorStatuses.Add(connectorStatus);
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        connectorStatus.LastStatus = status;
                        connectorStatus.LastStatusTime = ((statusTime.HasValue) ? statusTime.Value : DateTimeOffset.UtcNow).DateTime;
                    }
                    _context.SaveChanges();
                    Console.WriteLine("UpdateConnectorStatus => Save ConnectorStatus: ID={0} / Connector={1} / Status={2}", connectorStatus.ChargePointID, connectorId, status);
                    return true;
                
            }
            catch (Exception exp)
            {
                Console.WriteLine( "UpdateConnectorStatus => Exception writing connector status (ID={0} / Connector={1}): {2}", chargePointStatus?.Id, connectorId, exp.Message);
                Console.WriteLine("INNER EXCEPTION : ");
                Console.WriteLine(exp.StackTrace);
                if(exp.InnerException != null)
                    Console.WriteLine(exp.InnerException.ToString());
            }

            return false;
        }

    }
}