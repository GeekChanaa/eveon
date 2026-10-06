using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;
using VoltaXApi.SmartCharging;
using ValidationException = VoltaXApi.Exceptions.ValidationException;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
void Throws<TException>(Action action, string name) where TException : Exception
{
    try { action(); }
    catch (TException) { Check(true, name); return; }
    catch (Exception ex) { throw new Exception($"FAILED: {name} (threw {ex.GetType().Name}: {ex.Message})"); }
    throw new Exception("FAILED (no exception): " + name);
}
bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;
JObject Wire(object message) => JObject.Parse(JsonConvert.SerializeObject(message, OCPPMessageFactory.DefaultSettings));

var t0 = new DateTime(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);
LoadLimitSettings Settings(double? maxA, LoadBalancingStrategyEnum strategy = LoadBalancingStrategyEnum.EqualShare, double min = 6,
    double? maxKW = null, int phases = 3, double margin = 0) => new(maxA, maxKW, phases, 230, min, strategy, margin);
List<SessionDemand> Sessions(params double?[] caps) => caps.Select((c, i) => new SessionDemand(i + 1, t0.AddMinutes(i), c)).ToList();
double A(AllocationResult r, int id) => r.Sessions.Single(s => s.TransactionId == id).AllocatedA;
bool Q(AllocationResult r, int id) => r.Sessions.Single(s => s.TransactionId == id).Queued;

// ---------------------------------------------------------------- allocator: station limit
Check(Near(StationLoadAllocator.EffectiveLimitA(Settings(32)), 32), "limit from MaxCurrentA");
Check(Near(StationLoadAllocator.EffectiveLimitA(Settings(null, maxKW: 22)), 31.8), "22 kW / (230 V x 3) = 31.8 A (rounded down)");
Check(Near(StationLoadAllocator.EffectiveLimitA(Settings(20, maxKW: 22)), 20), "lower of current and power limit");
Check(Near(StationLoadAllocator.EffectiveLimitA(Settings(32, margin: 10)), 28.8), "safety margin 10 % of 32 A = 28.8 A");
Check(Near(StationLoadAllocator.EffectiveLimitA(Settings(null)), 0), "no limit configured = 0 A");
Check(Near(StationLoadAllocator.ToKW(16, Settings(32)), 11.04), "16 A x 230 V x 3 = 11.04 kW");

// ---------------------------------------------------------------- allocator: EqualShare
var r = StationLoadAllocator.Allocate(Settings(32), Sessions(null, null, null));
Check(r.Sessions.All(s => Near(s.AllocatedA, 10.6) && !s.Queued), "EqualShare 32 A / 3 = 10.6 A each (rounded down)");
r = StationLoadAllocator.Allocate(Settings(32), Sessions(null, null, null, null, null, null));
Check(Enumerable.Range(1, 5).All(i => Near(A(r, i), 6.4)) && Q(r, 6) && A(r, 6) == 0, "EqualShare: 5 sessions fit 6 A min in 32 A (6.4 A each), the latest is queued at 0 A");
r = StationLoadAllocator.Allocate(Settings(10), Sessions(null, null));
Check(Near(A(r, 1), 10) && Q(r, 2), "EqualShare: 10 A cannot give 6 A to two sessions; the first started keeps charging");
r = StationLoadAllocator.Allocate(Settings(32), Sessions(10, null, null));
Check(Near(A(r, 1), 10) && Near(A(r, 2), 11) && Near(A(r, 3), 11), "EqualShare: EV max 10 A frees current for the others (10/11/11)");
r = StationLoadAllocator.Allocate(Settings(13), Sessions(2, null, null));
Check(Near(A(r, 1), 2) && Near(A(r, 2), 11) && Q(r, 3), "EqualShare: an EV below the minimum needs only its maximum to be admitted");
r = StationLoadAllocator.Allocate(Settings(32), Sessions());
Check(r.Sessions.Count == 0 && r.TotalA == 0, "no session, no allocation");
r = StationLoadAllocator.Allocate(Settings(0), Sessions(null, null));
Check(r.Sessions.All(s => s.Queued && s.AllocatedA == 0), "station limit 0: every session queued");
var reversed = new List<SessionDemand> { new(7, t0.AddMinutes(5)), new(3, t0), new(9, t0.AddMinutes(1)) };
r = StationLoadAllocator.Allocate(Settings(12), reversed);
Check(!Q(r, 3) && !Q(r, 9) && Q(r, 7), "EqualShare admits by start time, not by list order");

// ---------------------------------------------------------------- allocator: FirstComeFirstServed
r = StationLoadAllocator.Allocate(Settings(32, LoadBalancingStrategyEnum.FirstComeFirstServed), Sessions(20, null, null));
Check(Near(A(r, 1), 20) && Near(A(r, 2), 12) && Q(r, 3), "FCFS: 20 A (EV max), then the 12 A left, then queued");
r = StationLoadAllocator.Allocate(Settings(32, LoadBalancingStrategyEnum.FirstComeFirstServed), Sessions(28, null, 4));
Check(Near(A(r, 1), 28) && Q(r, 2) && Near(A(r, 3), 4), "FCFS: 4 A left are too few for an uncapped session (6 A min) but enough for a 4 A EV");

// ---------------------------------------------------------------- allocator: random sets never exceed the limit
var random = new Random(20261004);
var cases = 0;
for (var i = 0; i < 20_000; i++)
{
    var strategy = random.Next(2) == 0 ? LoadBalancingStrategyEnum.EqualShare : LoadBalancingStrategyEnum.FirstComeFirstServed;
    var settings = new LoadLimitSettings(
        random.Next(4) == 0 ? null : Math.Round(random.NextDouble() * 250, 2),
        random.Next(3) == 0 ? Math.Round(random.NextDouble() * 150, 2) : null,
        random.Next(2) == 0 ? 1 : 3,
        random.Next(5) == 0 ? 120 + random.Next(300) : 230,
        Math.Round(random.NextDouble() * 12, 1),
        strategy,
        random.Next(3) == 0 ? random.NextDouble() * 40 : 0);
    var sessions = Enumerable.Range(1, random.Next(0, 40))
        .Select(id => new SessionDemand(id, t0.AddSeconds(random.Next(0, 3600)), random.Next(3) == 0 ? Math.Round(random.NextDouble() * 40, 1) : null))
        .ToList();
    var result = StationLoadAllocator.Allocate(settings, sessions); // also runs CheckInvariants
    var limit = StationLoadAllocator.EffectiveLimitA(settings);
    if (result.TotalA > limit + 1e-3) throw new Exception($"FAILED: random case {i} exceeds the limit ({result.TotalA} > {limit})");
    if (result.Sessions.Any(s => s.AllocatedA < 0 || s.Queued && s.AllocatedA != 0)) throw new Exception($"FAILED: random case {i} has an invalid allocation");
    if (strategy == LoadBalancingStrategyEnum.EqualShare && sessions.All(s => s.MaxCurrentA == null))
    {
        var active = result.Sessions.Where(s => !s.Queued).Select(s => s.AllocatedA).ToList();
        if (active.Count > 0 && active.Max() - active.Min() > 1e-6) throw new Exception($"FAILED: random case {i} EqualShare shares differ");
        if (active.Any(a => a + 1e-6 < settings.MinPerSessionA)) throw new Exception($"FAILED: random case {i} below the minimum");
        var admitted = active.Count;
        var expected = settings.MinPerSessionA <= 0 ? sessions.Count : Math.Min(sessions.Count, (int)Math.Floor((limit + 1e-3) / settings.MinPerSessionA));
        if (admitted != expected) throw new Exception($"FAILED: random case {i} admitted {admitted}, expected {expected}");
        var lastAdmitted = result.Sessions.Where(s => !s.Queued).Select(s => sessions.Single(x => x.TransactionId == s.TransactionId).StartedAt).DefaultIfEmpty(DateTime.MinValue).Max();
        var firstQueued = result.Sessions.Where(s => s.Queued).Select(s => sessions.Single(x => x.TransactionId == s.TransactionId).StartedAt).DefaultIfEmpty(DateTime.MaxValue).Min();
        if (firstQueued < lastAdmitted) throw new Exception($"FAILED: random case {i} queued an earlier session");
    }
    cases++;
}
Check(cases == 20_000, "20 000 random session sets: never above the station limit, EqualShare equal shares >= minimum, queue in start order");
Throws<InvalidOperationException>(() => StationLoadAllocator.CheckInvariants(
    new AllocationResult(10, new[] { new SessionAllocation(1, 6, false), new SessionAllocation(2, 6, false) }), Sessions(null, null), 6),
    "invariant check rejects an allocation above the station limit");

// ---------------------------------------------------------------- profile DTO JSON shapes: 1.6 vs 2.0.1
var periods = new List<ChargingProfilePeriod> { new(0, 16, 3), new(3600, 10.67, 3) };
var txDefault = new ChargingProfile
{
    OcppProfileId = 42, EvseId = 0, StackLevel = 1, Purpose = ChargingProfilePurposeEnum.TxDefaultProfile, Kind = ChargingProfileKindEnum.Recurring,
    RecurrencyKind = ChargingProfileRecurrencyEnum.Daily, ChargingRateUnit = ChargingRateUnitEnum.A,
    StartSchedule = new DateTime(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc), Duration = 86_400
};
ChargingProfileMessages.Validate(txDefault, periods, OcppProtocols.Ocpp16);
var json16 = Wire(ChargingProfileMessages.ToOcpp16(txDefault, periods));
Check(json16["connectorId"]!.Value<int>() == 0 && json16["evseId"] == null, "1.6: connectorId, no evseId");
var cs = (JObject)json16["csChargingProfiles"]!;
Check(cs["chargingProfileId"]!.Value<int>() == 42 && cs["stackLevel"]!.Value<int>() == 1, "1.6: csChargingProfiles.chargingProfileId / stackLevel");
Check(cs["chargingProfilePurpose"]!.Value<string>() == "TxDefaultProfile" && cs["chargingProfileKind"]!.Value<string>() == "Recurring" && cs["recurrencyKind"]!.Value<string>() == "Daily",
    "1.6: purpose / kind / recurrency as enum names");
Check(cs["transactionId"] == null && cs["validFrom"] == null, "1.6: absent optional fields are omitted");
Check(cs["chargingSchedule"] is JObject schedule16 && schedule16["chargingRateUnit"]!.Value<string>() == "A" && schedule16["duration"]!.Value<int>() == 86_400,
    "1.6: chargingSchedule is a single object");
Check(cs["chargingSchedule"]!["startSchedule"]!.Value<DateTime>().ToUniversalTime() == new DateTime(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc)
      && JsonConvert.SerializeObject(ChargingProfileMessages.ToOcpp16(txDefault, periods), OCPPMessageFactory.DefaultSettings).Contains("2026-10-13T23:00:00Z"),
    "1.6: startSchedule serialized as UTC (Z)");
var periods16 = (JArray)cs["chargingSchedule"]!["chargingSchedulePeriod"]!;
Check(periods16.Count == 2 && periods16[1]["startPeriod"]!.Value<int>() == 3600 && periods16[1]["limit"]!.Value<decimal>() == 10.6m && periods16[1]["phaseToUse"] == null,
    "1.6: periods with limit rounded down to 0.1 and no phaseToUse");

var json201 = Wire(ChargingProfileMessages.ToOcpp201(txDefault, periods));
Check(json201["evseId"]!.Value<int>() == 0 && json201["connectorId"] == null && json201["csChargingProfiles"] == null, "2.0.1: evseId + chargingProfile");
var cp201 = (JObject)json201["chargingProfile"]!;
Check(cp201["id"]!.Value<int>() == 42 && cp201["chargingProfilePurpose"]!.Value<string>() == "TxDefaultProfile", "2.0.1: chargingProfile.id / purpose");
Check(cp201["chargingSchedule"] is JArray { Count: 1 } schedules && schedules[0]["id"]!.Value<int>() == 42 && schedules[0]["chargingRateUnit"]!.Value<string>() == "A",
    "2.0.1: chargingSchedule is an array of schedules with an id");
Check(((JArray)cp201["chargingSchedule"]![0]!["chargingSchedulePeriod"]!)[1]["limit"]!.Value<double>() == 10.6, "2.0.1: limit rounded down to 0.1");

var stationMax = new ChargingProfile
{
    OcppProfileId = 7, EvseId = 0, Purpose = ChargingProfilePurposeEnum.ChargingStationMaxProfile, Kind = ChargingProfileKindEnum.Absolute,
    StartSchedule = t0, ChargingRateUnit = ChargingRateUnitEnum.W
};
var maxPeriods = new List<ChargingProfilePeriod> { new(0, 22000) };
Check(Wire(ChargingProfileMessages.ToOcpp16(stationMax, maxPeriods))["csChargingProfiles"]!["chargingProfilePurpose"]!.Value<string>() == "ChargePointMaxProfile",
    "1.6: ChargingStationMaxProfile is sent as ChargePointMaxProfile");
Check(Wire(ChargingProfileMessages.ToOcpp201(stationMax, maxPeriods))["chargingProfile"]!["chargingProfilePurpose"]!.Value<string>() == "ChargingStationMaxProfile",
    "2.0.1: ChargingStationMaxProfile");

var txProfile = new ChargingProfile
{
    OcppProfileId = 9, EvseId = 2, Purpose = ChargingProfilePurposeEnum.TxProfile, Kind = ChargingProfileKindEnum.Relative,
    TransactionId = "ocpp16-1234", ChargingRateUnit = ChargingRateUnitEnum.A
};
var txPeriods = new List<ChargingProfilePeriod> { new(0, 12.5, 3) };
ChargingProfileMessages.Validate(txProfile, txPeriods, OcppProtocols.Ocpp16);
var tx16 = Wire(ChargingProfileMessages.ToOcpp16(txProfile, txPeriods));
Check(tx16["connectorId"]!.Value<int>() == 2 && tx16["csChargingProfiles"]!["transactionId"]!.Type == JTokenType.Integer
      && tx16["csChargingProfiles"]!["transactionId"]!.Value<int>() == 1234, "1.6: TxProfile transactionId is the integer of the 1.6 transaction");
Check(tx16["csChargingProfiles"]!["chargingSchedule"]!["startSchedule"] == null, "Relative profile: no startSchedule");
var tx201Profile = new ChargingProfile
{
    OcppProfileId = 9, EvseId = 2, Purpose = ChargingProfilePurposeEnum.TxProfile, Kind = ChargingProfileKindEnum.Relative,
    TransactionId = "a1b2-c3", ChargingRateUnit = ChargingRateUnitEnum.A
};
ChargingProfileMessages.Validate(tx201Profile, txPeriods, OcppProtocols.Ocpp201);
Check(Wire(ChargingProfileMessages.ToOcpp201(tx201Profile, txPeriods))["chargingProfile"]!["transactionId"]!.Value<string>() == "a1b2-c3",
    "2.0.1: TxProfile transactionId is the charger's string id");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(tx201Profile, txPeriods, OcppProtocols.Ocpp16),
    "1.6: a TxProfile for a non-1.6 transaction is refused");

Throws<ValidationException>(() => ChargingProfileMessages.Validate(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.TxProfile, EvseId = 0, TransactionId = "x" },
    txPeriods, OcppProtocols.Ocpp201), "a TxProfile on EVSE 0 is refused");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.ChargingStationMaxProfile, EvseId = 1, Kind = ChargingProfileKindEnum.Relative },
    txPeriods, OcppProtocols.Ocpp201), "a ChargingStationMaxProfile on an EVSE is refused");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(stationMax, new List<ChargingProfilePeriod> { new(60, 10) }, OcppProtocols.Ocpp201),
    "the first period must start at 0");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(stationMax, new List<ChargingProfilePeriod> { new(0, 10), new(0, 12) }, OcppProtocols.Ocpp201),
    "period starts must increase");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(stationMax, new List<ChargingProfilePeriod> { new(0, 10, 1, 2) }, OcppProtocols.Ocpp16),
    "phaseToUse is refused for 1.6");
ChargingProfileMessages.Validate(stationMax, new List<ChargingProfilePeriod> { new(0, 10, 1, 2) }, OcppProtocols.Ocpp201);
Check(true, "phaseToUse with numberPhases 1 accepted for 2.0.1");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.ChargingStationExternalConstraints, Kind = ChargingProfileKindEnum.Relative },
    txPeriods, OcppProtocols.Ocpp201), "external constraints cannot be set by the CSMS");
Throws<ValidationException>(() => ChargingProfileMessages.Validate(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.TxDefaultProfile, Kind = ChargingProfileKindEnum.Recurring, StartSchedule = t0 },
    txPeriods, OcppProtocols.Ocpp201), "a recurring profile needs its recurrence");

// Reported 2.0.1 profile (ReportChargingProfiles) back to a stored row.
var reportedJson = "{\"id\":5,\"stackLevel\":2,\"chargingProfilePurpose\":\"TxDefaultProfile\",\"chargingProfileKind\":\"Absolute\",\"chargingSchedule\":[{\"id\":5,\"startSchedule\":\"2026-10-14T08:00:00Z\",\"chargingRateUnit\":\"W\",\"chargingSchedulePeriod\":[{\"startPeriod\":0,\"limit\":7400.0,\"numberPhases\":1}]}]}";
var (reported, reportedPeriods) = ChargingProfileMessages.FromOcpp201(JsonConvert.DeserializeObject<ChargingProfileType>(reportedJson)!, 1);
Check(reported.OcppProfileId == 5 && reported.EvseId == 1 && reported.StackLevel == 2 && reported.ChargingRateUnit == ChargingRateUnitEnum.W
      && reported.StartSchedule == t0 && reportedPeriods.Single().Limit == 7400, "reported 2.0.1 profile converted to a stored row");

// ---------------------------------------------------------------- ReportChargingProfiles parts (tbc)
var tracker = new ChargingProfileReportTracker();
tracker.Register("CP1", 77, new ChargingProfileReportScope(null, null, null, null, true));
var first = tracker.AddPart("CP1", new ReportChargingProfilesRequest { RequestId = 77, Tbc = true, EvseId = 1, ChargingProfile = new() });
var last = tracker.AddPart("CP1", new ReportChargingProfilesRequest { RequestId = 77, Tbc = false, EvseId = 2, ChargingProfile = new() });
Check(!first.Complete && last.Complete && last.Parts.Count == 2 && last.Scope is { IncludesCso: true }, "report parts collected until tbc is false, with the request scope");
var unsolicited = tracker.AddPart("CP1", new ReportChargingProfilesRequest { RequestId = 5, EvseId = 0, ChargingProfile = new() });
Check(unsolicited.Complete && unsolicited.Scope == null, "an unsolicited single-part report completes without a scope");

// ---------------------------------------------------------------- strategy -> profile periods, time zones
// Casablanca is UTC+1 without DST (outside Ramadan). Host time zone data differs (Windows "Morocco Standard Time" may say
// UTC+0 in October), so the DST-free case uses an explicit +01:00 zone and the host zone is only checked for consistency.
var casablanca = TimeZoneInfo.CreateCustomTimeZone("Africa/Casablanca+01", TimeSpan.FromHours(1), "Casablanca (+01)", "Casablanca (+01)");
var hostCasablanca = BusinessClock.ResolveTimeZone("Africa/Casablanca");
var paris = BusinessClock.ResolveTimeZone("Europe/Paris");
Check(hostCasablanca != TimeZoneInfo.Utc && paris != TimeZoneInfo.Utc, "Casablanca and Paris time zones resolved on this host");

var seededOffPeak = SmartChargingJson.Deserialize<ChargingStrategyPeriod>("[{\"startSeconds\":0,\"limit\":32},{\"startSeconds\":21600,\"limit\":10},{\"startSeconds\":79200,\"limit\":32}]");
var offPeak = new ChargingStrategy { Name = "Off-peak night boost", Kind = ChargingProfileKindEnum.Recurring, RecurrencyKind = ChargingProfileRecurrencyEnum.Daily };
Check(seededOffPeak.Count == 3 && seededOffPeak[1] == new ChargingStrategyPeriod(21600, 10), "seeded strategy JSON parses");

// Casablanca: UTC+1 all year except during Ramadan; 14 October 2026 is outside it.
var shape = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, casablanca, new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc));
Check(shape.StartSchedule == new DateTime(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc) && shape.StartSchedule.Kind == DateTimeKind.Utc,
    "Casablanca daily: startSchedule = local midnight 14 Oct = 13 Oct 23:00 UTC");
Check(shape.Duration == 86_400 && shape.Kind == ChargingProfileKindEnum.Recurring && shape.RecurrencyKind == ChargingProfileRecurrencyEnum.Daily, "daily: 24 h recurring");
Check(shape.Periods.Select(p => (p.StartPeriod, p.Limit)).SequenceEqual(new[] { (0, 32.0), (21600, 10.0), (79200, 32.0) }),
    "off-peak: full 00:00-06:00, 10 A 06:00-22:00, full 22:00-24:00");
shape = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, casablanca, new DateTime(2026, 10, 14, 23, 30, 0, DateTimeKind.Utc));
Check(shape.StartSchedule == new DateTime(2026, 10, 14, 23, 0, 0, DateTimeKind.Utc), "Casablanca: 23:30 UTC is already 15 Oct locally");
var hostNow = new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);
var hostLocalMidnight = TimeZoneInfo.ConvertTimeFromUtc(hostNow, hostCasablanca).Date;
shape = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, hostCasablanca, hostNow);
Check(shape.StartSchedule == hostLocalMidnight - hostCasablanca.GetUtcOffset(shape.StartSchedule) && shape.StartSchedule.Kind == DateTimeKind.Utc,
    $"host Casablanca zone: startSchedule = host local midnight minus its offset ({hostCasablanca.GetUtcOffset(hostNow)})");

var wrapped = new List<ChargingStrategyPeriod> { new(21600, 10), new(79200, 32) };
shape = ChargingStrategyConverter.ToProfileShape(offPeak, wrapped, casablanca, t0);
Check(shape.Periods.Select(p => (p.StartPeriod, p.Limit)).SequenceEqual(new[] { (0, 32.0), (21600, 10.0), (79200, 32.0) }),
    "a period crossing midnight (22:00-06:00) is wrapped to start the day");

// Paris: UTC+2 in summer, UTC+1 in winter.
shape = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, paris, new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
Check(shape.StartSchedule == new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc), "Paris summer: local midnight = 22:00 UTC");
shape = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, paris, new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc));
Check(shape.StartSchedule == new DateTime(2026, 11, 30, 23, 0, 0, DateTimeKind.Utc), "Paris winter: local midnight = 23:00 UTC");
shape = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, paris, new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc));
Check(shape.StartSchedule == new DateTime(2026, 3, 28, 23, 0, 0, DateTimeKind.Utc), "Paris DST change day (29 Mar 2026): midnight still UTC+1");
Check(ChargingStrategyConverter.LocalToUtc(new DateTime(2026, 3, 29, 2, 30, 0), paris) == new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc),
    "a wall-clock time inside the DST gap moves to the first valid time (03:00 local)");

var weekly = new ChargingStrategy { Name = "Weekend", Kind = ChargingProfileKindEnum.Recurring, RecurrencyKind = ChargingProfileRecurrencyEnum.Weekly };
shape = ChargingStrategyConverter.ToProfileShape(weekly, new List<ChargingStrategyPeriod> { new(0, 16), new(5 * 86_400, 32) }, casablanca,
    new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc));
Check(shape.StartSchedule == new DateTime(2026, 10, 11, 23, 0, 0, DateTimeKind.Utc) && shape.Duration == 7 * 86_400,
    "weekly: starts at local Monday 00:00 of the current week (Wed 14 Oct -> Mon 12 Oct 00:00 = 11 Oct 23:00 UTC)");

var fairShare = new ChargingStrategy { Name = "Fair share 16A", Kind = ChargingProfileKindEnum.Absolute };
shape = ChargingStrategyConverter.ToProfileShape(fairShare, new List<ChargingStrategyPeriod> { new(0, 16) }, casablanca, t0.AddTicks(1234567));
Check(shape.Kind == ChargingProfileKindEnum.Absolute && shape.StartSchedule == t0.AddSeconds(0) && shape.Duration == null && shape.Periods.Single().Limit == 16,
    "absolute strategy: starts now (whole seconds), no duration");
Throws<ValidationException>(() => ChargingStrategyConverter.ToProfileShape(fairShare, new List<ChargingStrategyPeriod> { new(60, 16) }, casablanca, t0),
    "absolute strategy must start at 0");
Throws<ValidationException>(() => ChargingStrategyConverter.Validate(offPeak, new List<ChargingStrategyPeriod> { new(86_400, 16) }),
    "daily strategy period beyond 24 h refused");
Throws<ValidationException>(() => ChargingStrategyConverter.Validate(new ChargingStrategy { Name = "x", Purpose = ChargingProfilePurposeEnum.TxProfile, Kind = ChargingProfileKindEnum.Absolute },
    new List<ChargingStrategyPeriod> { new(0, 16) }), "a strategy cannot be a TxProfile");

// A generated profile passes the profile validation of both versions.
var generated = ChargingStrategyConverter.ToProfileShape(offPeak, seededOffPeak, casablanca, t0);
var generatedProfile = new ChargingProfile
{
    OcppProfileId = 1, Purpose = ChargingProfilePurposeEnum.TxDefaultProfile, Kind = generated.Kind, RecurrencyKind = generated.RecurrencyKind,
    StartSchedule = generated.StartSchedule, Duration = generated.Duration
};
ChargingProfileMessages.Validate(generatedProfile, generated.Periods, OcppProtocols.Ocpp16);
ChargingProfileMessages.Validate(generatedProfile, generated.Periods, OcppProtocols.Ocpp201);
Check(true, "strategy-generated profile is valid for 1.6 and 2.0.1");

Console.WriteLine($"All {checks} smart charging checks passed.");
