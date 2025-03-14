
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class OCPPTransactionsService : IOCPPTransactionsService
  {
    private readonly OCPPMessageFactory _messageFactory;
    private readonly OCPPMessageProcessor _messageProcessor;
    private readonly ILogger<OCPPTransactionsService> _logger;
    public OCPPTransactionsService(
      OCPPMessageProcessor messageProcessor,
      ILogger<OCPPTransactionsService> logger
    )
    {
      _messageProcessor = messageProcessor;
      _messageFactory = new OCPPMessageFactory();
      _logger = logger;
    }

    public async Task GetTransactionStatusRequest(string chargePointID, GetTransactionStatusRequest request)
    {
      _logger.LogInformation("Starting GetTransactionStatusRequest for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("GetTransactionStatusRequest", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed GetTransactionStatusRequest for ChargePoint: {ChargePointID}", chargePointID);
    }
  }
}