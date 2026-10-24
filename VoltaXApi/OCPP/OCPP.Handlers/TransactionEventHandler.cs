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
                    else if (transactionEventRequest.EventType == TransactionEventEnumType.Started
                             && (transactionEventRequest.EVSE == null || transactionEventRequest.IdToken == null))
                    {
                        // Authorization before plug-in (no EVSE yet) or plug-in before authorization (no token yet).
                        connectorId = await StartPending(transactionEventRequest, transactionEventResponse, chargePointStatus, chargePoint);
                    }
                    else if (await FindPending(transactionEventRequest, chargePoint) is { } pending)
                    {
                        connectorId = await ContinuePending(pending, transactionEventRequest, transactionEventResponse, chargePointStatus, chargePoint);
                    }
                    else
                    {
                        var connector = await ResolveConnector(transactionEventRequest, chargePoint);
                        if (connector == null)
                        {
                            if (transactionEventRequest.EVSE == null)
                                _logger.LogWarning("TransactionEvent => {EventType} without EVSE for unknown transaction {TransactionId} from {ChargePointId}",
                                    transactionEventRequest.EventType, transactionEventRequest.TransactionInfo?.TransactionId, chargePoint.ChargePointId);
                            Refuse(transactionEventRequest, transactionEventResponse);
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
                                await Process(transactionEventRequest, transactionEventResponse, chargePointStatus, connector);
                            }
                            await MarkReservationUsed(transactionEventRequest, transactionEventResponse, chargePointStatus.Id);
                        }
                    }

                    // idTokenInfo answers the idToken of the request: an event without one gets none, so the charger
                    // never acts on a status ("Accepted" by default, "Invalid" for a start) for a token it did not send.
                    if (transactionEventRequest.IdToken == null)
                        transactionEventResponse.IdTokenInfo = null!;
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
        /// Started event without EVSE (authorization before plug-in) or without idToken (plug-in before authorization,
        /// TxStartPoint EVConnected): a token is checked like a regular start (same answer and customer notifications),
        /// and the transaction waits in <see cref="OcppPendingTransaction"/> for the part it lacks, without being billed.
        /// Returns the connector id (0 when unknown) for the message log.
        /// </summary>
        private async Task<int> StartPending(TransactionEventRequest request, TransactionEventResponse response, ChargePointStatus chargePointStatus, ChargePoint chargePoint)
        {
            var transactionUid = request.TransactionInfo?.TransactionId ?? "";
            if (string.IsNullOrEmpty(transactionUid))
            {
                Refuse(request, response);
                return 0;
            }

            // A Started event sent again after its transaction started (lost CALLRESULT): acknowledged as is.
            if (await _db.Transactions.AnyAsync(t => t.Uid == transactionUid && t.Connector!.ChargePointID == chargePoint.ID))
            {
                _logger.LogInformation("TransactionEvent => Transaction {TransactionId} of {ChargePointId} already started; repeated Started acknowledged",
                    transactionUid, chargePointStatus.Id);
                return 0;
            }

            var roaming = await _roamingObserver.OnTransactionEventAsync(chargePointStatus.Id, request);
            if (roaming.HasValue)
            {
                response.IdTokenInfo.Status = roaming.Value;
                await MarkReservationUsed(request, response, chargePointStatus.Id);
                return 0;
            }

            var connector = request.EVSE != null ? await ResolveConnector(request, chargePoint) : null;
            var idTag = request.IdToken != null ? CleanChargeTagId(request.IdToken.IdToken) : null;
            var meter = MeterValueNormalizer.Extract(request.MeterValue);
            var data = TransactionEventData.FromTransactionEvent(request, chargePointStatus.Id, idTag, meter.EnergyKWh);
            if (request.IdToken != null)
            {
                var status = await _transactionService.StartTransaction(data, null, validateOnly: true) ?? AuthorizationStatusEnumType.Invalid;
                response.IdTokenInfo = new IdTokenInfoType { Status = status };
                if (status != AuthorizationStatusEnumType.Accepted)
                    return connector?.ID ?? 0;
            }

            if (!await _db.OcppPendingTransactions.AnyAsync(p => p.ChargePointID == chargePoint.ID && p.TransactionUid == transactionUid))
            {
                _db.OcppPendingTransactions.Add(new OcppPendingTransaction
                {
                    ChargePointID = chargePoint.ID,
                    TransactionUid = transactionUid,
                    IdTag = string.IsNullOrEmpty(idTag) ? null : idTag,
                    ConnectorID = connector?.ID,
                    Timestamp = request.Timestamp,
                    MeterStartKWh = meter.EnergyKWh,
                    TriggerReason = data.TriggerReason,
                    ReservationId = request.ReservationId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            _logger.LogInformation("TransactionEvent => Transaction {TransactionId} of {ChargePointId} started by the charger; waiting for its {Missing}",
                transactionUid, chargePointStatus.Id, request.IdToken == null ? "authorization" : "EVSE");
            await MarkReservationUsed(request, response, chargePointStatus.Id);
            return connector?.ID ?? 0;
        }

        /// <summary>The pending transaction a later event (Updated, Ended) belongs to, if any.</summary>
        private async Task<OcppPendingTransaction?> FindPending(TransactionEventRequest request, ChargePoint chargePoint)
        {
            var transactionUid = request.TransactionInfo?.TransactionId;
            if (request.EventType == TransactionEventEnumType.Started || string.IsNullOrEmpty(transactionUid))
                return null;
            return await _db.OcppPendingTransactions.FirstOrDefaultAsync(p => p.ChargePointID == chargePoint.ID && p.TransactionUid == transactionUid);
        }

        /// <summary>
        /// An event of a pending transaction. It may bring the missing EVSE or, in an Updated event (triggerReason
        /// Authorized), the missing token, which is checked like a start. Once both are known the session starts and the
        /// event itself is applied: after authorization before plug-in with the Started event's time and meter, after
        /// plug-in before authorization at the authorization (the driver pays from the moment they authorized). A
        /// transaction that ends while still pending is dropped: nothing was billed. Returns the connector id (0 when
        /// unknown) for the message log.
        /// </summary>
        private async Task<int> ContinuePending(OcppPendingTransaction pending, TransactionEventRequest request, TransactionEventResponse response,
            ChargePointStatus chargePointStatus, ChargePoint chargePoint)
        {
            var connector = request.EVSE != null ? await ResolveConnector(request, chargePoint) : null;
            if (connector == null && pending.ConnectorID.HasValue)
                connector = await _connectorRepository.GetByIdAsync(pending.ConnectorID.Value);
            if (connector != null)
                pending.ConnectorID = connector.ID;

            var idTag = request.IdToken != null ? CleanChargeTagId(request.IdToken.IdToken) : null;
            if (request.IdToken != null)
                response.IdTokenInfo = new IdTokenInfoType { Status = AuthorizationStatusEnumType.Accepted };

            if (string.IsNullOrEmpty(pending.IdTag) && !string.IsNullOrEmpty(idTag) && request.EventType == TransactionEventEnumType.Updated)
            {
                var roaming = await _roamingObserver.OnTransactionEventAsync(chargePointStatus.Id, request);
                if (roaming.HasValue)
                {
                    response.IdTokenInfo.Status = roaming.Value;
                    _db.OcppPendingTransactions.Remove(pending);
                    await _db.SaveChangesAsync();
                    return connector?.ID ?? 0;
                }

                var meter = MeterValueNormalizer.Extract(request.MeterValue);
                var status = await _transactionService.StartTransaction(
                    TransactionEventData.FromTransactionEvent(request, chargePointStatus.Id, idTag, meter.EnergyKWh), null, validateOnly: true)
                    ?? AuthorizationStatusEnumType.Invalid;
                response.IdTokenInfo.Status = status;
                if (status != AuthorizationStatusEnumType.Accepted)
                {
                    // Still waiting for a valid token; the charger deauthorizes and ends the transaction.
                    await _db.SaveChangesAsync();
                    return connector?.ID ?? 0;
                }

                pending.IdTag = idTag;
                pending.Timestamp = request.Timestamp;
                pending.MeterStartKWh = meter.EnergyKWh ?? pending.MeterStartKWh;
                pending.TriggerReason = request.TriggerReason.ToString();
                if (pending.ReservationId.HasValue)
                    await _reservations.MarkUsedAsync(chargePointStatus.Id, pending.ReservationId.Value, pending.TransactionUid);
            }

            if (!string.IsNullOrEmpty(pending.IdTag) && connector != null)
            {
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
                _logger.LogInformation("TransactionEvent => Pending transaction {TransactionId} of {ChargePointId} started on connector {ConnectorId}: {Status}",
                    pending.TransactionUid, chargePointStatus.Id, connector.ID, status);
                if (status == AuthorizationStatusEnumType.Accepted)
                    await Process(request, response, chargePointStatus, connector);
                return connector.ID;
            }

            if (request.EventType == TransactionEventEnumType.Ended)
            {
                _db.OcppPendingTransactions.Remove(pending);
                _logger.LogInformation("TransactionEvent => Transaction {TransactionId} of {ChargePointId} ended before it was authorized and plugged in; nothing billed",
                    pending.TransactionUid, chargePoint.ChargePointId);
            }
            await _db.SaveChangesAsync();
            return connector?.ID ?? 0;
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
