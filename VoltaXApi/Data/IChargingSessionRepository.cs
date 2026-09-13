using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface IChargingSessionRepository : IRepository<ChargingSession>
    {
        IQueryable<ChargePointChargingSessionListDto> GetChargePointChargingSessions(int chargePointID, GlobalParams globalParams);
        Task<ChargingSession> GetLastChargingSession(int connectorID);
        Task<ChargingSessionInformationsDto> GetChargingSessionInformations(int chargingSessionID);
        Task<ChargingSessionForMailDto> GetChargingSessionForMail(int chargingSessionID);
        Task<List<int>> GetChargePointChargingSessionsIDs(int chargePointID);
        Task<int> GetChargePointNbrChargingSessions(int chargePointID);
        Task<int> GetChargePointNbrChargingSessionsToday(int chargePointID);
        Task<Dictionary<DateTime, double>> GetChargePointNbrChargingSessionsLast30Days(int chargePointID);
        IQueryable<ChargingSessionListDto> GetChargingSessions();
        IQueryable<ChargingSessionListDto> GetUserChargingSessions(int userID, GlobalParams globalParams);
        Task<ChargingSessionInformationsDto?> GetUserCurrentChargingSession(int userID, GlobalParams globalParams);
        Task<ChargingSession> CreateChargingSessionForTransaction(Connector connector, Card card, DateTime startDate);
        IQueryable<PartnerChargingSessionListDto> GetPartnerChargingSessions(int partnerID);
        

    }
}