using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
    public class OCPPMessageFactory
    {
        private readonly JsonSerializerSettings _defaultSettings;
        
        public OCPPMessageFactory()
        {
            _defaultSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                NullValueHandling = NullValueHandling.Ignore
            };
        }
        
        public OCPPMessage CreateMessage<T>(string action, T request, JsonSerializerSettings? settings = null)
        {
            return new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = action,
                JsonPayload = JsonConvert.SerializeObject(request, settings ?? _defaultSettings)
            };
        }
    }
}