using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoltaXApi.OCPP.Messages
{
    public class ReadingContextEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ReadingContextEnumType?)
                || objectType == typeof(ReadingContextEnumType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    // Replace dots with underscores to match enum names
                    var enumName = value.Replace('.', '_');

                    if (Enum.TryParse(typeof(ReadingContextEnumType), enumName, out var result))
                    {
                        return result;
                    }
                }
            }

            throw new JsonSerializationException(
                $"Unable to convert \"{reader.Value}\" to {nameof(ReadingContextEnumType)}."
            );
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ReadingContextEnumType enumValue)
            {
                // Replace underscores with dots for serialization
                var stringValue = enumValue.ToString().Replace('_', '.');
                writer.WriteValue(stringValue);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
