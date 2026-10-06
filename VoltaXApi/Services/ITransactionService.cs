using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using OCPP.Core.Server;

namespace VoltaXApi.Services
{
    public interface ITransactionService
    {
      Task StartTransaction( TransactionEventRequest transactionEventRequest, TransactionEventResponse transactionEventResponse, ChargePointStatus chargePointStatus, Connector connector, string? idTag, string? errorCode, double? meterKWH);
      Task UpdateTransaction( TransactionEventRequest transactionEventRequest, TransactionEventResponse transactionEventResponse, ChargePointStatus chargePointStatus, Connector connector, string? idTag, string? errorCode, double? meterKWH);
      Task EndTransaction( TransactionEventRequest transactionEventRequest, TransactionEventResponse transactionEventResponse, ChargePointStatus chargePointStatus, Connector connector, string? idTag, string? errorCode, double? meterKWH);

      // Version-neutral core (OCPP 1.6 and 2.0.1). Each returns the authorization status to answer, or null when
      // the event does not change it.
      Task<AuthorizationStatusEnumType?> StartTransaction(TransactionEventData data, Connector? connector, bool validateOnly = false);
      Task<AuthorizationStatusEnumType?> UpdateTransaction(TransactionEventData data, Connector connector);
      Task<AuthorizationStatusEnumType?> EndTransaction(TransactionEventData data, Connector connector);
    }
}