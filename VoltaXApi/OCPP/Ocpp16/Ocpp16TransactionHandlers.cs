using VoltaXApi.SmartCharging;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Ocpi.Services;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Ocpp16
{
    public class Authorize16Handler : Ocpp16HandlerBase<Authorize16Request, Authorize16Response>
    {
        private readonly IdTokenAuthorizationService _authorization;

        public Authorize16Handler(IMessageLogRepository messageLog, ILogger<Authorize16Handler> logger, IdTokenAuthorizationService authorization)
            : base(messageLog, logger)
        {
            _authorization = authorization;
        }

        protected override async Task<Ocpp16Outcome<Authorize16Response>> Process(Authorize16Request request, ChargePointStatus chargePointStatus)
        {
            var status = Ocpp16Mapping.ToIdTagStatus(await _authorization.AuthorizeAsync(request.IdTag, chargePointStatus.Id));
            Logger.LogInformation("Authorize (1.6) => {ChargePointId} token status {Status}", chargePointStatus.Id, status);
            return new(new Authorize16Response { IdTagInfo = new IdTagInfo { Status = status } }, $"'{request.IdTag}'=>{status}");
        }
    }

    /// <summary>
    /// Shared plumbing of the 1.6 transaction messages: the transactionId table, roaming (OCPI) observation and the
    /// version-neutral card pipeline of <see cref="ITransactionService"/>.
    /// </summary>
    public abstract class Ocpp16TransactionHandlerBase<TRequest, TResponse> : Ocpp16HandlerBase<TRequest, TResponse> where TRequest : class
    {
        protected readonly VoltaXApiDbContext Db;
        protected readonly ITransactionService Transactions;
        protected readonly IRoamingTransactionObserver Roaming;
        protected readonly Ocpp16ConnectorResolver Connectors;
        // Station load balancing reacts to sessions starting, stopping and drawing power (debounced, never throws).
        protected readonly ILoadBalancingTrigger LoadBalancing;

        protected Ocpp16TransactionHandlerBase(IMessageLogRepository messageLog, ILogger logger, VoltaXApiDbContext db,
            ITransactionService transactions, IRoamingTransactionObserver roaming, Ocpp16ConnectorResolver connectors,
            ILoadBalancingTrigger loadBalancing)
            : base(messageLog, logger)
        {
            LoadBalancing = loadBalancing;
            Db = db;
            Transactions = transactions;
            Roaming = roaming;
            Connectors = connectors;
        }

        protected Task<ChargePoint?> FindChargePoint(string chargePointId) =>
            Db.ChargePoints.AsNoTracking().FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId);

        protected Task<Ocpp16Transaction?> FindTransaction(int chargePointDbId, int transactionId) =>
            Db.Ocpp16Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.ChargePointID == chargePointDbId);

        protected static void UpdateOnlineConnector(ChargePointStatus chargePointStatus, int connectorId, MeterReading meter)
        {
            if (connectorId <= 0 || (!meter.EnergyKWh.HasValue && !meter.PowerKW.HasValue && !meter.StateOfCharge.HasValue))
                return;
            if (!chargePointStatus.OnlineConnectors.TryGetValue(connectorId, out var online))
            {
                online = new OnlineConnectorStatus();
                chargePointStatus.OnlineConnectors[connectorId] = online;
            }
            if (meter.PowerKW.HasValue) online.ChargeRateKW = meter.PowerKW;
            if (meter.EnergyKWh.HasValue) online.MeterKWH = meter.EnergyKWh;
            if (meter.StateOfCharge.HasValue) online.SoC = meter.StateOfCharge;
        }
    }

    /// <summary>
    /// 1.6 StartTransaction: a transactionId is always issued (the charger needs one even when refused); the idTag is
    /// then accepted or refused with the same rules as a 2.0.1 Started event.
    /// </summary>
    public class StartTransaction16Handler : Ocpp16TransactionHandlerBase<StartTransaction16Request, StartTransaction16Response>
    {
        private readonly ReservationService _reservations;

        public StartTransaction16Handler(IMessageLogRepository messageLog, ILogger<StartTransaction16Handler> logger, VoltaXApiDbContext db,
            ITransactionService transactions, IRoamingTransactionObserver roaming, Ocpp16ConnectorResolver connectors, ReservationService reservations,
            ILoadBalancingTrigger loadBalancing)
            : base(messageLog, logger, db, transactions, roaming, connectors, loadBalancing)
        {
            _reservations = reservations;
        }

        protected override async Task<Ocpp16Outcome<StartTransaction16Response>> Process(StartTransaction16Request request, ChargePointStatus chargePointStatus)
        {
            var timestamp = Ocpp16Mapping.ToUtc(request.Timestamp);
            var chargePoint = await FindChargePoint(chargePointStatus.Id);
            var transaction = new Ocpp16Transaction
            {
                ChargePointID = chargePoint?.ID ?? 0,
                ConnectorId = request.ConnectorId,
                IdTag = request.IdTag,
                ReservationId = request.ReservationId,
                MeterStartWh = request.MeterStart,
                StartTimestamp = timestamp,
                CreatedAt = DateTime.UtcNow
            };
            Db.Ocpp16Transactions.Add(transaction);
            await Db.SaveChangesAsync();

            var status = await Authorize(request, chargePointStatus, chargePoint, transaction, timestamp);
            transaction.AuthorizationStatus = Ocpp16Mapping.ToIdTagStatus(status);
            await Db.SaveChangesAsync();

            if (status == AuthorizationStatusEnumType.Accepted && request.ReservationId.HasValue)
                await _reservations.MarkUsedAsync(chargePointStatus.Id, request.ReservationId.Value, transaction.Uid);

            Logger.LogInformation("StartTransaction (1.6) => {ChargePointId} connector {ConnectorId} transaction {TransactionId}: {Status}",
                chargePointStatus.Id, request.ConnectorId, transaction.Id, transaction.AuthorizationStatus);
            return new(new StartTransaction16Response
            {
                TransactionId = transaction.Id,
                IdTagInfo = new IdTagInfo { Status = transaction.AuthorizationStatus }
            }, $"'{request.IdTag}'=>{transaction.AuthorizationStatus} (transaction {transaction.Id})", request.ConnectorId);
        }

        private async Task<AuthorizationStatusEnumType> Authorize(StartTransaction16Request request, ChargePointStatus chargePointStatus,
            ChargePoint? chargePoint, Ocpp16Transaction transaction, DateTime timestamp)
        {
            if (chargePoint == null || !await ChargePointRegistration.IsAcceptedAsync(Db, chargePointStatus.Id))
            {
                Logger.LogWarning("StartTransaction (1.6) => Charge point {ChargePointId} is unknown or not accepted; transaction {TransactionId} refused",
                    chargePointStatus.Id, transaction.Id);
                return AuthorizationStatusEnumType.Invalid;
            }

            var connector = await Connectors.GetOrCreateAsync(chargePoint, request.ConnectorId);
            if (connector == null)
            {
                Logger.LogWarning("StartTransaction (1.6) => Invalid connector {ConnectorId} from {ChargePointId}", request.ConnectorId, chargePointStatus.Id);
                return AuthorizationStatusEnumType.Invalid;
            }

            UpdateOnlineConnector(chargePointStatus, request.ConnectorId, new MeterReading(request.MeterStart / 1000.0, null, null, timestamp));

            var roaming = await Roaming.OnTransactionEventAsync(chargePointStatus.Id, new RoamingTransactionEvent(
                TransactionEventEnumType.Started, transaction.Uid, request.IdTag, request.ConnectorId, request.MeterStart, timestamp));
            if (roaming.HasValue)
                return roaming.Value;

            LoadBalancing.RequestRebalanceForChargePoint(chargePointStatus.Id);
            return await Transactions.StartTransaction(new TransactionEventData
            {
                ChargePointId = chargePointStatus.Id,
                TransactionUid = transaction.Uid,
                EventType = TransactionEventEnumType.Started,
                Timestamp = Ocpp16Mapping.ToTimestamp(timestamp),
                IdTag = request.IdTag,
                MeterKWh = request.MeterStart / 1000.0,
                TriggerReason = "StartTransaction"
            }, connector) ?? AuthorizationStatusEnumType.Invalid;
        }
    }

    /// <summary>
    /// 1.6 StopTransaction: closes and bills the session on meterStop exactly like a 2.0.1 Ended event. The
    /// transactionData readings only fill in the online connector state.
    /// </summary>
    public class StopTransaction16Handler : Ocpp16TransactionHandlerBase<StopTransaction16Request, StopTransaction16Response>
    {
        private readonly IdTokenAuthorizationService _authorization;

        public StopTransaction16Handler(IMessageLogRepository messageLog, ILogger<StopTransaction16Handler> logger, VoltaXApiDbContext db,
            ITransactionService transactions, IRoamingTransactionObserver roaming, Ocpp16ConnectorResolver connectors, IdTokenAuthorizationService authorization,
            ILoadBalancingTrigger loadBalancing)
            : base(messageLog, logger, db, transactions, roaming, connectors, loadBalancing)
        {
            _authorization = authorization;
        }

        protected override async Task<Ocpp16Outcome<StopTransaction16Response>> Process(StopTransaction16Request request, ChargePointStatus chargePointStatus)
        {
            var timestamp = Ocpp16Mapping.ToUtc(request.Timestamp);
            var chargePoint = await FindChargePoint(chargePointStatus.Id);
            var transaction = chargePoint == null ? null : await FindTransaction(chargePoint.ID, request.TransactionId);
            var idTag = string.IsNullOrWhiteSpace(request.IdTag) ? null : request.IdTag;

            if (transaction == null || transaction.StopTimestamp.HasValue)
            {
                // Always acknowledged so the charger's transaction queue moves on.
                Logger.LogWarning("StopTransaction (1.6) => Unknown or already stopped transaction {TransactionId} from {ChargePointId}",
                    request.TransactionId, chargePointStatus.Id);
                var unknownStatus = idTag == null ? null : Ocpp16Mapping.ToIdTagStatus(await _authorization.AuthorizeAsync(idTag, chargePointStatus.Id));
                return new(new StopTransaction16Response { IdTagInfo = unknownStatus == null ? null : new IdTagInfo { Status = unknownStatus } },
                    $"Unknown transaction {request.TransactionId}");
            }

            var meter = MeterValueNormalizer.Extract(Ocpp16Mapping.ToMeterValues(request.TransactionData));
            UpdateOnlineConnector(chargePointStatus, transaction.ConnectorId, meter with { EnergyKWh = request.MeterStop / 1000.0 });

            AuthorizationStatusEnumType? status;
            var roaming = await Roaming.OnTransactionEventAsync(chargePointStatus.Id, new RoamingTransactionEvent(
                TransactionEventEnumType.Ended, transaction.Uid, idTag, transaction.ConnectorId, request.MeterStop, timestamp));
            if (roaming.HasValue)
            {
                status = roaming.Value;
            }
            else
            {
                var connector = await Connectors.FindAsync(chargePoint!.ID, transaction.ConnectorId);
                if (connector == null || transaction.AuthorizationStatus != Ocpp16AuthorizationStatus.Accepted)
                {
                    Logger.LogWarning("StopTransaction (1.6) => Transaction {TransactionId} of {ChargePointId} was never started (connector {ConnectorId}, status {Status}); nothing billed",
                        transaction.Id, chargePointStatus.Id, transaction.ConnectorId, transaction.AuthorizationStatus);
                    status = idTag == null ? null : await _authorization.AuthorizeAsync(idTag, chargePointStatus.Id);
                }
                else
                {
                    status = await Transactions.EndTransaction(new TransactionEventData
                    {
                        ChargePointId = chargePointStatus.Id,
                        TransactionUid = transaction.Uid,
                        EventType = TransactionEventEnumType.Ended,
                        Timestamp = Ocpp16Mapping.ToTimestamp(timestamp),
                        IdTag = idTag,
                        MeterKWh = request.MeterStop / 1000.0,
                        TriggerReason = request.Reason ?? "Local",
                        AbnormalStop = Ocpp16Mapping.IsAbnormalStopReason(request.Reason),
                        ReasonSuffix = TransactionEventData.FormatReason(request.Reason)
                    }, connector);
                }
            }

            transaction.MeterStopWh = request.MeterStop;
            transaction.StopTimestamp = timestamp;
            transaction.StopReason = request.Reason ?? "Local";
            await Db.SaveChangesAsync();

            Logger.LogInformation("StopTransaction (1.6) => {ChargePointId} transaction {TransactionId} stopped ({Reason}), {Energy} Wh",
                chargePointStatus.Id, transaction.Id, transaction.StopReason, request.MeterStop - transaction.MeterStartWh);
            var idTagStatus = idTag == null || !status.HasValue ? null : Ocpp16Mapping.ToIdTagStatus(status.Value);
            LoadBalancing.RequestRebalanceForChargePoint(chargePointStatus.Id);
            return new(new StopTransaction16Response { IdTagInfo = idTagStatus == null ? null : new IdTagInfo { Status = idTagStatus } },
                $"Transaction {transaction.Id} stopped: {transaction.StopReason}", transaction.ConnectorId);
        }
    }

    /// <summary>1.6 MeterValues: live connector readings and, for a transaction, the same running update as a 2.0.1 Updated event.</summary>
    public class MeterValues16Handler : Ocpp16TransactionHandlerBase<MeterValues16Request, Empty16Response>
    {
        public MeterValues16Handler(IMessageLogRepository messageLog, ILogger<MeterValues16Handler> logger, VoltaXApiDbContext db,
            ITransactionService transactions, IRoamingTransactionObserver roaming, Ocpp16ConnectorResolver connectors,
            ILoadBalancingTrigger loadBalancing)
            : base(messageLog, logger, db, transactions, roaming, connectors, loadBalancing)
        {
        }

        protected override async Task<Ocpp16Outcome<Empty16Response>> Process(MeterValues16Request request, ChargePointStatus chargePointStatus)
        {
            var meterValues = Ocpp16Mapping.ToMeterValues(request.MeterValue);
            var meter = MeterValueNormalizer.Extract(meterValues);
            if (meter.UnexpectedUnits.Count > 0)
                Logger.LogWarning("MeterValues (1.6) => Unexpected meter units {Units} from {ChargePointId}; values used unconverted", meter.UnexpectedUnits, chargePointStatus.Id);
            UpdateOnlineConnector(chargePointStatus, request.ConnectorId, meter);
            var logResult = $"Meter (kWh): {meter.EnergyKWh} | Charge (kW): {meter.PowerKW} | SoC (%): {meter.StateOfCharge}";

            if (!request.TransactionId.HasValue || !meter.EnergyKWh.HasValue)
                return new(new Empty16Response(), logResult, request.ConnectorId);

            var chargePoint = await FindChargePoint(chargePointStatus.Id);
            var transaction = chargePoint == null ? null : await FindTransaction(chargePoint.ID, request.TransactionId.Value);
            if (transaction == null || transaction.StopTimestamp.HasValue || transaction.AuthorizationStatus != Ocpp16AuthorizationStatus.Accepted)
            {
                Logger.LogWarning("MeterValues (1.6) => Transaction {TransactionId} of {ChargePointId} is not running; readings not applied",
                    request.TransactionId, chargePointStatus.Id);
                return new(new Empty16Response(), logResult, request.ConnectorId);
            }

            var timestamp = meter.EnergyTimestamp ?? meterValues.Select(m => (DateTime?)m.Timestamp).Max() ?? DateTime.UtcNow;
            var roaming = await Roaming.OnTransactionEventAsync(chargePointStatus.Id, new RoamingTransactionEvent(
                TransactionEventEnumType.Updated, transaction.Uid, null, transaction.ConnectorId, meter.EnergyKWh * 1000.0, timestamp));
            if (!roaming.HasValue)
            {
                var connector = await Connectors.FindAsync(chargePoint!.ID, transaction.ConnectorId);
                if (connector != null)
                    await Transactions.UpdateTransaction(new TransactionEventData
                    {
                        ChargePointId = chargePointStatus.Id,
                        TransactionUid = transaction.Uid,
                        EventType = TransactionEventEnumType.Updated,
                        Timestamp = Ocpp16Mapping.ToTimestamp(timestamp),
                        MeterKWh = meter.EnergyKWh,
                        TriggerReason = "MeterValuePeriodic"
                    }, connector);
            }
            LoadBalancing.RequestRebalanceForChargePoint(chargePointStatus.Id);
            return new(new Empty16Response(), logResult, request.ConnectorId);
        }
    }
}
