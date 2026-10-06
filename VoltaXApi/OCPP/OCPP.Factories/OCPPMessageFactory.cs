using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
    public class OCPPMessageFactory
    {
        /// <summary>OCPP JSON schemas use camelCase property names and enum names as strings.</summary>
        public static readonly JsonSerializerSettings DefaultSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        public OCPPMessage CreateMessage<T>(string action, T request, JsonSerializerSettings? settings = null)
        {
            return new OCPPMessage
            {
                MessageType = "2",
                UniqueId = Guid.NewGuid().ToString("N"),
                Action = action,
                JsonPayload = JsonConvert.SerializeObject(request, settings ?? DefaultSettings)
            };
        }
    }
}
