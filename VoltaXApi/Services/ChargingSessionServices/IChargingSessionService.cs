using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public interface IChargingSessionService
    {
        Task<ChargingSession> StartChargingSession(Connector connector, Card card, DateTime startDate);
        Task<ChargingSession> UpdateChargingSession(int chargingSessionID, double minutesCharged, double chargedKwhs);

        /// <summary>
        /// Ends the session and bills it in one save: the session amounts (see <see cref="ICostCalculator"/>), the
        /// card debit and the wallet movement are committed together with whatever the caller already changed in
        /// the same DbContext (the ended transaction). Ending an already billed session bills nothing again.
        /// </summary>
        Task<ChargingSession> EndChargingSession(int chargingSessionID, double minutesCharged, double chargedKwhs, DateTime endDate);

        /// <summary>Bills the idle time of the last ended session of a connector when the car leaves it.</summary>
        Task<ChargingSession?> HandleIdleMinutes(int connectorID, DateTimeOffset? statusTime);

        /// <summary>Emails the session quote to its owner; failures are logged, never thrown.</summary>
        Task SendSessionQuoteAsync(int chargingSessionID);
    }
}
