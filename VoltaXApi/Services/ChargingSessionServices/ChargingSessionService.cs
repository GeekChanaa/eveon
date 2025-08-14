using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    public class ChargingSessionService : IChargingSessionService
    {
        private readonly IChargingSessionRepository _chargingSessionRepository;
        private readonly ICardService _cardService;
        private readonly IConnectorStatusRepository _connectorStatusRepository;
        private readonly GlobalConfigurations _globalConfigurations;


        public ChargingSessionService(
          IChargingSessionRepository chargingSessionRepository,
          IConnectorStatusRepository connectorStatusRepository,
          GlobalConfigurations globalConfigurations,
          ICardService cardService
        )
        {
            _chargingSessionRepository = chargingSessionRepository;
            _connectorStatusRepository = connectorStatusRepository;
            _cardService = cardService;
            _globalConfigurations = globalConfigurations;
        }



        public async Task<ChargingSession> StartChargingSession(Connector connector, Card card, DateTime startDate)
        {
            return await _chargingSessionRepository.CreateChargingSessionForTransaction(connector, card, startDate);
        }

        public async Task<ChargingSession> EndChargingSession(int chargingSessionID, double minutesCharged, DateTime endDate)
        {
            ChargingSession chargingSession = await _chargingSessionRepository.GetByIdAsync(chargingSessionID);
            ConnectorStatus connectorStatus = await _connectorStatusRepository.GetConnectorStatusByConnectorID(chargingSession.ConnectorID);
            chargingSession.ChargedMinutes = minutesCharged;
            chargingSession.EndDate = endDate;
            chargingSession.ChargingSessionStatus = ChargingSessionStatusEnum.CompletedOccupied;
            if (connectorStatus.LastStatus == ConnectorStatusEnumType.Available)
            {
                chargingSession.IdleMinutes = 0;
                chargingSession.EndIdleDate = endDate;
                chargingSession.ChargingSessionStatus = ChargingSessionStatusEnum.Completed;
            }
            await this._chargingSessionRepository.Update(chargingSession);
            return chargingSession;
        }

        public async Task<ChargingSession> HandleIdleMinutes(int connectorID, DateTimeOffset? statusTime)
        {
            ChargingSession chargingSession = await _chargingSessionRepository.GetLastChargingSession(connectorID);
            if (chargingSession != null && chargingSession.IdleMinutes == null)
            {
                double idleMinutes = (statusTime - chargingSession.EndDate)?.TotalMinutes ?? 0;
                chargingSession.IdleMinutes = idleMinutes;
                chargingSession.EndIdleDate = statusTime?.UtcDateTime;
                chargingSession.ChargingSessionStatus = ChargingSessionStatusEnum.Completed;
                await _chargingSessionRepository.Update(chargingSession);
                
                if (_globalConfigurations.GracePeriod < idleMinutes * 60)
                    await _cardService.SubstractAmountFromCardByIdleMinutes(chargingSession.CardID, idleMinutes, connectorID);
            }
            return chargingSession;
        }
    }
}