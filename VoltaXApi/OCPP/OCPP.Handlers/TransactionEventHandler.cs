using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class TransactionEventHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ITransactionService _transactionService;
        private readonly IConnectorRepository _connectorRepository;
        private readonly IChargePointRepository _chargePointRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly VoltaXApiDbContext _db;
        private readonly IRoamingTransactionObserver _roamingObserver;
        private readonly ReservationService _reservations;
        private readonly ILogger _logger;

        private readonly VoltaXApi.SmartCharging.ILoadBalancingTrigger _loadBalancing;

        public TransactionEventHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            ITransactionService transactionService,
            IConnectorRepository connectorRepository,
            IChargePointRepository chargePointRepository,
            IServiceScopeFactory scopeFactory,
            VoltaXApiDbContext db,
            IRoamingTransactionObserver roamingObserver,
            ReservationService reservations,
            VoltaXApi.SmartCharging.ILoadBalancingTrigger loadBalancing
        )
        {
            _loadBalancing = loadBalancing;
            _roamingObserver = roamingObserver;
            _reservations = reservations;
            _logger = loggerFactory.CreateLogger(typeof(TransactionEventHandler));
            _msgLogRepo = messageLogRepository;
            _transactionService = transactionService;
            _connectorRepository = connectorRepository;
            _chargePointRepository = chargePointRepository;
            _scopeFactory = scopeFactory;
            _db = db;
        }

        public async Task<string> Handle(
            OCPPMessage msgIn,
            OCPPMessage msgOut,
            ChargePointStatus chargePointStatus
        )
        {
            string errorCode = null!;
            var transactionEventResponse = new TransactionEventResponse
            {
                CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId },
                IdTokenInfo = new IdTokenInfoType()
            };

            int connectorId = 0;

            try
            {
                _logger.LogInformation("TransactionEvent => Processing request from {ChargePointId}", chargePointStatus.Id);

                var transactionEventRequest = JsonConvert.DeserializeObject<TransactionEventRequest>(msgIn.JsonPayload ?? string.Empty);
                if (transactionEventRequest == null)
                {
                    _logger.LogWarning("TransactionEvent => Failed to deserialize request payload from {ChargePointId}", chargePointStatus.Id);
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    var chargePoint = await AcceptedChargePoint(transactionEventRequest, chargePointStatus.Id);
                    if (chargePoint == null)
                    {
                        Refuse(transactionEventRequest, transactionEventResponse);
                    }
                    else if (transactionEventRequest.EVSE == null && transactionEventRequest.EventType == TransactionEventEnumType.Started)
                    {
                        // Authorization before plug-in: the EVSE arrives with a later event.
                        await StartPending(transactionEventRequest, transactionEventResponse, chargePointStatus, chargePoint);
                    }
                    else
                    {
                        var connector = await ResolveConnector(transactionEventRequest, chargePoint);
                        if (connector == null)
                        {
                            await HandleWithoutConnector(transactionEventRequest, transactionEventResponse, chargePoint);
                        }
                        else
                        {
                            connectorId = connector.ID;
                            var roaming = await _roamingObserver.OnTransactionEventAsync(chargePointStatus.Id, transactionEventRequest);
                            if (roaming.HasValue)
                            {
                                transactionEventResponse.IdTokenInfo.Status = roaming.Value;
                            }
                            else
                            {
                                await StartIfPending(transactionEventRequest, chargePointStatus, chargePoint, connector);
                                await Process(transactionEventRequest, transactionEventResponse, chargePointStatus, connector);
                            }
                            await MarkReservationUsed(transactionEventRequest, transactionEventResponse, chargePointStatus.Id);
                        }
                    }

                    msgOut.JsonPayload = JsonConvert.SerializeObject(transactionEventResponse, OCPPMessageFactory.DefaultSettings);
                    _logger.LogInformation("TransactionEvent => Response serialized for {EventType}", transactionEventRequest.EventType);
                }
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "TransactionEvent => Exception processing request from {ChargePointId}", chargePointStatus?.Id);
                errorCode = ErrorCodes.FormationViolation;
            }

            await _msgLogRepo.SaveLogMessage(
                chargePointStatus?.Id ?? string.Empty,
                connectorId,
                msgIn.Action,
                transactionEventResponse.IdTokenInfo?.Status.ToString() ?? string.Empty,
                errorCode ?? string.Empty,
                msgIn,
                msgOut
            );

            return errorCode!;
        }

        private async Task Process(
            TransactionEventRequest request,
            TransactionEventResponse response,
            ChargePointStatus chargePointStatus,
            Connector connector)
        {
            string idTag = request.IdToken != null ? CleanChargeTagId(request.IdToken.IdToken) : string.Empty;

            var meter = MeterValueNormalizer.Extract(request.MeterValue);
            if (meter.UnexpectedUnits.Count > 0)
                _logger.LogWarning("TransactionEvent => Unexpected meter units {Units} from {ChargePointId}; values used unconverted",
                    meter.UnexpectedUnits, chargePointStatus.Id);
            _logger.LogDebug("TransactionEvent => Meter {Energy} kWh, {Power} kW, SoC {SoC}%", meter.EnergyKWh, meter.PowerKW, meter.StateOfCharge);

            switch (request.EventType)
            {
                case TransactionEventEnumType.Started:
                    await _transactionService.StartTransaction(request, response, chargePointStatus, connector, idTag, null, meter.EnergyKWh);
                    break;

                case TransactionEventEnumType.Updated:
                    await _transactionService.UpdateTransaction(request, response, chargePointStatus, connector, idTag, null, meter.EnergyKWh);
                    break;

                case TransactionEventEnumType.Ended:
                    await _transactionService.EndTransaction(request, response, chargePointStatus, connector, idTag, null, meter.EnergyKWh);
                    break;

                default:
                    _logger.LogWarning("TransactionEvent => Unknown event type: {EventType}", request.EventType);
                    break;
            }

            // Station load balancing reacts to sessions starting, stopping and drawing power (debounced, never throws).
            _loadBalancing.RequestRebalanceForChargePoint(chargePointStatus.Id);
        }

        /// <summary>The charge point of the event, or null when it is unknown or not Accepted (the event is then refused).</summary>
        private async Task<ChargePoint?> AcceptedChargePoint(TransactionEventRequest request, string chargePointId)
        {
            var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointId);
            if (chargePoint == null)
            {
                _logger.LogWarning("TransactionEvent => Charge point not found: {ChargePointId}", chargePointId);
                return null;
            }

            if (!await ChargePointRegistration.IsAcceptedAsync(_db, chargePointId))
            {
                _logger.LogWarning("TransactionEvent => Charge point {ChargePointId} is not accepted (pending provisioning); {EventType} of transaction {TransactionId} ignored",
                    chargePointId, request.EventType, request.TransactionInfo?.TransactionId);
                return null;
            }
            return chargePoint;
        }

        /// <summary>
        /// Finds the connector of the event from its EVSE or, when the event has none, from the transaction it belongs to.
        /// Null when it cannot be identified (yet).
        /// </summary>
        private async Task<Connector?> ResolveConnector(TransactionEventRequest request, ChargePoint chargePoint)
        {
            var evse = request.EVSE;
            if (evse == null)
                return await FindConnectorOfTransaction(request.TransactionInfo?.TransactionId, chargePoint.ID);

            var connector = await FindConnector(evse, chargePoint.ID);
            if (connector == null)
            {
                // The device model report arrives later through NotifyReport; this event is refused meanwhile.
                _logger.LogError("TransactionEvent => Connector {EvseId}/{ConnectorId} not found for {ChargePointId}; requesting its device model",
                    evse.Id, evse.ConnectorId, chargePoint.ChargePointId);
                var chargePointId = chargePoint.ChargePointId;
                OcppBackgroundCommand.Run(_scopeFactory, _logger, "RefreshConnectors", chargePointId,
                    services => services.GetRequiredService<IConfigurationService>().RefreshConnectors(chargePointId));
            }

            return connector;
        }

        /// <summary>
        /// Started event without EVSE: the token is checked like a regular start (same answer and customer
        /// notifications), and an accepted transaction waits for its connector without being billed.
        /// </summary>
        private async Task StartPending(TransactionEventRequest request, TransactionEventResponse response, ChargePointStatus chargePointStatus, ChargePoint chargePoint)
        {
            var roaming = await _roamingObserver.OnTransactionEventAsync(chargePointStatus.Id, request);
            if (roaming.HasValue)
            {
                response.IdTokenInfo.Status = roaming.Value;
                await MarkReservationUsed(request, response, chargePointStatus.Id);
                return;
            }

            var idTag = request.IdToken != null ? CleanChargeTagId(request.IdToken.IdToken) : string.Empty;
            var meter = MeterValueNormalizer.Extract(request.MeterValue);
            var data = TransactionEventData.FromTransactionEvent(request, chargePointStatus.Id, idTag, meter.EnergyKWh);
            var status = await _transactionService.StartTransaction(data, null, validateOnly: true) ?? AuthorizationStatusEnumType.Invalid;
            response.IdTokenInfo = request.IdToken != null ? new IdTokenInfoType { Status = status } : null!;
            if (status != AuthorizationStatusEnumType.Accepted)
                return;

            var transactionUid = data.TransactionUid;
            if (!await _db.OcppPendingTransactions.AnyAsync(p => p.ChargePointID == chargePoint.ID && p.TransactionUid == transactionUid))
            {
                _db.OcppPendingTransactions.Add(new OcppPendingTransaction
                {
                    ChargePointID = chargePoint.ID,
                    TransactionUid = transactionUid,
                    IdTag = idTag,
                    Timestamp = request.Timestamp,
                    MeterStartKWh = meter.EnergyKWh,
                    TriggerReason = data.TriggerReason,
                    ReservationId = request.ReservationId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            _logger.LogInformation("TransactionEvent => Transaction {TransactionId} of {ChargePointId} authorized; waiting for its EVSE",
                transactionUid, chargePointStatus.Id);
            await MarkReservationUsed(request, response, chargePointStatus.Id);
        }

        /// <summary>First event carrying the EVSE of a pending transaction: the session starts with the data of its Started event.</summary>
        private async Task StartIfPending(TransactionEventRequest request, ChargePointStatus chargePointStatus, ChargePoint chargePoint, Connector connector)
        {
            var transactionUid = request.TransactionInfo?.TransactionId;
            if (request.EventType == TransactionEventEnumType.Started || string.IsNullOrEmpty(transactionUid))
                return;

            var pending = await _db.OcppPendingTransactions.FirstOrDefaultAsync(p => p.ChargePointID == chargePoint.ID && p.TransactionUid == transactionUid);
            if (pending == null)
                return;

            var status = await _transactionService.StartTransaction(new TransactionEventData
            {
                ChargePointId = chargePointStatus.Id,
                TransactionUid = pending.TransactionUid,
                EventType = TransactionEventEnumType.Started,
                Timestamp = pending.Timestamp,
                IdTag = pending.IdTag,
                MeterKWh = pending.MeterStartKWh,
                TriggerReason = pending.TriggerReason
            }, connector);
            _db.OcppPendingTransactions.Remove(pending);
            await _db.SaveChangesAsync();
            _logger.LogInformation("TransactionEvent => Pending transaction {TransactionId} of {ChargePointId} attached to connector {ConnectorId}: {Status}",
                transactionUid, chargePointStatus.Id, connector.ID, status);
        }

        /// <summary>
        /// Event whose connector is unknown: a transaction still waiting for its EVSE is acknowledged (and dropped
        /// when it ends, nothing having been billed); anything else is refused.
        /// </summary>
        private async Task HandleWithoutConnector(TransactionEventRequest request, TransactionEventResponse response, ChargePoint chargePoint)
        {
            var transactionUid = request.TransactionInfo?.TransactionId;
            var pending = request.EVSE == null && !string.IsNullOrEmpty(transactionUid)
                ? await _db.OcppPendingTransactions.FirstOrDefaultAsync(p => p.ChargePointID == chargePoint.ID && p.TransactionUid == transactionUid)
                : null;
            if (pending == null)
            {
                if (request.EVSE == null)
                    _logger.LogWarning("TransactionEvent => {EventType} without EVSE for unknown transaction {TransactionId} from {ChargePointId}",
                        request.EventType, transactionUid, chargePoint.ChargePointId);
                Refuse(request, response);
                return;
            }

            response.IdTokenInfo = request.IdToken != null ? new IdTokenInfoType { Status = AuthorizationStatusEnumType.Accepted } : null!;
            if (request.EventType == TransactionEventEnumType.Ended)
            {
                _db.OcppPendingTransactions.Remove(pending);
                await _db.SaveChangesAsync();
                _logger.LogInformation("TransactionEvent => Transaction {TransactionId} of {ChargePointId} ended before any EVSE was known; nothing billed",
                    transactionUid, chargePoint.ChargePointId);
            }
        }

        private async Task MarkReservationUsed(TransactionEventRequest request, TransactionEventResponse response, string chargePointId)
        {
            if (request.EventType != TransactionEventEnumType.Started || !request.ReservationId.HasValue
                || response.IdTokenInfo?.Status != AuthorizationStatusEnumType.Accepted)
                return;
            await _reservations.MarkUsedAsync(chargePointId, request.ReservationId.Value, request.TransactionInfo?.TransactionId ?? "");
        }

        private async Task<Connector?> FindConnector(EVSEType evse, int chargePointId)
        {
            if (evse.ConnectorId.HasValue)
                return await _connectorRepository.GetConnectorByConnectorIdEvseId(evse.ConnectorId, evse.Id, chargePointId);

            // EVSE without connector id: the first connector of that EVSE.
            return (await _connectorRepository.FindAsync(c => c.ChargePointID == chargePointId && c.EvseID == evse.Id))
                .OrderBy(c => c.ConnectorID)
                .FirstOrDefault();
        }

        private async Task<Connector?> FindConnectorOfTransaction(string? transactionUid, int chargePointId)
        {
            if (string.IsNullOrWhiteSpace(transactionUid))
                return null;

            var connectorId = await _db.Transactions.AsNoTracking()
                .Where(t => t.Uid == transactionUid && t.Connector!.ChargePointID == chargePointId)
                .OrderByDescending(t => t.ID)
                .Select(t => t.ConnectorID)
                .FirstOrDefaultAsync();

            return connectorId.HasValue ? await _connectorRepository.GetByIdAsync(connectorId.Value) : null;
        }

        /// <summary>
        /// Answers without touching sessions or cards. The charger still gets a valid response so its
        /// transaction message queue is not blocked; a presented token is reported Invalid.
        /// </summary>
        private static void Refuse(TransactionEventRequest request, TransactionEventResponse response)
        {
            response.IdTokenInfo = request.IdToken != null
                ? new IdTokenInfoType { Status = AuthorizationStatusEnumType.Invalid }
                : null!;
        }

        /// <summary>
        /// Clean vendor-specific suffixes from charge tag IDs (e.g., KEBA appends "_serial").
        /// </summary>
        private string CleanChargeTagId(string rawChargeTagId)
        {
            if (string.IsNullOrWhiteSpace(rawChargeTagId))
                return string.Empty;

            int sep = rawChargeTagId.IndexOf('_');
            if (sep >= 0)
            {
                var cleaned = rawChargeTagId[..sep];
                _logger.LogDebug("CleanChargeTagId => '{Raw}' => '{Cleaned}'", rawChargeTagId, cleaned);
                return cleaned;
            }

            return rawChargeTagId;
        }
    }
}
