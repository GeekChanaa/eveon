using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface IChargingSessionRepository : IRepository<ChargingSession>
    {
        IQueryable<ChargePointChargingSessionListDto> GetChargePointChargingSessions(int chargePointID);
        Task<ChargingSession> GetLastChargingSession(int connectorID);
    }
}