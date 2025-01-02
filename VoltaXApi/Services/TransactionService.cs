using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.Services
{
  public class TransactionService : ITransactionService
  {

    private readonly ICardService _cardService;
    private readonly ICardRepository _cardRepository;
    private readonly IChargePointRepository _chargePointRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IChargingSessionRepository _chargingSessionRepository;
    private readonly VoltaXApiDbContext _context;

    public TransactionService(
      ICardService cardService,
      ICardRepository cardRepository,
      IChargePointRepository chargePointRepository,
      ITransactionRepository transactionRepository,
      IChargingSessionRepository chargingSessionRepository,
      VoltaXApiDbContext context
    )
    {
      _cardService = cardService;
      _cardRepository = cardRepository;
      _chargePointRepository = chargePointRepository;
      _transactionRepository = transactionRepository;
      _context = context;
      _chargingSessionRepository = chargingSessionRepository;
    }

    public async Task StartTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        int connectorID,
        string? idTag,
        string errorCode,
        double meterKWH
    )
    {
      try
      {
        int cardTagID = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).First().ID;
        int chargePointID = (await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id)).ID;
        int chargingSessionID = (await _chargingSessionRepository.GetLastChargingSession(connectorID)).ID;

        transactionEventResponse.IdTokenInfo.Status = await _cardService.ValidateCard(idTag);
        if (transactionEventResponse.IdTokenInfo.Status == AuthorizationStatusEnumType.Accepted)
        {
          try
          {
            Transaction transaction = new Transaction();
            transaction.Uid = transactionEventRequest.TransactionInfo.TransactionId;
            transaction.ConnectorID = connectorID;
            transaction.StartCardID = cardTagID;
            transaction.ChargingSessionID = chargingSessionID;
            transaction.StartTime = DateTime.Parse(transactionEventRequest.Timestamp);
            transaction.MeterStart = meterKWH;
            transaction.StartResult = transactionEventRequest.TriggerReason.ToString();
            await _transactionRepository.AddAsync(transaction);
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
        int connectorID,
        string? idTag,
        string errorCode,
        double meterKWH
    )
    {
      try
      {
        int cardTagID = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).First().ID;
        ChargePoint chargePoint = (await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id));

        Transaction? transaction = 
            (await _transactionRepository
              .FindAsync(t => t.Uid == transactionEventRequest.TransactionInfo.TransactionId))
              .OrderByDescending(t => t.ID)
              .FirstOrDefault();
        if (
            transaction == null
            || chargePoint.ChargePointId != chargePointStatus.Id
            || transaction.StopTime.HasValue
        )
        {
          // unknown transaction id or already stopped transaction
          // => find latest transaction for the charge point and check if its open
          Console.WriteLine(
              "UpdateTransaction => Unknown or closed transaction uid={0}",
              transactionEventRequest.TransactionInfo?.TransactionId
          );
          // find latest transaction for this charge point
          transaction = (await _transactionRepository
              .FindAsync(t => t.ConnectorID == connectorID))
              .OrderByDescending(t => t.ID)
              .FirstOrDefault();

          if (transaction != null)
          {
            Console.WriteLine(
                "UpdateTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'",
                transaction.ID,
                transaction.StartTime.ToString("O"),
                transaction?.StopTime?.ToString("O")
            );
            if (transaction.StopTime.HasValue)
            {
              Console.WriteLine(
                  "UpdateTransaction => Last transaction (id={0}) is already closed ",
                  transaction.ID
              );
              transaction = null;
            }
          }
          else
          {
            Console.WriteLine(
                "UpdateTransaction => Found no transaction for charge point '{0}' and connectorID '{1}'",
                chargePointStatus.Id,
                connectorID
            );
          }
        }

        if (transaction != null)
        {
          if (meterKWH >= 0)
          {
            transaction.MeterStop = meterKWH;
            _context.SaveChanges();
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
        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
      }
    }

    public async Task EndTransaction(
        TransactionEventRequest transactionEventRequest,
        TransactionEventResponse transactionEventResponse,
        ChargePointStatus chargePointStatus,
        int connectorID,
        string? idTag,
        string errorCode,
        double meterKWH
    )
    {
      try
      {
        int cardTagID = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).First().ID;
        ChargePoint chargePoint = (await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id));

        if (string.IsNullOrWhiteSpace(idTag))
          transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
        else
          transactionEventResponse.IdTokenInfo.Status = await _cardService.ValidateCard(idTag);

        Transaction? transaction = _context
            .Transactions.Where(t =>
                t.Uid == transactionEventRequest.TransactionInfo.TransactionId
            )
            .OrderByDescending(t => t.ID)
            .FirstOrDefault();
        if (
            transaction == null
            || transaction.ConnectorID != connectorID
            || transaction.StopTime.HasValue
        )
        {
          // unknown transaction id or already stopped transaction
          // => find latest transaction for the charge point and check if its open
          Console.WriteLine(
              "EndTransaction => Unknown or closed transaction uid={0}",
              transactionEventRequest.TransactionInfo?.TransactionId
          );
          // find latest transaction for this charge point
          transaction = _context
              .Transactions.Where(t => t.ConnectorID == connectorID)
              .OrderByDescending(t => t.ID)
              .FirstOrDefault();

          if (transaction != null)
          {
            Console.WriteLine(
                "EndTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'",
                transaction.ID,
                transaction.StartTime.ToString("O"),
                transaction?.StopTime?.ToString("O")
            );
            if (transaction.StopTime.HasValue)
            {
              Console.WriteLine(
                  "EndTransaction => Last transaction (id={0}) is already closed ",
                  transaction.ID
              );
              transaction = null;
            }
          }
          else
          {
            Console.WriteLine(
                "EndTransaction => Found no transaction for charge point '{0}' and connectorID '{1}'",
                chargePointStatus.Id,
                connectorID
            );
          }
        }

        if (transaction != null)
        {
          // check current tag against start tag
          // bool valid = true;
          // if (!string.Equals(transaction.StartTagId, idTag, StringComparison.InvariantCultureIgnoreCase))
          // {
          //     // tags are different => same group?
          //     ChargeTag? startTag = _context.ChargeTags.Where(c => c.TagID == transaction.StartTagId).FirstOrDefault();
          //     if (startTag != null)
          //     {
          //         if (!string.Equals(startTag.ParentTagId, ct?.ParentTagId, StringComparison.InvariantCultureIgnoreCase))
          //         {
          //             Console.WriteLine("EndTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, ct?.ID);
          //             transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
          //             valid = false;
          //         }
          //         else
          //         {
          //             Console.WriteLine("EndTransaction => Different charge tags but matching group ('{0}')", ct?.ParentTagId);
          //         }
          //     }
          //     else
          //     {
          //         Console.WriteLine("EndTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
          //         // assume "valid" and allow to end the transaction
          //     }
          // }

          // if (valid)
          // {
          // write current meter value in "stop" value
          Console.WriteLine("EndTransaction => Meter='{0}' (kWh)", meterKWH);

          transaction.StopTime = DateTime.Parse(transactionEventRequest.Timestamp);
          transaction.MeterStop = meterKWH;
          transaction.StopCardID = cardTagID;
          transaction.StopReason = transactionEventRequest.TriggerReason.ToString();
          _context.SaveChanges();

          // Update connecter status to available

          // }
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