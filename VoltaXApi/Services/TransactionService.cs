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

namespace VoltaXApi.Services
{
  public class TransactionService : ITransactionService
  {
    private const double LowBalanceThresholdBuffer = 5.0;

    private readonly ICardService _cardService;
    private readonly ICardRepository _cardRepository;
    private readonly IChargePointRepository _chargePointRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IChargingSessionRepository _chargingSessionRepository;
    private readonly IChargingSessionService _chargingSessionService;
    private readonly IConnectorStatusRepository _connectorStatusRepository;
    private readonly IConnectorUptimeRepository _connectorUptimeRepository;
    private readonly IEVDriverService _evDriverService;
    private readonly IHubContext<ChargingSessionHub> _chargingSessionHub;
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
      IEVDriverService eVDriverService,
      IHubContext<ChargingSessionHub> chargingSessionHub,
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
      _evDriverService = eVDriverService;
      _chargingSessionService = chargingSessionService;
      _chargingSessionHub = chargingSessionHub;
      _logger = logger;
    }

    public async Task StartTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        Connector connector,
        string? idTag,
        string? errorCode,
        double meterKWH
    )
    {
      try
      {
        Card? card = await _cardRepository.GetCardByNumber(idTag ?? string.Empty);
        if (card == null)
        {
          _logger.LogWarning("StartTransaction => Card not found for idTag: {IdTag}", idTag);
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
          return;
        }

        // Check if the card has sufficient balance before starting
        if (card.Balance <= 0)
        {
          _logger.LogWarning("StartTransaction => Card {CardId} has no balance ({Balance}), rejecting start", card.ID, card.Balance);
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.NoCredit;
          return;
        }

        // Check if card is blocked
        if (card.Blocked == true)
        {
          _logger.LogWarning("StartTransaction => Card {CardId} is blocked, rejecting start", card.ID);
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
          return;
        }

        // Check card expiration
        if (card.ExpirationDate < DateTime.UtcNow)
        {
          _logger.LogWarning("StartTransaction => Card {CardId} is expired ({ExpirationDate}), rejecting start", card.ID, card.ExpirationDate);
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
          return;
        }

        var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);

        var chargingSession = await _chargingSessionService.StartChargingSession(
            connector, card, DateTime.Parse(transactionEventRequest.Timestamp));

        transactionEventResponse.IdTokenInfo.Status = await _cardService.ValidateCard(idTag);
        if (transactionEventResponse.IdTokenInfo.Status != AuthorizationStatusEnumType.Accepted)
        {
          _logger.LogWarning("StartTransaction => Card validation failed with status: {Status}", transactionEventResponse.IdTokenInfo.Status);
          return;
        }

        try
        {
          var transaction = new Transaction
          {
            Uid = transactionEventRequest.TransactionInfo.TransactionId,
            ConnectorID = connector.ID,
            StartCardID = card.ID,
            ChargingSessionID = chargingSession.ID,
            StartTime = DateTime.Parse(transactionEventRequest.Timestamp),
            MeterStart = meterKWH,
            Status = TransactionStatusEnum.Current,
            StartResult = transactionEventRequest.TriggerReason.ToString()
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
              transactionEventRequest.TransactionInfo?.TransactionId);
          errorCode = ErrorCodes.InternalError;
        }
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "StartTransaction => Exception for charge point {ChargePointId}", chargePointStatus?.Id);
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }

    public async Task UpdateTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        Connector connector,
        string? idTag,
        string? errorCode,
        double meterKWH
    )
    {
      try
      {
        ChargePoint chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);

        Transaction? transaction = await FindActiveTransaction(
            transactionEventRequest.TransactionInfo.TransactionId,
            chargePoint.ChargePointId,
            chargePointStatus.Id,
            connector);

        if (transaction == null)
        {
          _logger.LogWarning("UpdateTransaction => No active transaction found for uid={TransactionUid} on connector {ConnectorId}",
              transactionEventRequest.TransactionInfo?.TransactionId, connector?.ID);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          return;
        }

        Card? card = (await _cardRepository.FindAsync(c => c.ID == transaction.StartCardID)).FirstOrDefault();
        if (card == null)
        {
          _logger.LogWarning("UpdateTransaction => Card not found for transaction {TransactionId}", transaction.ID);
          errorCode = ErrorCodes.InternalError;
          return;
        }

        if (meterKWH >= 0)
        {
          transaction.MeterStop = meterKWH;
          await _context.SaveChangesAsync();

          double chargedKwhs = (transaction.MeterStop ?? 0) - transaction.MeterStart;
          double elapsedMinutes = (DateTime.Parse(transactionEventRequest.Timestamp) - transaction.StartTime).TotalMinutes;
          double currentCostByKwh = chargedKwhs * connector.PricePerKWh;
          double currentCostByMinutes = elapsedMinutes * connector.PricePerMinute;
          double currentCost = Math.Max(currentCostByKwh, currentCostByMinutes);

          // Update the charging session with latest meter data
          await _chargingSessionService.UpdateChargingSession(
              transaction.ChargingSessionID, elapsedMinutes, chargedKwhs);

          _logger.LogInformation(
              "UpdateTransaction => Transaction {TransactionUid}: {Kwh:0.000} kWh, cost={Cost:0.00}, balance={Balance:0.00}",
              transaction.Uid, chargedKwhs, currentCost, card.Balance);

          // Check if balance is running low — stop charging if insufficient
          if (card.Balance <= currentCost + LowBalanceThresholdBuffer)
          {
            _logger.LogWarning(
                "UpdateTransaction => Low balance detected for card {CardId} (Balance={Balance:0.00}, Cost={Cost:0.00}). Stopping transaction {TransactionUid}.",
                card.ID, card.Balance, currentCost, transaction.Uid);

            var stopRequest = new RequestStopTransactionRequest
            {
              TransactionId = transaction.Uid!
            };
            await _evDriverService.RequestStopTransaction(chargePoint.ChargePointId, stopRequest);

            // Notify client about low balance
            await NotifySessionUpdate(transaction.ChargingSessionID, new ChargingSessionUpdateDto
            {
              SessionId = transaction.ChargingSessionID,
              TransactionUid = transaction.Uid,
              Status = "StoppingLowBalance",
              EnergyKWh = chargedKwhs,
              DurationMinutes = elapsedMinutes,
              CurrentCost = currentCost,
              CardBalance = card.Balance,
              Timestamp = DateTime.UtcNow
            });

            return;
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
            CardBalance = card.Balance,
            Timestamp = DateTime.UtcNow
          });
        }
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "UpdateTransaction => Exception for charge point {ChargePointId}", chargePointStatus?.Id);
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }

    public async Task EndTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        Connector connector,
        string? idTag,
        string? errorCode,
        double meterKWH
    )
    {
      try
      {
        ChargePoint chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);

        Card? card = null;
        int cardTagID = 0;

        if (!string.IsNullOrWhiteSpace(idTag))
        {
          card = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).FirstOrDefault();
          if (card != null)
          {
            cardTagID = card.ID;
            transactionEventResponse.IdTokenInfo.Status = await _cardService.ValidateCard(idTag);
          }
          else
          {
            _logger.LogWarning("EndTransaction => Card not found for idTag: {IdTag}", idTag);
            transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
          }
        }
        else
        {
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
        }

        Transaction? transaction = await FindActiveTransaction(
            transactionEventRequest.TransactionInfo.TransactionId,
            chargePoint.ChargePointId,
            chargePointStatus.Id,
            connector);

        if (transaction == null)
        {
          _logger.LogWarning(
              "EndTransaction => No active transaction found: uid='{TransactionUid}' / chargepoint='{ChargePointId}' / tag={IdTag}",
              transactionEventRequest.TransactionInfo?.TransactionId, chargePointStatus?.Id, idTag);
          errorCode = ErrorCodes.PropertyConstraintViolation;
          return;
        }

        // If we didn't find the card by idTag, fall back to the transaction's start card
        if (card == null)
        {
          card = (await _cardRepository.FindAsync(c => c.ID == transaction.StartCardID)).FirstOrDefault();
          cardTagID = card?.ID ?? 0;
        }

        _logger.LogInformation("EndTransaction => Meter='{MeterKWh:0.000}' kWh for transaction {TransactionUid}", meterKWH, transaction.Uid);

        transaction.StopTime = DateTime.Parse(transactionEventRequest.Timestamp);
        transaction.MeterStop = meterKWH;
        transaction.StopCardID = cardTagID;
        transaction.StopReason = transactionEventRequest.TriggerReason.ToString();
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
            DateTime.Parse(transactionEventRequest.Timestamp));

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "EndTransaction => Transaction {TransactionUid} ended: {Kwh:0.000} kWh, {Minutes:0.0} minutes",
            transaction.Uid, chargedKwhs, minutesCharged);

        // Reload updated card balance
        double updatedBalance = card?.Balance ?? 0;
        if (card != null)
        {
          await _context.Entry(card).ReloadAsync();
          updatedBalance = card.Balance;
        }

        // Notify connected clients about session end
        double finalCost = chargedKwhs * connector.PricePerKWh;
        await NotifySessionUpdate(transaction.ChargingSessionID, new ChargingSessionUpdateDto
        {
          SessionId = transaction.ChargingSessionID,
          TransactionUid = transaction.Uid,
          Status = "Ended",
          EnergyKWh = chargedKwhs,
          DurationMinutes = minutesCharged,
          CurrentCost = finalCost,
          CardBalance = updatedBalance,
          StopReason = transactionEventRequest.TriggerReason.ToString(),
          Timestamp = transaction.StopTime ?? DateTime.UtcNow
        });
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "EndTransaction => Exception for charge point {ChargePointId}", chargePointStatus?.Id);
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }

    #region Private Helpers

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

    #endregion
  }
}