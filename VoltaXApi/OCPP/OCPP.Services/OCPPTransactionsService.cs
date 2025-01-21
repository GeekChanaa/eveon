
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class OCPPTransactionsService : IOCPPTransactionsService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public OCPPTransactionsService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

    public async Task GetTransactionStatusRequest(string chargePointID, GetTransactionStatusRequest request)
    {
        var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() }
                
            };
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetTransactionStatusRequest",
            JsonPayload = JsonConvert.SerializeObject(request,settings)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }
  }
}