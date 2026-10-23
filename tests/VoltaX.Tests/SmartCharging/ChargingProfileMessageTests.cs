using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.SmartCharging;
using ValidationException = VoltaXApi.Exceptions.ValidationException;

namespace VoltaX.Tests.SmartCharging;

/// <summary>SetChargingProfile as each protocol version expects it on the wire, and the profile rules checked before sending.</summary>
public class ChargingProfileMessageTests
{
    private static readonly DateTime T0 = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);
    private static readonly List<ChargingProfilePeriod> TwoPeriods = new() { new(0, 16, 3), new(3600, 10.67, 3) };
    private static readonly List<ChargingProfilePeriod> OnePeriod = new() { new(0, 12.5, 3) };

    private static JObject Wire(object message) => JObject.Parse(JsonConvert.SerializeObject(message, OCPPMessageFactory.DefaultSettings));

    private static ChargingProfile DailyTxDefault() => new()
    {
        OcppProfileId = 42, EvseId = 0, StackLevel = 1, Purpose = ChargingProfilePurposeEnum.TxDefaultProfile, Kind = ChargingProfileKindEnum.Recurring,
        RecurrencyKind = ChargingProfileRecurrencyEnum.Daily, ChargingRateUnit = ChargingRateUnitEnum.A,
        StartSchedule = new DateTime(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc), Duration = 86_400
    };

    private static ChargingProfile StationMax() => new()
    {
        OcppProfileId = 7, EvseId = 0, Purpose = ChargingProfilePurposeEnum.ChargingStationMaxProfile, Kind = ChargingProfileKindEnum.Absolute,
        StartSchedule = T0, ChargingRateUnit = ChargingRateUnitEnum.W
    };

    private static ChargingProfile TxProfile(string transactionId) => new()
    {
        OcppProfileId = 9, EvseId = 2, Purpose = ChargingProfilePurposeEnum.TxProfile, Kind = ChargingProfileKindEnum.Relative,
        TransactionId = transactionId, ChargingRateUnit = ChargingRateUnitEnum.A
    };

    [Fact]
    public void A_1_6_profile_uses_connectorId_and_csChargingProfiles_with_a_single_schedule()
    {
        ChargingProfileMessages.Validate(DailyTxDefault(), TwoPeriods, OcppProtocols.Ocpp16);
        var message = ChargingProfileMessages.ToOcpp16(DailyTxDefault(), TwoPeriods);
        var json = Wire(message);

        Assert.Equal(0, json["connectorId"]!.Value<int>());
        Assert.Null(json["evseId"]);
        var profile = (JObject)json["csChargingProfiles"]!;
        Assert.Equal(42, profile["chargingProfileId"]!.Value<int>());
        Assert.Equal(1, profile["stackLevel"]!.Value<int>());
        Assert.Equal("TxDefaultProfile", profile["chargingProfilePurpose"]!.Value<string>());
        Assert.Equal("Recurring", profile["chargingProfileKind"]!.Value<string>());
        Assert.Equal("Daily", profile["recurrencyKind"]!.Value<string>());
        Assert.Null(profile["transactionId"]); // absent optionals are omitted
        Assert.Null(profile["validFrom"]);
        var schedule = Assert.IsType<JObject>(profile["chargingSchedule"]);
        Assert.Equal("A", schedule["chargingRateUnit"]!.Value<string>());
        Assert.Equal(86_400, schedule["duration"]!.Value<int>());
        Assert.Equal(new DateTime(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc), schedule["startSchedule"]!.Value<DateTime>().ToUniversalTime());
        Assert.Contains("2026-10-13T23:00:00Z", JsonConvert.SerializeObject(message, OCPPMessageFactory.DefaultSettings)); // UTC with Z
        var periods = (JArray)schedule["chargingSchedulePeriod"]!;
        Assert.Equal(2, periods.Count);
        Assert.Equal(3600, periods[1]["startPeriod"]!.Value<int>());
        Assert.Equal(10.6m, periods[1]["limit"]!.Value<decimal>()); // rounded down to 0.1
        Assert.Null(periods[1]["phaseToUse"]);
    }

    [Fact]
    public void A_2_0_1_profile_uses_evseId_and_an_array_of_identified_schedules()
    {
        var json = Wire(ChargingProfileMessages.ToOcpp201(DailyTxDefault(), TwoPeriods));

        Assert.Equal(0, json["evseId"]!.Value<int>());
        Assert.Null(json["connectorId"]);
        Assert.Null(json["csChargingProfiles"]);
        var profile = (JObject)json["chargingProfile"]!;
        Assert.Equal(42, profile["id"]!.Value<int>());
        Assert.Equal("TxDefaultProfile", profile["chargingProfilePurpose"]!.Value<string>());
        var schedule = Assert.Single(Assert.IsType<JArray>(profile["chargingSchedule"]));
        Assert.Equal(42, schedule["id"]!.Value<int>());
        Assert.Equal("A", schedule["chargingRateUnit"]!.Value<string>());
        Assert.Equal(10.6, ((JArray)schedule["chargingSchedulePeriod"]!)[1]["limit"]!.Value<double>());
    }

    [Fact]
    public void The_station_maximum_profile_keeps_its_name_per_version()
    {
        var periods = new List<ChargingProfilePeriod> { new(0, 22000) };

        Assert.Equal("ChargePointMaxProfile", Wire(ChargingProfileMessages.ToOcpp16(StationMax(), periods))["csChargingProfiles"]!["chargingProfilePurpose"]!.Value<string>());
        Assert.Equal("ChargingStationMaxProfile", Wire(ChargingProfileMessages.ToOcpp201(StationMax(), periods))["chargingProfile"]!["chargingProfilePurpose"]!.Value<string>());
    }

    [Fact]
    public void A_TxProfile_names_the_transaction_as_each_version_does()
    {
        var for16 = TxProfile("ocpp16-1234");
        ChargingProfileMessages.Validate(for16, OnePeriod, OcppProtocols.Ocpp16);
        var json16 = Wire(ChargingProfileMessages.ToOcpp16(for16, OnePeriod));
        Assert.Equal(2, json16["connectorId"]!.Value<int>());
        Assert.Equal(JTokenType.Integer, json16["csChargingProfiles"]!["transactionId"]!.Type); // the integer 1.6 transaction id
        Assert.Equal(1234, json16["csChargingProfiles"]!["transactionId"]!.Value<int>());
        Assert.Null(json16["csChargingProfiles"]!["chargingSchedule"]!["startSchedule"]); // relative profile

        var for201 = TxProfile("a1b2-c3");
        ChargingProfileMessages.Validate(for201, OnePeriod, OcppProtocols.Ocpp201);
        Assert.Equal("a1b2-c3", Wire(ChargingProfileMessages.ToOcpp201(for201, OnePeriod))["chargingProfile"]!["transactionId"]!.Value<string>());
        Assert.Throws<ValidationException>(() => ChargingProfileMessages.Validate(for201, OnePeriod, OcppProtocols.Ocpp16)); // not a 1.6 transaction
    }

    [Fact]
    public void Invalid_profiles_are_refused_before_sending()
    {
        void Refused(ChargingProfile profile, List<ChargingProfilePeriod> periods, string protocol = OcppProtocols.Ocpp201) =>
            Assert.Throws<ValidationException>(() => ChargingProfileMessages.Validate(profile, periods, protocol));

        Refused(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.TxProfile, EvseId = 0, TransactionId = "x" }, OnePeriod);           // TxProfile on EVSE 0
        Refused(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.ChargingStationMaxProfile, EvseId = 1, Kind = ChargingProfileKindEnum.Relative }, OnePeriod);
        Refused(StationMax(), new() { new(60, 10) });                       // the first period must start at 0
        Refused(StationMax(), new() { new(0, 10), new(0, 12) });            // period starts must increase
        Refused(StationMax(), new() { new(0, 10, 1, 2) }, OcppProtocols.Ocpp16); // no phaseToUse in 1.6
        Refused(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.ChargingStationExternalConstraints, Kind = ChargingProfileKindEnum.Relative }, OnePeriod);
        Refused(new ChargingProfile { Purpose = ChargingProfilePurposeEnum.TxDefaultProfile, Kind = ChargingProfileKindEnum.Recurring, StartSchedule = T0 }, OnePeriod);
    }

    [Fact]
    public void PhaseToUse_with_a_single_phase_is_accepted_for_2_0_1()
    {
        ChargingProfileMessages.Validate(StationMax(), new List<ChargingProfilePeriod> { new(0, 10, 1, 2) }, OcppProtocols.Ocpp201);
    }

    [Fact]
    public void A_reported_2_0_1_profile_becomes_a_stored_row()
    {
        const string reportedJson = "{\"id\":5,\"stackLevel\":2,\"chargingProfilePurpose\":\"TxDefaultProfile\",\"chargingProfileKind\":\"Absolute\",\"chargingSchedule\":[{\"id\":5,\"startSchedule\":\"2026-10-14T08:00:00Z\",\"chargingRateUnit\":\"W\",\"chargingSchedulePeriod\":[{\"startPeriod\":0,\"limit\":7400.0,\"numberPhases\":1}]}]}";

        var (reported, periods) = ChargingProfileMessages.FromOcpp201(JsonConvert.DeserializeObject<ChargingProfileType>(reportedJson)!, 1);

        Assert.Equal((5, 1, 2), (reported.OcppProfileId, reported.EvseId, reported.StackLevel));
        Assert.Equal(ChargingRateUnitEnum.W, reported.ChargingRateUnit);
        Assert.Equal(T0, reported.StartSchedule);
        Assert.Equal(7400, Assert.Single(periods).Limit);
    }

    [Fact]
    public void Report_parts_are_collected_until_tbc_is_false()
    {
        var tracker = new ChargingProfileReportTracker();
        tracker.Register("CP1", 77, new ChargingProfileReportScope(null, null, null, null, true));

        var first = tracker.AddPart("CP1", new ReportChargingProfilesRequest { RequestId = 77, Tbc = true, EvseId = 1, ChargingProfile = new() });
        var last = tracker.AddPart("CP1", new ReportChargingProfilesRequest { RequestId = 77, Tbc = false, EvseId = 2, ChargingProfile = new() });
        var unsolicited = tracker.AddPart("CP1", new ReportChargingProfilesRequest { RequestId = 5, EvseId = 0, ChargingProfile = new() });

        Assert.False(first.Complete);
        Assert.True(last.Complete);
        Assert.Equal(2, last.Parts.Count);
        Assert.True(last.Scope is { IncludesCso: true });
        Assert.True(unsolicited.Complete);
        Assert.Null(unsolicited.Scope);
    }
}
