using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Data.Seeders;
using VoltaXApi.OCPP.Services;
using Microsoft.IdentityModel.Tokens;
using VoltaXApi.Exceptions;

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
    private readonly IEVDriverService _evDriverService;
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
    }

    public async Task StartTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        Connector connector,
        string? idTag,
        string errorCode,
        double meterKWH
    )
    {
      try
      {
        Card? card = await _cardRepository.GetCardByNumber(idTag);
        if(card == null) throw new CardNotFoundException("Invalid Card");

        int chargePointID = (await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id)).ID;

        
        var chargingSession = await _chargingSessionService.StartChargingSession(connector,card,DateTime.Parse(transactionEventRequest.Timestamp));


        transactionEventResponse.IdTokenInfo.Status = await _cardService.ValidateCard(idTag);
        if (transactionEventResponse.IdTokenInfo.Status == AuthorizationStatusEnumType.Accepted)
        {
          try
          {
            Transaction transaction = new Transaction();
            transaction.Uid = transactionEventRequest.TransactionInfo.TransactionId;
            transaction.ConnectorID = connector.ID;
            transaction.StartCardID = card.ID;
            transaction.ChargingSessionID = chargingSession.ID;
            transaction.StartTime = DateTime.Parse(transactionEventRequest.Timestamp);
            transaction.MeterStart = meterKWH;
            transaction.Status = TransactionStatusEnum.Current;
            transaction.StartResult = transactionEventRequest.TriggerReason.ToString();
            await _transactionRepository.AddAsync(transaction);
            await _connectorUptimeRepository.TransactionStartUptimeHandle(transaction.ID, connector.ID);
          } 
          catch (Exception exp)
          {
            errorCode = ErrorCodes.InternalError;
          }
        }
      }
      catch (Exception exp)
      {
        Console.WriteLine("StartTransaction => Exception: {0}", exp.Message);
        Console.WriteLine(exp.StackTrace);
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }

    public async Task UpdateTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        Connector connector,
        string? idTag,
        string errorCode,
        double meterKWH
    )
    {
      try
      {
        Card? card = null;
        
        ChargePoint chargePoint = (await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id));
        
        Transaction? transaction = (await _transactionRepository
              .FindAsync(t => t.Uid == transactionEventRequest.TransactionInfo.TransactionId))
              .OrderByDescending(t => t.ID)
              .FirstOrDefault();

        if (transaction == null && connector == null)
        {
          return;
        }
          
        if (transaction == null || chargePoint.ChargePointId != chargePointStatus.Id || transaction.StopTime.HasValue)
        {
          Console.WriteLine("UpdateTransaction => Unknown or closed transaction uid={0}", transactionEventRequest.TransactionInfo?.TransactionId);

          transaction = (await _transactionRepository
              .FindAsync(t => t.ConnectorID == connector.ID))
              .OrderByDescending(t => t.ID)
              .FirstOrDefault();

          if (transaction != null)
          {
            card = (await _cardRepository.FindAsync(c => c.ID == transaction.StartCardID)).First();
            Console.WriteLine("UpdateTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'", transaction.ID, transaction.StartTime.ToString("O"), transaction?.StopTime?.ToString("O"));

            if (transaction.StopTime.HasValue)
            {
              Console.WriteLine("UpdateTransaction => Last transaction (id={0}) is already closed ", transaction.ID);
              transaction = null;
            }
          }
          else
          {
            Console.WriteLine(
                "UpdateTransaction => Found no transaction for charge point '{0}' and connectorID '{1}'",
                chargePointStatus.Id,
                connector.ID
            );
          }
        }

        if (transaction != null)
        {
          card = (await _cardRepository.FindAsync(c => c.ID == transaction.StartCardID)).First();
          if (meterKWH >= 0)
          {
            transaction.MeterStop = meterKWH;
            _context.SaveChanges();
            var kwhs = transaction.MeterStop - transaction.MeterStart;
            var amount = (double) kwhs * connector.PricePerKWh;
            if((double) card.Balance <= amount+5)
            { 
              RequestStopTransactionRequest request = new(){
                TransactionId = transaction.Uid
              };
              await _evDriverService.RequestStopTransaction(chargePoint.ChargePointId, request);
            }
          }
        }
        else
        {
          errorCode = ErrorCodes.PropertyConstraintViolation;
        }
      }
      catch (Exception exp)
      {
        Console.WriteLine("UpdateTransaction => Exception: {0}", exp.Message);
        Console.WriteLine("UpdateTransaction => StackTrace: {0}", exp.StackTrace);
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }

    public async Task EndTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        Connector connector,
        string? idTag,
        string errorCode,
        double meterKWH
    )
    {
      try
      {
        int cardTagID = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).First().ID;
        ChargePoint chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);

        if (string.IsNullOrWhiteSpace(idTag))
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
        else
          transactionEventResponse.IdTokenInfo.Status = await _cardService.ValidateCard(idTag);

        Transaction? transaction = _context
                    .Transactions.Where(t =>t.Uid == transactionEventRequest.TransactionInfo.TransactionId)
                                .OrderByDescending(t => t.ID)
                                .FirstOrDefault();

        if (
            transaction == null
            || transaction.ConnectorID != connector.ID
            || transaction.StopTime != null
        )
        {
          Console.WriteLine(
              "EndTransaction => Unknown or closed transaction uid={0}",
              transactionEventRequest.TransactionInfo?.TransactionId
          );
          transaction = _context
              .Transactions.Where(t => t.ConnectorID == connector.ID)
              .OrderByDescending(t => t.ID)
              .FirstOrDefault();

          if (transaction != null)
          {
            if (transaction.StopTime.HasValue)
            {
              Console.WriteLine("EndTransaction => Last transaction (id={0}) is already closed ", transaction.ID);
              transaction = null;
            }
          }
          else
          {
            Console.WriteLine("EndTransaction => Found no transaction for charge point '{0}' and connectorID '{1}'", chargePointStatus.Id, connector.ID);
          }
        }

        if (transaction != null)
        {
          Console.WriteLine("EndTransaction => Meter='{0}' (kWh)", meterKWH);
          transaction.StopTime = DateTime.Parse(transactionEventRequest.Timestamp);
          transaction.MeterStop = meterKWH;
          transaction.StopCardID = cardTagID;
          transaction.StopReason = transactionEventRequest.TriggerReason.ToString();
          transaction.Status = TransactionStatusEnum.Ended;
          double minutesCharged = 0;
          if (transaction.StopTime.HasValue)
          {
              minutesCharged = (transaction.StopTime.Value - transaction.StartTime).TotalMinutes;
          }

          // Updating the Amount of the card related to the tag id.
          await _cardService.SubstractAmountFromCardByMinutes(cardTagID, minutesCharged, connector.ID);
          await _chargingSessionService.EndChargingSession(transaction.ChargingSessionID, minutesCharged, DateTime.Parse(transactionEventRequest.Timestamp));
          _context.SaveChanges();
        }
        else
        {
          Console.WriteLine(
              "EndTransaction => Unknown transaction: uid='{0}' / chargepoint='{1}' / tag={2}",
              transactionEventRequest.TransactionInfo?.TransactionId,
              chargePointStatus?.Id,
              idTag
          );
          // await _msgLogRepo.SaveLogMessage(ChargePointStatus?.Id, connectorID, msgIn.Action, string.Format("UnknownTransaction:UID={0}/Meter={1}", transactionEventRequest.TransactionInfo?.TransactionId, GetMeterValue(transactionEventRequest.MeterValues)), errorCode);
          errorCode = ErrorCodes.PropertyConstraintViolation;
        }
      }
      catch (Exception exp)
      {
        Console.WriteLine("EndTransaction => Exception: {0}", exp.Message);
        Console.WriteLine(exp.StackTrace);
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }
  }

}