using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Data.Seeders;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Exceptions;
using VoltaXApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.OCPP.Helpers;

namespace VoltaXApi.Services
{
  public class TransactionService : ITransactionService
  {
    private readonly ICardService _cardService;
    private readonly ICardRepository _cardRepository;
    private readonly IChargePointRepository _chargePointRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IChargingSessionRepository _chargingSessionRepository;
    private readonly IChargingSessionService _chargingSessionService;
    private readonly IConnectorStatusRepository _connectorStatusRepository;
    private readonly IConnectorUptimeRepository _connectorUptimeRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ChargingSessionHub> _chargingSessionHub;
    private readonly GlobalConfigurations _globalConfigurations;
    private readonly ILogger<TransactionService> _logger;
    private readonly VoltaXApiDbContext _context;

    public TransactionService(
      ICardService cardService,
      ICardRepository cardRepository,
      IChargePointRepository chargePointRepository,
      ITransactionRepository transactionRepository,
      IChargingSessionRepository chargingSessionRepository,
      IChargingSessionService chargingSessionService,
      IConnectorStatusRepository connectorStatusRepository,
      IConnectorUptimeRepository connectorUptimeRepository,
      IServiceScopeFactory scopeFactory,
      IHubContext<ChargingSessionHub> chargingSessionHub,
      GlobalConfigurations globalConfigurations,
      ILogger<TransactionService> logger,
      VoltaXApiDbContext context
    )
    {
      _cardService = cardService;
      _cardRepository = cardRepository;
      _chargePointRepository = chargePointRepository;
      _transactionRepository = transactionRepository;
      _context = context;
      _chargingSessionRepository = chargingSessionRepository;
      _connectorStatusRepository = connectorStatusRepository;
      _connectorUptimeRepository = connectorUptimeRepository;
      _scopeFactory = scopeFactory;
      _chargingSessionService = chargingSessionService;
      _chargingSessionHub = chargingSessionHub;
      _globalConfigurations = globalConfigurations;
      _logger = logger;
    }

    // OCPP 2.0.1 TransactionEvent entry points.
    public async Task StartTransaction(TransactionEventRequest request, TransactionEventResponse response, ChargePointStatus chargePointStatus,
        Connector connector, string? idTag, string? errorCode, double? meterKWH)
    {
      response.IdTokenInfo.Status = await StartTransaction(TransactionEventData.FromTransactionEvent(request, chargePointStatus.Id, idTag, meterKWH), connector)
          ?? AuthorizationStatusEnumType.Invalid;
    }

    public async Task UpdateTransaction(TransactionEventRequest request, TransactionEventResponse response, ChargePointStatus chargePointStatus,
        Connector connector, string? idTag, string? errorCode, double? meterKWH)
    {
      var status = await UpdateTransaction(TransactionEventData.FromTransactionEvent(request, chargePointStatus.Id, idTag, meterKWH), connector);
      if (status.HasValue)
        response.IdTokenInfo.Status = status.Value;
    }

    public async Task EndTransaction(TransactionEventRequest request, TransactionEventResponse response, ChargePointStatus chargePointStatus,
        Connector connector, string? idTag, string? errorCode, double? meterKWH)
    {
      var status = await EndTransaction(TransactionEventData.FromTransactionEvent(request, chargePointStatus.Id, idTag, meterKWH), connector);
      if (status.HasValue)
        response.IdTokenInfo.Status = status.Value;
    }

    /// <summary>
    /// Starts the card-based session and transaction of a charger transaction (any OCPP version) and returns the
    /// authorization status to answer. With <paramref name="validateOnly"/> only the card checks run (and their
    /// customer notifications): used while the connector of the transaction is not known yet.
    /// </summary>
    public async Task<AuthorizationStatusEnumType?> StartTransaction(TransactionEventData data, Connector? connector, bool validateOnly = false)
    {
      AuthorizationStatusEnumType? status = null;
      string? errorCode = null;
      string? idTag = data.IdTag;
      double? meterKWH = data.MeterKWh;
      Card? card = null;
      ChargingSession? chargingSession = null;
      if (string.IsNullOrWhiteSpace(data.TransactionUid))
      {
        _logger.LogWarning("StartTransaction => Event without transaction id from {ChargePointId} refused", data.ChargePointId);
        return AuthorizationStatusEnumType.Invalid;
      }

      try
      {
        card = await _cardRepository.GetCardByNumber(idTag ?? string.Empty);

        if (card == null)
        {
          _logger.LogWarning("StartTransaction => Card not found for idTag: {IdTag}", idTag);
          status = AuthorizationStatusEnumType.Invalid;
          return status;
        }

        // Check if the card has sufficient balance before starting
        if (card.Balance <= 0)
        {
          _logger.LogWarning("StartTransaction => Card {CardId} has no balance ({Balance}), rejecting start", card.ID, card.Balance);
          status = AuthorizationStatusEnumType.NoCredit;
          await CreateChargingSessionNotification(
              card,
              "ChargingStartRejectedNoCredit",
              "Your charging session could not start because your charging card has no available balance. Please top up your card and try again.",
              data.EventKey(card),
              urgent: true);
          return status;
        }

        // Check if card is blocked
        if (card.Blocked == true)
        {
          _logger.LogWarning("StartTransaction => Card {CardId} is blocked, rejecting start", card.ID);
          status = AuthorizationStatusEnumType.Blocked;
          await CreateChargingSessionNotification(
              card,
              "ChargingStartRejectedBlockedCard",
              "Your charging session could not start because this charging card is blocked. Please contact support if you believe this is a mistake.",
              data.EventKey(card),
              urgent: true);
          return status;
        }

        // Check card expiration
        if (card.ExpirationDate < DateTime.UtcNow)
        {
          _logger.LogWarning("StartTransaction => Card {CardId} is expired ({ExpirationDate}), rejecting start", card.ID, card.ExpirationDate);
          status = AuthorizationStatusEnumType.Expired;
          await CreateChargingSessionNotification(
              card,
              "ChargingStartRejectedExpiredCard",
              $"Your charging session could not start because this charging card expired on {card.ExpirationDate:dd MMM yyyy}.",
              data.EventKey(card),
              urgent: true);
          return status;
        }

        // Without a Transaction.Begin reading the start register is unknown and recorded as 0, as before.
        double meterStart = meterKWH ?? 0;
        if (meterStart < 0)
        {
          _logger.LogWarning("StartTransaction => Invalid starting meter value {MeterKWh} for card {CardId}", meterStart, card.ID);
          status = AuthorizationStatusEnumType.Invalid;
          await CreateChargingSessionNotification(
              card,
              "ChargingStartRejectedInvalidMeter",
              "Your charging session could not start because the charge point reported an invalid meter reading. Please try another connector or contact support.",
              data.EventKey(card),
              urgent: true);
          return status;
        }

        status = await _cardService.ValidateCard(idTag);
        if (status != AuthorizationStatusEnumType.Accepted)
        {
          _logger.LogWarning("StartTransaction => Card validation failed with status: {Status}", status);
          await CreateChargingSessionNotification(
              card,
              "ChargingStartRejectedValidation",
              $"Your charging session could not start because the charging card was rejected ({FormatAuthorizationStatus(status.Value)}).",
              data.EventKey(card),
              urgent: true);
          return status;
        }

        if (validateOnly || connector == null)
          return status;

        if (!TryParseUtc(data.Timestamp, out var startTimestamp))
        {
          _logger.LogWarning("StartTransaction => Invalid timestamp {Timestamp}; server time used", data.Timestamp);
          startTimestamp = DateTime.UtcNow;
        }

        chargingSession = await _chargingSessionService.StartChargingSession(connector, card, startTimestamp);

        try
        {
          var transaction = new Transaction
          {
            Uid = data.TransactionUid,
            ConnectorID = connector.ID,
            StartCardID = card.ID,
            ChargingSessionID = chargingSession.ID,
            StartTime = startTimestamp,
            MeterStart = meterStart,
            Status = TransactionStatusEnum.Current,
            StartResult = data.TriggerReason
          };

          await _transactionRepository.AddAsync(transaction);
          await _connectorUptimeRepository.TransactionStartUptimeHandle(transaction.ID, connector.ID);

          _logger.LogInformation("StartTransaction => Transaction {TransactionUid} started for card {CardId} on connector {ConnectorId}",
              transaction.Uid, card.ID, connector.ID);

          // Notify connected clients about session start
          await NotifySessionUpdate(chargingSession.ID, new ChargingSessionUpdateDto
          {
            SessionId = chargingSession.ID,
            TransactionUid = transaction.Uid,
            Status = "Started",
            EnergyKWh = 0,
            DurationMinutes = 0,
            CurrentCost = 0,
            CardBalance = card.Balance,
            Timestamp = transaction.StartTime
          });
        }
        catch (Exception exp)
        {
          _logger.LogError(exp, "StartTransaction => Failed to create transaction record for {TransactionUid}",
              data.TransactionUid);
          errorCode = ErrorCodes.InternalError;
          await CreateChargingSessionNotification(
              card,
              "ChargingStartProcessingFailed",
              "The charge point accepted your card, but the charging session could not be initialized correctly. No further action is required unless charging actually started; if it did, please contact support.",
              data.EventKey(card),
              urgent: true,
              chargingSessionId: chargingSession?.ID);
        }
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "StartTransaction => Exception for charge point {ChargePointId}", data.ChargePointId);
        status = AuthorizationStatusEnumType.Invalid;
        if (card != null)
        {
          await CreateChargingSessionNotification(
              card,
              "ChargingStartProcessingFailed",
              "Your charging session could not start because the charge point returned an unexpected error. Please try again or use another connector.",
              data.EventKey(card),
              urgent: true,
              chargingSessionId: chargingSession?.ID);
        }
      }
      return status;
    }

    public async Task<AuthorizationStatusEnumType?> UpdateTransaction(TransactionEventData data, Connector connector)
    {
      AuthorizationStatusEnumType? status = null;
      string? errorCode = null;
      string? idTag = data.IdTag;
      double? meterKWH = data.MeterKWh;
      Transaction? transaction = null;
      Card? card = null;
      if (string.IsNullOrWhiteSpace(data.TransactionUid))
      {
        _logger.LogWarning("UpdateTransaction => Event without transaction id from {ChargePointId} refused", data.ChargePointId);
        return AuthorizationStatusEnumType.Invalid;
      }

      try
      {
        ChargePoint chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(data.ChargePointId);

        transaction = await FindActiveTransaction(
            data.TransactionUid,
            chargePoint.ChargePointId,
            data.ChargePointId,
            connector);

        if (transaction == null)
        {
          _logger.LogWarning("UpdateTransaction => No active transaction found for uid={TransactionUid} on connector {ConnectorId}",
              data.TransactionUid, connector?.ID);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          if (!string.IsNullOrWhiteSpace(idTag))
          {
            card = await _cardRepository.GetCardByNumber(idTag);
            if (card != null)
            {
              await CreateChargingSessionNotification(
                  card,
                  "ChargingUpdateWithoutActiveSession",
                  "The charge point sent an update for your card, but no active charging session could be found. Please check that your vehicle is charging as expected.",
                  data.EventKey(card),
                  urgent: true);
            }
          }
          return status;
        }

        card = await _cardRepository.FindCardAsNoTrackingAsync(c => c.ID == transaction.StartCardID);

        if (card == null)
        {
          _logger.LogWarning("UpdateTransaction => Card not found for transaction {TransactionId}", transaction.ID);
          errorCode = ErrorCodes.InternalError;
          return status;
        }

        // An update without an energy reading keeps the last known register value.
        double currentMeter = meterKWH ?? transaction.MeterStop ?? transaction.MeterStart;
        if (currentMeter < 0)
        {
          _logger.LogWarning("UpdateTransaction => Invalid negative meter value {MeterKWh} for transaction {TransactionUid}", currentMeter, transaction.Uid);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          await CreateChargingSessionNotification(
              card,
              "ChargingInvalidMeterReading",
              "Your charging session reported an invalid meter reading. The update was ignored and the session should be checked.",
              data.TransactionKey(transaction),
              urgent: true,
              chargingSessionId: transaction.ChargingSessionID);
          return status;
        }

        if (currentMeter < transaction.MeterStart)
        {
          _logger.LogWarning(
              "UpdateTransaction => Meter rollback for transaction {TransactionUid}: start={MeterStart}, current={MeterKWh}",
              transaction.Uid, transaction.MeterStart, currentMeter);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          await CreateChargingSessionNotification(
              card,
              "ChargingMeterRollback",
              "Your charging session reported a meter value lower than its starting value. The incorrect update was ignored and the charge point should be checked.",
              data.TransactionKey(transaction),
              urgent: true,
              chargingSessionId: transaction.ChargingSessionID);
          return status;
        }

        if (!TryParseUtc(data.Timestamp, out var updateTimestamp) || updateTimestamp < transaction.StartTime)
        {
          _logger.LogWarning(
              "UpdateTransaction => Invalid timestamp {Timestamp} for transaction {TransactionUid} started at {StartTime}",
              data.Timestamp, transaction.Uid, transaction.StartTime);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          await CreateChargingSessionNotification(
              card,
              "ChargingInvalidTimestamp",
              "Your charging session reported an invalid time sequence. The incorrect update was ignored and the charge point should be checked.",
              data.TransactionKey(transaction),
              urgent: true,
              chargingSessionId: transaction.ChargingSessionID);
          return status;
        }

        if (data.AbnormalCondition)
        {
          await CreateChargingSessionNotification(
              card,
              "ChargingAbnormalCondition",
              $"The charge point reported an abnormal condition during your charging session{data.ReasonSuffix}. Please check your vehicle and connector.",
              data.TransactionKey(transaction),
              urgent: true,
              chargingSessionId: transaction.ChargingSessionID);
        }

        transaction.MeterStop = currentMeter;
        await _context.SaveChangesAsync();

          double chargedKwhs = (transaction.MeterStop ?? 0) - transaction.MeterStart;
          double elapsedMinutes = (updateTimestamp - transaction.StartTime).TotalMinutes;
          double currentCost = CostCalculator.Instance.RunningCost(connector, chargedKwhs, elapsedMinutes);
          double remainingBalance = Math.Max(0, card.Balance - currentCost);

          // Update the charging session with latest meter data
          await _chargingSessionService.UpdateChargingSession(
              transaction.ChargingSessionID, elapsedMinutes, chargedKwhs);

          _logger.LogInformation(
              "UpdateTransaction => Transaction {TransactionUid}: {Kwh:0.000} kWh, cost={Cost:0.00}, balance={Balance:0.00}",
              transaction.Uid, chargedKwhs, currentCost, card.Balance);

          // Check if balance is running low — stop charging if insufficient
          if (remainingBalance <= _globalConfigurations.DefaultLowBalanceThresholdBuffer)
          {
            _logger.LogWarning(
                "UpdateTransaction => Low balance detected for card {CardId} (Balance={Balance:0.00}, Cost={Cost:0.00}, Remaining={RemainingBalance:0.00}). Stopping transaction {TransactionUid}.",
                card.ID, card.Balance, currentCost, remainingBalance, transaction.Uid);

            // Persist the alert first so a failed remote-stop request cannot
            // hide the low-balance condition from the customer.
            await CreateLowBalanceNotification(card, transaction, remainingBalance);

            var stopRequest = new RequestStopTransactionRequest
            {
              TransactionId = transaction.Uid!
            };
            OcppBackgroundCommand.Run(_scopeFactory, _logger, "RequestStopTransaction (low balance)", chargePoint.ChargePointId,
                services => services.GetRequiredService<IEVDriverService>().RequestStopTransaction(chargePoint.ChargePointId, stopRequest));

            // Notify client about low balance
            await NotifySessionUpdate(transaction.ChargingSessionID, new ChargingSessionUpdateDto
            {
              SessionId = transaction.ChargingSessionID,
              TransactionUid = transaction.Uid,
              Status = "StoppingLowBalance",
              EnergyKWh = chargedKwhs,
              DurationMinutes = elapsedMinutes,
              CurrentCost = currentCost,
              CardBalance = remainingBalance,
              Timestamp = DateTime.UtcNow
            });

            return status;
          }

          // Notify connected clients about session update
          await NotifySessionUpdate(transaction.ChargingSessionID, new ChargingSessionUpdateDto
          {
            SessionId = transaction.ChargingSessionID,
            TransactionUid = transaction.Uid,
            Status = "Charging",
            EnergyKWh = chargedKwhs,
            DurationMinutes = elapsedMinutes,
            CurrentCost = currentCost,
            CardBalance = remainingBalance,
            Timestamp = DateTime.UtcNow
          });
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "UpdateTransaction => Exception for charge point {ChargePointId}", data.ChargePointId);
        status = AuthorizationStatusEnumType.Invalid;
        if (card != null)
        {
          await CreateChargingSessionNotification(
              card,
              "ChargingUpdateProcessingFailed",
              "An unexpected error occurred while processing an update from your charging session. Please check that charging is continuing normally.",
              transaction != null
                  ? data.TransactionKey(transaction)
                  : data.EventKey(card),
              urgent: true,
              chargingSessionId: transaction?.ChargingSessionID);
        }
      }
      return status;
    }

    public async Task<AuthorizationStatusEnumType?> EndTransaction(TransactionEventData data, Connector connector)
    {
      AuthorizationStatusEnumType? status = null;
      string? errorCode = null;
      string? idTag = data.IdTag;
      double? meterKWH = data.MeterKWh;
      Card? card = null;
      Transaction? transaction = null;
      if (string.IsNullOrWhiteSpace(data.TransactionUid))
      {
        _logger.LogWarning("EndTransaction => Event without transaction id from {ChargePointId} refused", data.ChargePointId);
        return AuthorizationStatusEnumType.Invalid;
      }

      try
      {
        ChargePoint chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(data.ChargePointId);

        int cardTagID = 0;

        if (!string.IsNullOrWhiteSpace(idTag))
        {
          card = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).FirstOrDefault();
          if (card != null)
          {
            cardTagID = card.ID;
            status = await _cardService.ValidateCard(idTag);
          }
          else
          {
            _logger.LogWarning("EndTransaction => Card not found for idTag: {IdTag}", idTag);
            status = AuthorizationStatusEnumType.Accepted;
          }
        }
        else
        {
          status = AuthorizationStatusEnumType.Accepted;
        }

        transaction = await FindActiveTransaction(
            data.TransactionUid,
            chargePoint.ChargePointId,
            data.ChargePointId,
            connector);

        if (transaction == null)
        {
          _logger.LogWarning(
              "EndTransaction => No active transaction found: uid='{TransactionUid}' / chargepoint='{ChargePointId}' / tag={IdTag}",
              data.TransactionUid, data.ChargePointId, idTag);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          if (card != null)
          {
            await CreateChargingSessionNotification(
                card,
                "ChargingEndWithoutActiveSession",
                "The charge point tried to end a charging session for your card, but no active session could be found. Please review your recent charging activity.",
                data.EventKey(card),
                urgent: true);
          }
          return status;
        }

        var startCard = card?.ID == transaction.StartCardID
            ? card
            : (await _cardRepository.FindAsync(c => c.ID == transaction.StartCardID)).FirstOrDefault();
        if (card != null && card.ID != transaction.StartCardID)
        {
          _logger.LogWarning(
              "EndTransaction => Stop card {StopCardId} does not match start card {StartCardId} for transaction {TransactionUid}",
              card.ID, transaction.StartCardID, transaction.Uid);
          if (startCard != null)
          {
            await CreateChargingSessionNotification(
                startCard,
                "ChargingStopCardMismatch",
                "A different charging card was presented when your charging session ended. The session was safely associated with the card that started it.",
                data.TransactionKey(transaction),
                urgent: true,
                chargingSessionId: transaction.ChargingSessionID);
          }
        }

        // A session must always be charged to the card that started it.
        card = startCard ?? card;
        cardTagID = card?.ID ?? 0;

        // An Ended event without an energy reading closes on the last known register value.
        double endMeter = meterKWH ?? transaction.MeterStop ?? transaction.MeterStart;
        _logger.LogInformation("EndTransaction => Meter={MeterKWh:0.000} kWh for transaction {TransactionUid}", endMeter, transaction.Uid);

        bool validEndTimestamp = TryParseUtc(data.Timestamp, out var endTimestamp)
            && endTimestamp >= transaction.StartTime;
        if (!validEndTimestamp)
        {
          endTimestamp = transaction.StartTime;
          if (card != null)
          {
            await CreateChargingSessionNotification(
                card,
                "ChargingInvalidEndTimestamp",
                "Your charging session ended with an invalid time sequence. The duration was corrected to prevent an incorrect charge.",
                data.TransactionKey(transaction),
                urgent: true,
                chargingSessionId: transaction.ChargingSessionID);
          }
        }

        bool validEndMeter = endMeter >= transaction.MeterStart;
        if (!validEndMeter)
        {
          endMeter = transaction.MeterStart;
          if (card != null)
          {
            await CreateChargingSessionNotification(
                card,
                "ChargingInvalidEndMeter",
                "Your charging session ended with an invalid meter reading. The delivered energy was corrected to prevent an incorrect charge.",
                data.TransactionKey(transaction),
                urgent: true,
                chargingSessionId: transaction.ChargingSessionID);
          }
        }

        if (card != null && data.AbnormalStop)
        {
          await CreateChargingSessionNotification(
              card,
              "ChargingEndedAbnormally",
              $"Your charging session ended because the charge point reported an unusual condition{data.ReasonSuffix}. Please check your vehicle and connector before starting another session.",
              data.TransactionKey(transaction),
              urgent: true,
              chargingSessionId: transaction.ChargingSessionID);
        }

        transaction.StopTime = endTimestamp;
        transaction.MeterStop = endMeter;
        transaction.StopCardID = cardTagID;
        transaction.StopReason = data.TriggerReason;
        transaction.Status = TransactionStatusEnum.Ended;

        double minutesCharged = transaction.StopTime.HasValue
            ? (transaction.StopTime.Value - transaction.StartTime).TotalMinutes
            : 0;

        double chargedKwhs = transaction.MeterStop.HasValue
            ? transaction.MeterStop.Value - transaction.MeterStart
            : 0;

        // Deduct amount from card
        if (cardTagID > 0)
          await _cardService.SubstractAmountFromCardByMinutes(cardTagID, minutesCharged, connector.ID);

        await _chargingSessionService.EndChargingSession(
            transaction.ChargingSessionID, minutesCharged, chargedKwhs,
            endTimestamp);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "EndTransaction => Transaction {TransactionUid} ended: {Kwh:0.000} kWh, {Minutes:0.0} minutes",
            transaction.Uid, chargedKwhs, minutesCharged);

        // Shows the billed amount on the charger's display (CostUpdated, 2.0.1 only); never blocks this handler.
        CostUpdatedSender.SendFinalInBackground(_scopeFactory, _logger, chargePoint.ChargePointId, transaction.Uid,
            CostCalculator.Instance.FinalCost(connector, minutesCharged));

        // Reload updated card balance
        double updatedBalance = card?.Balance ?? 0;
        if (card != null)
        {
          await _context.Entry(card).ReloadAsync();
          updatedBalance = card.Balance;

          if (updatedBalance <= _globalConfigurations.DefaultLowBalanceThresholdBuffer)
          {
            await CreateChargingSessionNotification(
                card,
                "LowBalance",
                updatedBalance <= 0
                    ? "Your charging card balance has been exhausted. Please top up before starting another charging session."
                    : $"Your charging card balance is low ({updatedBalance:0.00} MAD remaining). Please top up before your next charging session.",
                $"Transaction:{transaction.ID}",
                urgent: true,
                chargingSessionId: transaction.ChargingSessionID,
                url: "/wallet");
          }
        }

        // Notify connected clients about session end
        double finalCost = CostCalculator.Instance.EnergyCost(connector, chargedKwhs);
        await NotifySessionUpdate(transaction.ChargingSessionID, new ChargingSessionUpdateDto
        {
          SessionId = transaction.ChargingSessionID,
          TransactionUid = transaction.Uid,
          Status = "Ended",
          EnergyKWh = chargedKwhs,
          DurationMinutes = minutesCharged,
          CurrentCost = finalCost,
          CardBalance = updatedBalance,
          StopReason = data.TriggerReason,
          Timestamp = transaction.StopTime ?? DateTime.UtcNow
        });
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "EndTransaction => Exception for charge point {ChargePointId}", data.ChargePointId);
        status = AuthorizationStatusEnumType.Invalid;
        if (card != null)
        {
          await CreateChargingSessionNotification(
              card,
              "ChargingEndProcessingFailed",
              "An unexpected error occurred while closing your charging session. Please review the session and contact support if its duration or amount looks incorrect.",
              transaction != null
                  ? data.TransactionKey(transaction)
                  : data.EventKey(card),
              urgent: true,
              chargingSessionId: transaction?.ChargingSessionID);
        }
      }
      return status;
    }

    #region Private Helpers

    /// <summary>Parses an OCPP timestamp as a UTC instant (a timestamp without offset is taken as UTC).</summary>
    private static bool TryParseUtc(string? timestamp, out DateTime utc) =>
        DateTime.TryParse(timestamp, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out utc);

    /// <summary>
    /// Finds an active (non-closed) transaction by UID first, then falls back to the latest on the connector.
    /// </summary>
    private async Task<Transaction?> FindActiveTransaction(
        string transactionUid,
        string chargePointId,
        string chargePointStatusId,
        Connector? connector)
    {
      // First try: find by transaction UID
      var transaction = (await _transactionRepository
          .FindAsync(t => t.Uid == transactionUid))
          .OrderByDescending(t => t.ID)
          .FirstOrDefault();

      if (transaction != null && !transaction.StopTime.HasValue)
        return transaction;

      if (transaction != null && transaction.StopTime.HasValue)
        _logger.LogDebug("FindActiveTransaction => Transaction {Uid} is already closed", transactionUid);

      // Fallback: find latest open transaction on this connector
      if (connector == null)
      {
        _logger.LogDebug("FindActiveTransaction => No connector provided, cannot find fallback transaction");
        return null;
      }

      transaction = (await _transactionRepository
          .FindAsync(t => t.ConnectorID == connector.ID && !t.StopTime.HasValue))
          .OrderByDescending(t => t.ID)
          .FirstOrDefault();

      if (transaction != null)
      {
        _logger.LogDebug("FindActiveTransaction => Found fallback transaction {TransactionId} on connector {ConnectorId}",
            transaction.ID, connector.ID);
        return transaction;
      }

      _logger.LogDebug("FindActiveTransaction => No active transaction found for connector {ConnectorId}", connector.ID);
      return null;
    }

    /// <summary>
    /// Send a real-time update to all clients subscribed to the given charging session.
    /// </summary>
    private async Task NotifySessionUpdate(int chargingSessionId, ChargingSessionUpdateDto update)
    {
      try
      {
        string groupName = ChargingSessionHub.GetSessionGroupName(chargingSessionId);
        await _chargingSessionHub.Clients.Group(groupName).SendAsync("SessionUpdate", update);
        _logger.LogDebug("NotifySessionUpdate => Sent update to session group {GroupName}", groupName);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "NotifySessionUpdate => Failed to send SignalR update for session {SessionId}", chargingSessionId);
      }
    }

    /// <summary>
    /// Creates one notification for the card owner when a transaction reaches the
    /// configured low-balance threshold. Notification failures must not prevent the
    /// charge point from being stopped.
    /// </summary>
    private async Task CreateLowBalanceNotification(
        Card card,
        Transaction transaction,
        double remainingBalance)
    {
      await CreateChargingSessionNotification(
          card,
          "LowBalance",
          remainingBalance <= 0
              ? "Your charging card balance has been exhausted. The charging session is being stopped."
              : $"Your charging card balance is low ({remainingBalance:0.00} MAD remaining). The charging session is being stopped.",
          $"Transaction:{transaction.ID}",
          urgent: true,
          chargingSessionId: transaction.ChargingSessionID,
          url: "/wallet");
    }

    /// <summary>
    /// Creates a customer-facing alert for an unusual charging-session event.
    /// The action and occurrence key make the alert idempotent when a charge point
    /// retries the same OCPP event.
    /// </summary>
    private async Task CreateChargingSessionNotification(
        Card card,
        string action,
        string description,
        string occurrenceKey,
        bool urgent,
        int? chargingSessionId = null,
        string? url = null)
    {
      if (!card.UserID.HasValue)
      {
        _logger.LogDebug(
            "CreateChargingSessionNotification => Card {CardId} has no owner; {Action} notification skipped",
            card.ID,
            action);
        return;
      }

      try
      {
        bool alreadySent = await _context.Notifications.AnyAsync(notification =>
            notification.ReceiverID == card.UserID.Value &&
            notification.Action == action &&
            notification.ActionOn == occurrenceKey);

        if (alreadySent)
          return;

        int? notificationTypeId = await _context.NotificationTypes
            .Where(type => type.Name == "Transaction")
            .Select(type => (int?)type.ID)
            .FirstOrDefaultAsync();

        _context.Notifications.Add(new Notification
        {
          NotificationTypeID = notificationTypeId,
          ReceiverID = card.UserID.Value,
          Read = false,
          Url = url ?? (chargingSessionId.HasValue
              ? $"/charging-sessions/{chargingSessionId.Value}"
              : "/charging-sessions"),
          Action = action,
          ActionOn = occurrenceKey,
          Description = description,
          Urgent = urgent
        });

        await _context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        _logger.LogWarning(
            ex,
            "CreateChargingSessionNotification => Failed to create {Action} for card {CardId} and occurrence {OccurrenceKey}",
            action,
            card.ID,
            occurrenceKey);
      }
    }

    private static string FormatAuthorizationStatus(AuthorizationStatusEnumType status) => status switch
    {
      AuthorizationStatusEnumType.NoCredit => "no available credit",
      AuthorizationStatusEnumType.NotAllowedTypeEVSE => "card not allowed for this connector type",
      AuthorizationStatusEnumType.NotAtThisLocation => "card not allowed at this location",
      AuthorizationStatusEnumType.NotAtThisTime => "card not allowed at this time",
      AuthorizationStatusEnumType.ConcurrentTx => "another charging session is already active",
      _ => Humanize(status.ToString())
    };

    private static string Humanize(string value) => TransactionEventData.Humanize(value);

    #endregion
  }
}
