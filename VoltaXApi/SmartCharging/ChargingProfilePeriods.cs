using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoltaXApi.SmartCharging
{
    /// <summary>A period of a charging schedule: <see cref="StartPeriod"/> seconds after the schedule start.</summary>
    public sealed record ChargingProfilePeriod(int StartPeriod, double Limit, int? NumberPhases = null, int? PhaseToUse = null);

    /// <summary>A period of a <see cref="Models.ChargingStrategy"/> (see its PeriodsJson for the meaning of StartSeconds).</summary>
    public sealed record ChargingStrategyPeriod(int StartSeconds, double Limit, int? NumberPhases = null);

    /// <summary>JSON stored in ChargingProfiles.PeriodsJson and ChargingStrategies.PeriodsJson.</summary>
    public static class SmartChargingJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string Serialize<T>(IEnumerable<T> periods) => JsonSerializer.Serialize(periods.ToList(), Options);

        public static List<T> Deserialize<T>(string? json) =>
            string.IsNullOrWhiteSpace(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
    }
}
