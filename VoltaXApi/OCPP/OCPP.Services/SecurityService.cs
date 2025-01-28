
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class SecurityService : ISecurityService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public SecurityService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

    public async Task InstallCertificate(string chargePointID, InstallCertificateRequest request)
    {
        var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() }
                
            };
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "InstallCertificate",
            JsonPayload = JsonConvert.SerializeObject(request,settings)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    
  }
}