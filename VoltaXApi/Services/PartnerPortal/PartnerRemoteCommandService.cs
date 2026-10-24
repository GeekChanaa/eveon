using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Services.PartnerPortal
{
    public enum PartnerResetType
    {
        /// <summary>Restart once idle (2.0.1 OnIdle, 1.6 Soft): running sessions finish first.</summary>
        Soft,
        /// <summary>Restart now (2.0.1 Immediate, 1.6 Hard): running sessions are stopped.</summary>
        Hard
    }

    public sealed class PartnerResetRequest
    {
        [Required]
        public PartnerResetType? Type { get; set; }
    }

    /// <summary>
    /// Outcome of a partner command on a charger the partner owns: refused before anything was sent
    /// (<see cref="Refusal"/>), or the charger's answer. Commands on chargers of other partners return null.
    /// </summary>
    public sealed record PartnerCommandOutcome<T>(T? Answer, string? Refusal) where T : class
    {
        public static PartnerCommandOutcome<T> Refused(string reason) => new(null, reason);
        public static PartnerCommandOutcome<T> Answered(T answer) => new(answer, null);
    }

    /// <summary>
    /// Remote actions of the partner portal on the partner's own chargers: reset, unlock a connector, stop a running
    /// session, ask for the connectors' status. Ownership is checked here (the charge point's station belongs to the
    /// partner) before anything is sent; the commands go through the regular OCPP services, so 1.6 and 2.0.1
    /// chargers are both handled. Every attempt is written to the audit log with its outcome.
    /// </summary>
    public sealed class PartnerRemoteCommandService
    {
        public const string NotRunning = "NotRunning";

        private readonly VoltaXApiDbContext _db;
        private readonly IConfigurationService _configuration;
        private readonly IEVDriverService _driver;
        private readonly IAuditLogger _audit;

        public PartnerRemoteCommandService(VoltaXApiDbContext db, IConfigurationService configuration, IEVDriverService driver, IAuditLogger audit)
        {
            _db = db;
            _configuration = configuration;
            _driver = driver;
            _audit = audit;
        }

        public async Task<PartnerCommandOutcome<ResetResponse>?> ResetAsync(int partnerId, int chargePointId, PartnerResetType type, CancellationToken ct = default)
        {
            var ocppId = await OwnedChargePoint(partnerId, chargePointId, ct);
            if (ocppId == null)
                return null;
            var request = new ResetRequest { Type = type == PartnerResetType.Hard ? ResetEnumType.Immediate : ResetEnumType.OnIdle };
            return await Send("PartnerPortal.Reset", ocppId, new { type = type.ToString() },
                () => _configuration.Reset(ocppId, request, ct), r => r.Status.ToString());
        }

        /// <summary>Asks the charger to send a StatusNotification for each connector (refreshes the states shown).</summary>
        public async Task<PartnerCommandOutcome<TriggerMessageResponse>?> RefreshStatusAsync(int partnerId, int chargePointId, CancellationToken ct = default)
        {
            var ocppId = await OwnedChargePoint(partnerId, chargePointId, ct);
            if (ocppId == null)
                return null;
            var request = new TriggerMessageRequest { RequestedMessage = MessageTriggerEnumType.StatusNotification, Evse = null! };
            return await Send("PartnerPortal.TriggerStatus", ocppId, null,
                () => _configuration.TriggerMessage(ocppId, request, ct), r => r.Status.ToString());
        }

        public async Task<PartnerCommandOutcome<UnlockConnectorResponse>?> UnlockAsync(int partnerId, int connectorId, CancellationToken ct = default)
        {
            var connector = await _db.Connectors.AsNoTracking()
                .Where(c => c.ID == connectorId && !c.IsDeleted && !c.ChargePoint!.IsDeleted && c.ChargePoint.ChargingStation!.PartnerID == partnerId)
                .Select(c => new { c.ID, c.EvseID, c.ConnectorID, OcppId = c.ChargePoint!.ChargePointId })
                .FirstOrDefaultAsync(ct);
            if (connector == null)
                return null;
            var request = new UnlockConnectorRequest { EvseId = connector.EvseID, ConnectorId = connector.ConnectorID ?? 1 };
            return await Send("PartnerPortal.UnlockConnector", connector.OcppId, new { connectorId = connector.ID, evseId = connector.EvseID, connector = connector.ConnectorID },
                () => _driver.UnlockConnector(connector.OcppId, request, ct), r => r.Status.ToString());
        }

        /// <summary>Stops a running session on one of the partner's chargers (the driver is billed what was charged).</summary>
        public async Task<PartnerCommandOutcome<RequestStopTransactionResponse>?> StopSessionAsync(int partnerId, int sessionId, CancellationToken ct = default)
        {
            var session = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                .Where(s => s.ID == sessionId && !s.IsDeleted && s.Connector!.ChargePoint!.ChargingStation!.PartnerID == partnerId)
                .Select(s => new { s.ID, s.EndDate, OcppId = s.Connector!.ChargePoint!.ChargePointId })
                .FirstOrDefaultAsync(ct);
            if (session == null)
                return null;
            var transactionUid = session.EndDate != null ? null : await _db.Transactions.AsNoTracking()
                .Where(t => t.ChargingSessionID == session.ID && t.StopTime == null && t.Status == TransactionStatusEnum.Current && t.Uid != null)
                .OrderByDescending(t => t.ID).Select(t => t.Uid).FirstOrDefaultAsync(ct);
            if (transactionUid == null)
                return PartnerCommandOutcome<RequestStopTransactionResponse>.Refused(NotRunning);
            var request = new RequestStopTransactionRequest { TransactionId = transactionUid };
            return await Send("PartnerPortal.StopSession", session.OcppId, new { sessionId = session.ID, transactionId = transactionUid },
                () => _driver.RequestStopTransaction(session.OcppId, request, ct), r => r.Status.ToString());
        }

        /// <summary>The OCPP id of the partner's charge point, or null when it is not theirs (or deleted).</summary>
        private Task<string?> OwnedChargePoint(int partnerId, int chargePointId, CancellationToken ct) =>
            _db.ChargePoints.AsNoTracking()
                .Where(cp => cp.ID == chargePointId && !cp.IsDeleted && cp.ChargingStation!.PartnerID == partnerId)
                .Select(cp => cp.ChargePointId)
                .FirstOrDefaultAsync(ct);

        /// <summary>Sends the command and audits it with the charger's answer, or with the failure (not connected, timeout...).</summary>
        private async Task<PartnerCommandOutcome<T>> Send<T>(string action, string ocppId, object? details, Func<Task<T>> send, Func<T, string> status) where T : class
        {
            try
            {
                var answer = await send();
                await _audit.LogAsync(action, nameof(ChargePoint), ocppId, new { request = details, status = status(answer) });
                return PartnerCommandOutcome<T>.Answered(answer);
            }
            catch (Exception ex)
            {
                await _audit.LogAsync(action, nameof(ChargePoint), ocppId, new { request = details, failure = ex.GetType().Name });
                throw;
            }
        }
    }
}
