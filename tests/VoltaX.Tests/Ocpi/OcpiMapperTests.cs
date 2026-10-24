using System.Text.Json;
using System.Text.RegularExpressions;
using VoltaXApi.Models;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.Ocpi.Services;
using VoltaXApi.OCPP.Messages;

namespace VoltaX.Tests.Ocpi;

/// <summary>OCPI 2.2.1 objects published to roaming partners: locations, EVSEs, connectors, tariffs, sessions and CDRs.</summary>
public class OcpiMapperTests
{
    private static readonly string[] EvseStatuses = { "AVAILABLE", "BLOCKED", "CHARGING", "INOPERATIVE", "OUTOFORDER", "PLANNED", "REMOVED", "RESERVED", "UNKNOWN" };
    private static readonly string[] ConnectorTypes = { "CHADEMO", "CHAOJI", "DOMESTIC_A", "DOMESTIC_B", "DOMESTIC_C", "DOMESTIC_D", "DOMESTIC_E", "DOMESTIC_F", "DOMESTIC_G",
        "DOMESTIC_H", "DOMESTIC_I", "DOMESTIC_J", "DOMESTIC_K", "DOMESTIC_L", "GBT_AC", "GBT_DC", "IEC_60309_2_single_16", "IEC_60309_2_three_16",
        "IEC_60309_2_three_32", "IEC_60309_2_three_64", "IEC_62196_T1", "IEC_62196_T1_COMBO", "IEC_62196_T2", "IEC_62196_T2_COMBO",
        "IEC_62196_T3A", "IEC_62196_T3C", "NEMA_5_20", "NEMA_6_30", "NEMA_6_50", "NEMA_10_30", "NEMA_10_50", "NEMA_14_30", "NEMA_14_50",
        "PANTOGRAPH_BOTTOM_UP", "PANTOGRAPH_TOP_DOWN", "TESLA_R", "TESLA_S" };
    private static readonly string[] Capabilities = { "CHARGING_PROFILE_CAPABLE", "CHARGING_PREFERENCES_CAPABLE", "CHIP_CARD_SUPPORT", "CONTACTLESS_CARD_SUPPORT",
        "CREDIT_CARD_PAYABLE", "DEBIT_CARD_PAYABLE", "PED_TERMINAL", "REMOTE_START_STOP_CAPABLE", "RESERVABLE", "RFID_READER",
        "START_SESSION_CONNECTOR_REQUIRED", "TOKEN_GROUP_CAPABLE", "UNLOCK_CAPABLE" };
    private static readonly Regex DateTimeFormat = new(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$");

    private static readonly OcpiIdentity Id = new("MA", "EVE", "EVEON", 0.2m);
    private static readonly DateTime Updated = new(2026, 10, 1, 8, 30, 0, DateTimeKind.Utc);

    private static JsonElement Json<T>(T value) => JsonDocument.Parse(OcpiJson.Serialize(value)).RootElement;

    private static void HasFields(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            Assert.True(element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null, $"missing required field {name} in {element}");
    }

    private static Connector NewConnector(int dbId, int evse, int number, ConnectorEnumType type, double kw) => new()
    {
        ID = dbId, EvseID = evse, ConnectorID = number, ConnectorType = type, Power = kw, ChargePointID = 7,
        PricePerKWh = 3.6m, PricePerMinute = 0.12m, PricePerIdleMinute = 0.5m, FlatFee = 2.4m, UpdatedAt = Updated
    };

    /// <summary>A public station: charge point 7 (EVSE 1: CCS2 60 kW; EVSE 2: Type 2 22 kW + Type 1 7 kW) and deleted charge point 8.</summary>
    private static ChargingStation Station() => new()
    {
        ID = 42, Name = "EVEON Casablanca Marina", Address = "Boulevard des Almohades", City = "Casablanca", Country = "Morocco",
        Latitude = "33.6061", Longitude = "-7.6325", Network = ChargingStationNetworkEnum.Public, Category = ChargingStationCategoryEnum.Public,
        WifiAmenity = true, UpdatedAt = Updated,
        ChargePoints = new List<ChargePoint>
        {
            new()
            {
                ID = 7, ChargePointId = "CP-0007", ChargingStationID = 42, ShowOnMap = true, UpdatedAt = Updated,
                Connectors = new List<Connector>
                {
                    NewConnector(100, 1, 1, ConnectorEnumType.cCCS2, 60),
                    NewConnector(101, 2, 1, ConnectorEnumType.sType2, 22),
                    NewConnector(102, 2, 2, ConnectorEnumType.cType1, 7)
                }
            },
            new() { ID = 8, ChargePointId = "CP-0008", ChargingStationID = 42, IsDeleted = true, UpdatedAt = Updated,
                Connectors = new List<Connector> { NewConnector(103, 1, 1, ConnectorEnumType.cType2, 11) } }
        }
    };

    private static readonly Dictionary<int, ConnectorStatus> Statuses = new()
    {
        [100] = new() { ConnectorID = 100, LastStatus = ConnectorStatusEnumType.Occupied, UpdatedAt = Updated.AddMinutes(5) },
        [101] = new() { ConnectorID = 101, LastStatus = ConnectorStatusEnumType.Available, UpdatedAt = Updated },
        [102] = new() { ConnectorID = 102, LastStatus = ConnectorStatusEnumType.Faulted, UpdatedAt = Updated }
    };

    private static JsonElement Location() => Json(OcpiMapper.ToLocation(Id, Station(), Statuses)!);

    [Fact]
    public void A_location_carries_the_required_fields_in_OCPI_formats()
    {
        var location = Location();

        HasFields(location, "country_code", "party_id", "id", "publish", "address", "city", "country", "coordinates", "time_zone", "last_updated");
        Assert.Equal("42", location.GetProperty("id").GetString());
        Assert.Equal("MA", location.GetProperty("country_code").GetString());
        Assert.Equal("EVE", location.GetProperty("party_id").GetString());
        Assert.Equal("MAR", location.GetProperty("country").GetString()); // ISO 3166 alpha-3
        Assert.Matches(@"^-?[0-9]{1,2}\.[0-9]{5,7}$", location.GetProperty("coordinates").GetProperty("latitude").GetString());
        Assert.Matches(@"^-?[0-9]{1,3}\.[0-9]{5,7}$", location.GetProperty("coordinates").GetProperty("longitude").GetString());
        Assert.Matches(DateTimeFormat, location.GetProperty("last_updated").GetString());
        Assert.Equal("2026-10-01T08:35:00Z", location.GetProperty("last_updated").GetString()); // the newest change below it
        Assert.Equal(new[] { "WIFI" }, location.GetProperty("facilities").EnumerateArray().Select(f => f.GetString()));
        Assert.Equal("EVEON Casablanca Marina", location.GetProperty("name").GetString());
        Assert.DoesNotContain("\"parking_type\":null", OcpiJson.Serialize(OcpiMapper.ToLocation(Id, Station(), Statuses))); // null optionals omitted
    }

    [Fact]
    public void Each_OCPP_EVSE_is_an_OCPI_EVSE_with_its_aggregated_status()
    {
        var evses = Location().GetProperty("evses").EnumerateArray().ToList();

        Assert.Equal(3, evses.Count); // the deleted charge point's EVSE is published as REMOVED
        Assert.All(evses, e => HasFields(e, "uid", "status", "connectors", "last_updated"));
        Assert.All(evses, e => Assert.Contains(e.GetProperty("status").GetString(), EvseStatuses));
        Assert.All(evses.SelectMany(e => e.GetProperty("capabilities").EnumerateArray()), c => Assert.Contains(c.GetString(), Capabilities));
        Assert.Equal("7-1", evses[0].GetProperty("uid").GetString());
        Assert.Equal("CHARGING", evses[0].GetProperty("status").GetString());  // Occupied connector
        Assert.Equal("AVAILABLE", evses[1].GetProperty("status").GetString()); // Available beats Faulted on one EVSE
        Assert.Equal("REMOVED", evses[2].GetProperty("status").GetString());
        Assert.Matches(@"^[A-Z]{2}\*?[A-Z0-9]{3}\*?E[A-Z0-9\*]{1,30}$", evses[0].GetProperty("evse_id").GetString()); // eMI3
    }

    [Fact]
    public void Connectors_use_OCPI_standards_formats_and_power_types()
    {
        var connectors = Location().GetProperty("evses").EnumerateArray().SelectMany(e => e.GetProperty("connectors").EnumerateArray()).ToList();

        Assert.All(connectors, c => HasFields(c, "id", "standard", "format", "power_type", "max_voltage", "max_amperage", "last_updated"));
        Assert.All(connectors, c => Assert.Contains(c.GetProperty("standard").GetString(), ConnectorTypes));
        Assert.All(connectors, c => Assert.Contains(c.GetProperty("format").GetString(), new[] { "SOCKET", "CABLE" }));
        Assert.All(connectors, c => Assert.Contains(c.GetProperty("power_type").GetString(), new[] { "AC_1_PHASE", "AC_2_PHASE", "AC_2_PHASE_SPLIT", "AC_3_PHASE", "DC" }));

        var ccs = connectors[0];
        Assert.Equal("IEC_62196_T2_COMBO", ccs.GetProperty("standard").GetString());
        Assert.Equal("DC", ccs.GetProperty("power_type").GetString());
        Assert.Equal(60000, ccs.GetProperty("max_electric_power").GetInt32());
        Assert.Equal("100", ccs.GetProperty("tariff_ids").EnumerateArray().Single().GetString());
        Assert.Equal("AC_3_PHASE", connectors[1].GetProperty("power_type").GetString()); // 22 kW Type 2
        Assert.Equal(32, connectors[1].GetProperty("max_amperage").GetInt32());
        Assert.Equal("AC_1_PHASE", connectors[2].GetProperty("power_type").GetString()); // Type 1 cable
        Assert.Equal("CABLE", connectors[2].GetProperty("format").GetString());
    }

    [Fact]
    public void Every_OCPP_connector_type_maps_to_an_OCPI_connector_type()
    {
        Assert.All(Enum.GetValues<ConnectorEnumType>(), type => Assert.Contains(OcpiMapper.ConnectorKind(type).Standard, ConnectorTypes));
    }

    [Fact]
    public void Stations_and_charge_points_that_are_not_public_are_not_published()
    {
        Assert.Null(OcpiMapper.ToLocation(Id, new ChargingStation { ID = 1, Latitude = null, Longitude = "x" }, Statuses));
        Assert.False(OcpiMapper.IsPublished(new ChargingStation { Network = ChargingStationNetworkEnum.Private }));
        Assert.False(OcpiMapper.IsPublished(new ChargePoint { ShowOnMap = false }));
    }

    [Theory]
    [InlineData(ChargingStationStatusEnum.UnderMaintenance, ChargePointStatusEnum.Available, ConnectorStatusEnumType.Available, "INOPERATIVE")]
    [InlineData(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Offline, ConnectorStatusEnumType.Available, "UNKNOWN")]
    [InlineData(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Available, ConnectorStatusEnumType.Reserved, "RESERVED")]
    [InlineData(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Available, ConnectorStatusEnumType.Faulted, "OUTOFORDER")]
    [InlineData(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Available, null, "UNKNOWN")]
    public void EVSE_status_follows_station_charge_point_and_connector(ChargingStationStatusEnum station, ChargePointStatusEnum point,
        ConnectorStatusEnumType? connector, string expected)
    {
        Assert.Equal(expected, OcpiMapper.EvseStatus(station, point, new[] { connector }));
    }

    [Fact]
    public void EVSE_uids_round_trip()
    {
        Assert.True(OcpiMapper.TryParseEvseUid("7-2", out var chargePointId, out var evse));
        Assert.Equal((7, 2), (chargePointId, evse));
        Assert.False(OcpiMapper.TryParseEvseUid("7", out _, out _));
    }

    [Fact]
    public void A_tariff_publishes_every_price_excluding_VAT()
    {
        var tariff = Json(OcpiMapper.ToTariff(Id, Station().ChargePoints!.First().Connectors!.First()));

        HasFields(tariff, "country_code", "party_id", "id", "currency", "elements", "last_updated");
        Assert.Equal("MAD", tariff.GetProperty("currency").GetString());
        var components = tariff.GetProperty("elements")[0].GetProperty("price_components").EnumerateArray().ToList();
        Assert.All(components, c => HasFields(c, "type", "price", "step_size"));
        Assert.Equal(new[] { "FLAT", "ENERGY", "TIME", "PARKING_TIME" }, components.Select(c => c.GetProperty("type").GetString()));
        var energy = components.Single(c => c.GetProperty("type").GetString() == "ENERGY");
        Assert.Equal(3m, energy.GetProperty("price").GetDecimal());   // 3.60 incl. 20 % VAT
        Assert.Equal(20m, energy.GetProperty("vat").GetDecimal());
        var time = components.Single(c => c.GetProperty("type").GetString() == "TIME");
        Assert.Equal(6m, time.GetProperty("price").GetDecimal());     // per hour, billed per minute
        Assert.Equal(60, time.GetProperty("step_size").GetInt32());
    }

    [Fact]
    public void A_free_connector_still_has_one_price_component()
    {
        var free = Json(OcpiMapper.ToTariff(Id, new Connector { ID = 5 }));
        Assert.Equal(1, free.GetProperty("elements")[0].GetProperty("price_components").GetArrayLength());
    }

    private static OcpiSession Session() => new()
    {
        ID = 900, OcpiPartyID = 1, TokenCountryCode = "NL", TokenPartyId = "EMS", TokenUid = "04A1B2C3", TokenType = "RFID", ContractId = "NL-EMS-C12345678-X",
        AuthMethod = "WHITELIST", ChargingStationID = 42, ChargePointID = 7, EvseId = 1, ConnectorID = 100, TransactionUid = "tx-1",
        Status = OcpiSessionStatus.Completed, StartDateTime = Updated, EndDateTime = Updated.AddMinutes(30), Kwh = 10,
        PricePerKWh = 3.6m, PricePerMinute = 0.12m, FlatFee = 2.4m, VatRate = 0.2m, LastUpdated = Updated.AddMinutes(31)
    };

    [Fact]
    public void A_session_references_its_location_EVSE_and_token()
    {
        var session = Json(OcpiMapper.ToSession(Id, Session(), Station().ChargePoints!.First().Connectors!.First()));

        HasFields(session, "country_code", "party_id", "id", "start_date_time", "kwh", "cdr_token", "auth_method", "location_id", "evse_uid",
            "connector_id", "currency", "status", "last_updated");
        Assert.Equal("COMPLETED", session.GetProperty("status").GetString());
        Assert.Equal("7-1", session.GetProperty("evse_uid").GetString());
        Assert.Equal("42", session.GetProperty("location_id").GetString());
        HasFields(session.GetProperty("cdr_token"), "country_code", "party_id", "uid", "type", "contract_id");
    }

    [Fact]
    public void A_CDR_totals_follow_the_published_tariff()
    {
        var station = Station();
        var point = station.ChargePoints!.First();
        var cdr = Json(OcpiMapper.ToCdr(Id, Session(), station, point, point.Connectors!.First()));

        HasFields(cdr, "country_code", "party_id", "id", "start_date_time", "end_date_time", "cdr_token", "auth_method", "cdr_location", "currency",
            "charging_periods", "total_cost", "total_energy", "total_time", "last_updated");
        HasFields(cdr.GetProperty("cdr_location"), "id", "address", "city", "country", "coordinates", "evse_uid", "evse_id", "connector_id",
            "connector_standard", "connector_format", "connector_power_type");
        var period = cdr.GetProperty("charging_periods")[0];
        HasFields(period, "start_date_time", "dimensions");
        Assert.All(period.GetProperty("dimensions").EnumerateArray(), d => Assert.Contains(d.GetProperty("type").GetString(), new[] { "ENERGY", "TIME" }));
        // 10 kWh x 3.6 + 30 min x 0.12 + 2.4 flat = 42.00 incl. VAT, 35.00 excl.
        Assert.Equal(42m, cdr.GetProperty("total_cost").GetProperty("incl_vat").GetDecimal());
        Assert.Equal(35m, cdr.GetProperty("total_cost").GetProperty("excl_vat").GetDecimal());
        Assert.Equal(10m, cdr.GetProperty("total_energy").GetDecimal());
        Assert.Equal(0.5m, cdr.GetProperty("total_time").GetDecimal()); // hours
        Assert.Equal(1, cdr.GetProperty("tariffs").GetArrayLength());    // the tariff snapshot used
    }
}
