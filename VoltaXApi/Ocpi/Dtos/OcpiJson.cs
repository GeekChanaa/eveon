using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoltaXApi.Ocpi.Dtos
{
    public static class OcpiJson
    {
        public static readonly JsonSerializerOptions Options = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
            };
            options.Converters.Add(new UtcDateTimeConverter());
            return options;
        }

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

        // The API stores UTC with an unspecified kind; local values are converted.
        public static DateTime ToUtc(DateTime value) =>
            value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        // OCPI DateTime: UTC, "Z" suffix, whole seconds.
        private sealed class UtcDateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
                writer.WriteStringValue(ToUtc(value).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        }
    }
}
