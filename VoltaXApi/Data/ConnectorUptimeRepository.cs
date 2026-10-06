using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public class ConnectorUptimeRepository : Repository<ConnectorUptime>, IConnectorUptimeRepository
    {
        public ConnectorUptimeRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task TransactionStartUptimeHandle(int transactionID, int connectorID)
        {
            var lastUptime = await _context.ConnectorUptimes.Where(cu => cu.ConnectorID == connectorID)
                                                      .OrderByDescending(cu => cu.StartDate)
                                                      .FirstOrDefaultAsync();

            if(lastUptime.ConnectorUptimeStatus == ConnectorUptimeStatusEnum.Charging)
            {
                lastUptime.TransactionID = transactionID;
                await _context.SaveChangesAsync();
            }
            else
            {
                lastUptime.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                ConnectorUptime newUptime = new (){
                    TransactionID = transactionID,
                    ConnectorID = connectorID,
                    ConnectorUptimeStatus = ConnectorUptimeStatusEnum.Charging,
                    StartDate = DateTime.UtcNow
                };
                await this.AddAsync(newUptime);
            }
        }

        public async Task TransactionEndUptimeHandle(int transactionID, int connectorID)
        {
            var lastUptime = await _context.ConnectorUptimes.Where(cu => cu.ConnectorID == connectorID)
                                                      .OrderByDescending(cu => cu.StartDate)
                                                      .FirstOrDefaultAsync();

            if(lastUptime.ConnectorUptimeStatus == ConnectorUptimeStatusEnum.Charging)
            {
                lastUptime.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                ConnectorUptime newUptime = new (){
                    TransactionID = transactionID,
                    ConnectorID = connectorID,
                    ConnectorUptimeStatus = ConnectorUptimeStatusEnum.SuspendedEV,
                    StartDate = DateTime.UtcNow
                };
                await this.AddAsync(newUptime);
            }
        }

        public async Task UpdateConnectorUptime(int connectorID, ConnectorStatusEnumType status)
        {
            var lastUptime = await _context.ConnectorUptimes.Where( u => u.ConnectorID == connectorID)
                                                        .OrderByDescending(u => u.StartDate)
                                                        .FirstOrDefaultAsync();
            if(lastUptime == null)
            {
                ConnectorUptime connectorUptime = new ()
                {
                    ConnectorID = connectorID,
                    StartDate = DateTime.UtcNow,
                    TransactionID = null,
                    ConnectorUptimeStatus = ConnectorStatusHelper.ConvertConnectorStatustoConnectorUptimeStatus(status)
                };
                await AddAsync(connectorUptime);
            }
            else if(lastUptime != null && lastUptime.ConnectorUptimeStatus != ConnectorStatusHelper.ConvertConnectorStatustoConnectorUptimeStatus(status))
            {
                lastUptime.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                ConnectorUptime connectorUptime = new ()
                {
                    ConnectorID = connectorID,
                    StartDate = DateTime.UtcNow,
                    TransactionID = null,
                    ConnectorUptimeStatus = ConnectorStatusHelper.ConvertConnectorStatustoConnectorUptimeStatus(status)
                };
                await AddAsync(connectorUptime);
            }
        }


        
    }
}