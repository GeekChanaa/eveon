using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface IChargingSessionService
    {
        Task<ChargingSession> StartChargingSession(Connector connector, Card card, DateTime startDate);
        Task<ChargingSession> EndChargingSession(int chargingSessionID, double minutesCharged, DateTime endDate);
        Task<ChargingSession> HandleIdleMinutes(int connectorID, DateTimeOffset? statusTime);
  }
}