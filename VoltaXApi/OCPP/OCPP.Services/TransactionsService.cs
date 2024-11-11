
using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;


namespace VoltaXApi.OCPP.Services
{
  public class TransactionsService : ITransactionsService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public TransactionsService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

    
  }
}