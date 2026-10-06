using Newtonsoft.Json;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
bool Near(double? actual, double expected) => actual.HasValue && Math.Abs(actual.Value - expected) < 1e-9 * Math.Max(1, Math.Abs(expected));

// ---- Multipliers: value * 10^multiplier, negative ones included ----
for (int m = -3; m <= 3; m++)
{
    Check(Near(MeterValueNormalizer.ApplyMultiplier(1234, m), 1234 * Math.Pow(10, m)), $"multiplier {m} applied to 1234");
    Check(Near(MeterValueNormalizer.ToKWh(1234, "Wh", m), 1234 * Math.Pow(10, m) / 1000), $"energy Wh with multiplier {m}");
    Check(Near(MeterValueNormalizer.ToKWh(1234, "kWh", m), 1234 * Math.Pow(10, m)), $"energy kWh with multiplier {m}");
    Check(Near(MeterValueNormalizer.ToKW(1234, "W", m), 1234 * Math.Pow(10, m) / 1000), $"power W with multiplier {m}");
    Check(Near(MeterValueNormalizer.ToKW(1234, "kW", m), 1234 * Math.Pow(10, m)), $"power kW with multiplier {m}");
}
Check(MeterValueNormalizer.ToKWh(1234, "kWh", -3) == 1.234, "1234 kWh * 10^-3 is exactly 1.234");
Check(MeterValueNormalizer.ToKWh(15000, "Wh", 0) == 15, "15000 Wh = 15 kWh");

// ---- Units ----
Check(Near(MeterValueNormalizer.ToKWh(5000, null, 0), 5), "missing energy unit defaults to Wh");
Check(Near(MeterValueNormalizer.ToKWh(5000, "", 0), 5), "empty energy unit defaults to Wh");
Check(Near(MeterValueNormalizer.ToKW(7400, null, 0), 7.4), "missing power unit defaults to W");
Check(Near(MeterValueNormalizer.ToKWh(5000, "varh", 0), 5) && Near(MeterValueNormalizer.ToKWh(5, "kvarh", 0), 5), "varh / kvarh");
Check(Near(MeterValueNormalizer.ToKWh(5000, "VAh", 0), 5) && Near(MeterValueNormalizer.ToKWh(5, "kVAh", 0), 5), "VAh / kVAh");
Check(Near(MeterValueNormalizer.ToKW(7400, "VA", 0), 7.4) && Near(MeterValueNormalizer.ToKW(7.4, "kvar", 0), 7.4), "VA / kvar");
Check(Near(MeterValueNormalizer.ToKW(11, "KW", 0), 11), "unit case is ignored (KW)");
Check(!MeterValueNormalizer.TryToKWh(230, "V", 0, out var raw) && raw == 230, "non-energy unit reported, value kept unconverted");
Check(!MeterValueNormalizer.TryToKW(16, "A", 1, out var rawPower) && rawPower == 160, "non-power unit reported, multiplier still applied");

// ---- Extraction from OCPP JSON ----
MeterReading Extract(string json) => MeterValueNormalizer.Extract(JsonConvert.DeserializeObject<List<MeterValueType>>(json));

var reading = Extract("""
[{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [
    { "value": 12345, "measurand": "Energy.Active.Import.Register", "unitOfMeasure": { "unit": "Wh", "multiplier": 0 } },
    { "value": 7400, "measurand": "Power.Active.Import", "unitOfMeasure": { "unit": "W" } },
    { "value": 55, "measurand": "SoC", "unitOfMeasure": { "unit": "Percent" } } ] }]
""");
Check(Near(reading.EnergyKWh, 12.345) && Near(reading.PowerKW, 7.4) && Near(reading.StateOfCharge, 55), "energy, power and SoC extracted");
Check(reading.EnergyTimestamp == new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc), "energy timestamp kept");
Check(reading.UnexpectedUnits.Count == 0, "no unexpected unit");

Check(Near(Extract("""[{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [ { "value": 42 } ] }]""").EnergyKWh, 0.042),
    "sample without measurand and unit is Energy.Active.Import.Register in Wh");
Check(Near(Extract("""[{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [ { "value": 12345678, "unitOfMeasure": { "unit": "kWh", "multiplier": -3 } } ] }]""").EnergyKWh, 12345.678),
    "negative multiplier applied in extraction (was ignored before)");

var final = Extract("""
[{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [
    { "value": 100, "context": "Transaction.End", "measurand": "Energy.Active.Import.Register", "unitOfMeasure": { "unit": "kWh" } },
    { "value": 50, "context": "Transaction.End", "measurand": "SoC" },
    { "value": 99000, "context": "Sample.Periodic", "measurand": "Energy.Active.Import.Register", "unitOfMeasure": { "unit": "Wh" } } ] }]
""");
Check(Near(final.EnergyKWh, 100) && Near(final.StateOfCharge, 50), "Transaction.End energy wins; a Transaction.End SoC sample is not taken as energy");

var phases = Extract("""
[{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [
    { "value": 30000, "measurand": "Energy.Active.Import.Register" },
    { "value": 10000, "measurand": "Energy.Active.Import.Register", "phase": "L1" } ] }]
""");
Check(Near(phases.EnergyKWh, 30), "whole-meter energy wins over a per-phase sample");
Check(MeterValueNormalizer.Extract(null) == MeterReading.Empty && Extract("[]").EnergyKWh == null, "no meter values => no reading");

// ---- Business clock: UTC boundaries of business days ----
var casablanca = BusinessClock.ResolveTimeZone("Africa/Casablanca");
var time = new FixedTime(new DateTimeOffset(2026, 10, 2, 23, 30, 0, TimeSpan.Zero));
var clock = new BusinessClock(time, casablanca);
var offset = casablanca.GetUtcOffset(time.GetUtcNow());
Check(clock.Today == time.GetUtcNow().ToOffset(offset).Date, "business today follows the business time zone");
Check(clock.StartOfDayUtc(clock.Today) == DateTime.SpecifyKind(clock.Today - offset, DateTimeKind.Utc), "start of business day in UTC");
Check(clock.StartOfDayUtc(clock.Today).Kind == DateTimeKind.Utc, "boundaries are UTC");
Check(clock.ToLocal(clock.StartOfDayUtc(clock.Today)) == clock.Today, "round trip of a day start");
var utcClock = new BusinessClock(time, TimeZoneInfo.Utc);
Check(utcClock.Today == new DateTime(2026, 10, 2) && utcClock.StartOfMonthUtc(2026, 10) == new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), "UTC zone: plain calendar");
Check(BusinessClock.ResolveTimeZone("Not/AZone") == TimeZoneInfo.Utc, "unknown zone falls back to UTC");

Console.WriteLine($"{checks} checks passed");

class FixedTime(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
