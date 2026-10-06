using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Ocpp16
{
    public sealed record Ocpp16Outcome<TResponse>(TResponse Response, string? LogResult = null, int? ConnectorId = null);

    /// <summary>
    /// Base of the OCPP 1.6 inbound handlers: deserializes the payload (FormationViolation when unreadable),
    /// serializes the response and writes the MessageLog. Exceptions are left to OCPPRequestHandler (InternalError).
    /// </summary>
    public abstract class Ocpp16HandlerBase<TRequest, TResponse> : IOCPPRequestHandler where TRequest : class
    {
        protected readonly IMessageLogRepository MessageLog;
        protected readonly ILogger Logger;

        protected Ocpp16HandlerBase(IMessageLogRepository messageLog, ILogger logger)
        {
            MessageLog = messageLog;
            Logger = logger;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            TRequest? request = null;
            try
            {
                request = JsonConvert.DeserializeObject<TRequest>(msgIn.JsonPayload ?? string.Empty, OCPPMessageFactory.DefaultSettings);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "{Action} (1.6) => Unreadable payload from {ChargePointId}", msgIn.Action, chargePointStatus.Id);
            }

            if (request == null)
            {
                await MessageLog.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, string.Empty, ErrorCodes.FormationViolation, msgIn, msgOut);
                return ErrorCodes.FormationViolation;
            }

            var outcome = await Process(request, chargePointStatus);
            msgOut.JsonPayload = JsonConvert.SerializeObject(outcome.Response, OCPPMessageFactory.DefaultSettings);
            await MessageLog.SaveLogMessage(chargePointStatus.Id, outcome.ConnectorId, msgIn.Action, outcome.LogResult ?? string.Empty, null!, msgIn, msgOut);
            return null!;
        }

        protected abstract Task<Ocpp16Outcome<TResponse>> Process(TRequest request, ChargePointStatus chargePointStatus);
    }

    /// <summary>
    /// OCPP 1.6 connector N is our EVSE N with a single connector (EvseID = N, ConnectorID = 1). Connectors a 1.6
    /// charger reports that are not configured yet are created with the default pricing (as the 2.0.1 inventory does).
    /// </summary>
    public class Ocpp16ConnectorResolver
    {
        private readonly VoltaXApiDbContext _db;
        private readonly GlobalConfigurations _globalConfigurations;
        private readonly ILogger<Ocpp16ConnectorResolver> _logger;

        public Ocpp16ConnectorResolver(VoltaXApiDbContext db, GlobalConfigurations globalConfigurations, ILogger<Ocpp16ConnectorResolver> logger)
        {
            _db = db;
            _globalConfigurations = globalConfigurations;
            _logger = logger;
        }

        public Task<Connector?> FindAsync(int chargePointDbId, int connectorId) =>
            _db.Connectors.Where(c => c.ChargePointID == chargePointDbId && c.EvseID == connectorId)
                .OrderBy(c => c.ConnectorID).FirstOrDefaultAsync();

        public async Task<Connector?> GetOrCreateAsync(ChargePoint chargePoint, int connectorId)
        {
            if (connectorId <= 0)
                return null;
            var connector = await FindAsync(chargePoint.ID, connectorId);
            if (connector != null)
                return connector;

            // The connector address stays unique across soft deletion: revive a removed one.
            connector = await _db.Connectors.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.ChargePointID == chargePoint.ID && c.EvseID == connectorId && c.ConnectorID == 1);
            if (connector != null)
            {
                connector.IsDeleted = false;
            }
            else
            {
                connector = new Connector
                {
                    ChargePointID = chargePoint.ID,
                    EvseID = connectorId,
                    ConnectorID = 1,
                    PricePerIdleMinute = (double)_globalConfigurations.DefaultIdleTimePricing,
                    PricePerKWh = (double)_globalConfigurations.DefaultPricePerKwh,
                    CostPerKwh = (double)_globalConfigurations.DefaultCostPerKwh,
                    FlatFee = (double)_globalConfigurations.DefaultFlatFee
                };
                _db.Connectors.Add(connector);
            }
            await _db.SaveChangesAsync();
            _logger.LogInformation("OCPP 1.6 connector {ConnectorId} of {ChargePointId} registered (connector row {Id})",
                connectorId, chargePoint.ChargePointId, connector.ID);
            return connector;
        }
    }
}
