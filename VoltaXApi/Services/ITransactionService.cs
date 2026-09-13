using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using OCPP.Core.Server;

namespace VoltaXApi.Services
{
    public interface ITransactionService
    {
      Task StartTransaction( TransactionEventRequest transactionEventRequest, TransactionEventResponse transactionEventResponse, ChargePointStatus chargePointStatus, Connector connector, string? idTag, string? errorCode, double meterKWH);
      Task UpdateTransaction( TransactionEventRequest transactionEventRequest, TransactionEventResponse transactionEventResponse, ChargePointStatus chargePointStatus, Connector connector, string? idTag, string? errorCode, double meterKWH);
      Task EndTransaction( TransactionEventRequest transactionEventRequest, TransactionEventResponse transactionEventResponse, ChargePointStatus chargePointStatus, Connector connector, string? idTag, string? errorCode, double meterKWH);
    }
}