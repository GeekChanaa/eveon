using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.OCPP.Messages;
namespace VoltaXApi.Data
{
    public interface IConnectorUptimeRepository : IRepository<ConnectorUptime>
    {
        Task TransactionStartUptimeHandle(int transactionID, int connectorID);
        Task TransactionEndUptimeHandle(int transactionID, int connectorID);
        Task UpdateConnectorUptime(int connectorID, ConnectorStatusEnumType status);
    }
}