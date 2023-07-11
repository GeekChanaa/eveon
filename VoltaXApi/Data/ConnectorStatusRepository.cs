using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

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

    }
}