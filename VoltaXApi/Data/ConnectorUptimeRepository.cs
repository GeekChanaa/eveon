using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;

namespace VoltaXApi.Data
{
    public class ConnectorUptimeRepository : Repository<ConnectorUptime>, IConnectorUptimeRepository
    {
        public ConnectorUptimeRepository(VoltaXApiDbContext context) : base(context)
        {
        }
    }
}