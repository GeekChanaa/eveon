using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using VoltaXApi.Models;
using VoltaXApi.Ocpi;
using VoltaXApi.Ocpi.Controllers;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.Ocpi.Services;
using VoltaXApi.OCPP.Messages;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

JsonElement Json<T>(T value) => JsonDocument.Parse(OcpiJson.Serialize(value)).RootElement;
bool Has(JsonElement e, params string[] names) => names.All(n => e.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null);
var dateTime = new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$");
string[] evseStatuses = { "AVAILABLE", "BLOCKED", "CHARGING", "INOPERATIVE", "OUTOFORDER", "PLANNED", "REMOVED", "RESERVED", "UNKNOWN" };
string[] connectorTypes = { "CHADEMO", "CHAOJI", "DOMESTIC_A", "DOMESTIC_B", "DOMESTIC_C", "DOMESTIC_D", "DOMESTIC_E", "DOMESTIC_F", "DOMESTIC_G",
    "DOMESTIC_H", "DOMESTIC_I", "DOMESTIC_J", "DOMESTIC_K", "DOMESTIC_L", "GBT_AC", "GBT_DC", "IEC_60309_2_single_16", "IEC_60309_2_three_16",
    "IEC_60309_2_three_32", "IEC_60309_2_three_64", "IEC_62196_T1", "IEC_62196_T1_COMBO", "IEC_62196_T2", "IEC_62196_T2_COMBO",
    "IEC_62196_T3A", "IEC_62196_T3C", "NEMA_5_20", "NEMA_6_30", "NEMA_6_50", "NEMA_10_30", "NEMA_10_50", "NEMA_14_30", "NEMA_14_50",
    "PANTOGRAPH_BOTTOM_UP", "PANTOGRAPH_TOP_DOWN", "TESLA_R", "TESLA_S" };
string[] capabilities = { "CHARGING_PROFILE_CAPABLE", "CHARGING_PREFERENCES_CAPABLE", "CHIP_CARD_SUPPORT", "CONTACTLESS_CARD_SUPPORT",
    "CREDIT_CARD_PAYABLE", "DEBIT_CARD_PAYABLE", "PED_TERMINAL", "REMOTE_START_STOP_CAPABLE", "RESERVABLE", "RFID_READER",
    "START_SESSION_CONNECTOR_REQUIRED", "TOKEN_GROUP_CAPABLE", "UNLOCK_CAPABLE" };
string[] priceComponentTypes = { "ENERGY", "FLAT", "PARKING_TIME", "TIME" };

var id = new OcpiIdentity("MA", "EVE", "EVEON", 0.2);
var updated = new DateTime(2026, 10, 1, 8, 30, 0, DateTimeKind.Utc);
Connector NewConnector(int dbId, int evse, int number, ConnectorEnumType type, double kw) => new()
{
    ID = dbId, EvseID = evse, ConnectorID = number, ConnectorType = type, Power = kw, ChargePointID = 7,
    PricePerKWh = 3.6, PricePerMinute = 0.12, PricePerIdleMinute = 0.5, FlatFee = 2.4, UpdatedAt = updated
};
var station = new ChargingStation
{
    ID = 42, Name = "EVEON Casablanca Marina", Address = "Boulevard des Almohades", City = "Casablanca", Country = "Morocco",
    Latitude = "33.6061", Longitude = "-7.6325", Network = ChargingStationNetworkEnum.Public, Category = ChargingStationCategoryEnum.Public,
    WifiAmenity = true, UpdatedAt = updated,
    ChargePoints = new List<ChargePoint>
    {
        new()
        {
            ID = 7, ChargePointId = "CP-0007", ChargingStationID = 42, ShowOnMap = true, UpdatedAt = updated,
            Connectors = new List<Connector>
            {
                NewConnector(100, 1, 1, ConnectorEnumType.cCCS2, 60),
                NewConnector(101, 2, 1, ConnectorEnumType.sType2, 22),
                NewConnector(102, 2, 2, ConnectorEnumType.cType1, 7)
            }
        },
        new() { ID = 8, ChargePointId = "CP-0008", ChargingStationID = 42, IsDeleted = true, UpdatedAt = updated,
            Connectors = new List<Connector> { NewConnector(103, 1, 1, ConnectorEnumType.cType2, 11) } }
    }
};
var statuses = new Dictionary<int, ConnectorStatus>
{
    [100] = new() { ConnectorID = 100, LastStatus = ConnectorStatusEnumType.Occupied, UpdatedAt = updated.AddMinutes(5) },
    [101] = new() { ConnectorID = 101, LastStatus = ConnectorStatusEnumType.Available, UpdatedAt = updated },
    [102] = new() { ConnectorID = 102, LastStatus = ConnectorStatusEnumType.Faulted, UpdatedAt = updated }
};

// ---- Location / EVSE / Connector ----
var location = OcpiMapper.ToLocation(id, station, statuses)!;
var lj = Json(location);
Check(Has(lj, "country_code", "party_id", "id", "publish", "address", "city", "country", "coordinates", "time_zone", "last_updated"), "Location has the required OCPI fields");
Check(lj.GetProperty("id").GetString() == "42" && lj.GetProperty("country_code").GetString() == "MA" && lj.GetProperty("party_id").GetString() == "EVE", "Location id and party derived from DB id and config");
Check(lj.GetProperty("country").GetString() == "MAR", "Location country is ISO 3166 alpha-3");
Check(Regex.IsMatch(lj.GetProperty("coordinates").GetProperty("latitude").GetString()!, @"^-?[0-9]{1,2}\.[0-9]{5,7}$")
      && Regex.IsMatch(lj.GetProperty("coordinates").GetProperty("longitude").GetString()!, @"^-?[0-9]{1,3}\.[0-9]{5,7}$"), "Coordinates match the OCPI GeoLocation format");
Check(dateTime.IsMatch(lj.GetProperty("last_updated").GetString()!), "DateTime is UTC with Z suffix");
Check(lj.GetProperty("last_updated").GetString() == "2026-10-01T08:35:00Z", "Location last_updated is the newest change below it");
Check(lj.GetProperty("facilities").EnumerateArray().Select(f => f.GetString()).SequenceEqual(new[] { "WIFI" }), "Facilities mapped to OCPI values");
var evses = lj.GetProperty("evses").EnumerateArray().ToList();
Check(evses.Count == 3, "One EVSE per OCPP EVSE of each charge point (deleted ones included as REMOVED)");
Check(evses.All(e => Has(e, "uid", "status", "connectors", "last_updated")), "EVSE has the required OCPI fields");
Check(evses.All(e => evseStatuses.Contains(e.GetProperty("status").GetString())), "EVSE status values are OCPI enum values");
Check(evses.SelectMany(e => e.GetProperty("capabilities").EnumerateArray()).All(c => capabilities.Contains(c.GetString())), "Capabilities are OCPI enum values");
Check(evses[0].GetProperty("uid").GetString() == "7-1" && evses[0].GetProperty("status").GetString() == "CHARGING", "Occupied connector => EVSE CHARGING, uid from DB ids");
Check(evses[1].GetProperty("status").GetString() == "AVAILABLE", "Available beats Faulted on a multi-connector EVSE");
Check(evses[2].GetProperty("status").GetString() == "REMOVED", "Soft deleted charge point => REMOVED");
Check(Regex.IsMatch(evses[0].GetProperty("evse_id").GetString()!, @"^[A-Z]{2}\*?[A-Z0-9]{3}\*?E[A-Z0-9\*]{1,30}$"), "evse_id follows the eMI3 format");
var connectors = evses.SelectMany(e => e.GetProperty("connectors").EnumerateArray()).ToList();
Check(connectors.All(c => Has(c, "id", "standard", "format", "power_type", "max_voltage", "max_amperage", "last_updated")), "Connector has the required OCPI fields");
Check(connectors.All(c => connectorTypes.Contains(c.GetProperty("standard").GetString())), "Connector standards are OCPI ConnectorType values");
Check(connectors.All(c => c.GetProperty("format").GetString() is "SOCKET" or "CABLE"), "Connector format is SOCKET or CABLE");
Check(connectors.All(c => c.GetProperty("power_type").GetString() is "AC_1_PHASE" or "AC_2_PHASE" or "AC_2_PHASE_SPLIT" or "AC_3_PHASE" or "DC"), "Power type is an OCPI value");
var ccs = connectors[0];
Check(ccs.GetProperty("standard").GetString() == "IEC_62196_T2_COMBO" && ccs.GetProperty("power_type").GetString() == "DC"
      && ccs.GetProperty("max_electric_power").GetInt32() == 60000, "CCS2 60 kW => T2 combo, DC, 60000 W");
Check(connectors[1].GetProperty("power_type").GetString() == "AC_3_PHASE" && connectors[1].GetProperty("max_amperage").GetInt32() == 32, "22 kW Type 2 => AC 3 phase 32 A");
Check(connectors[2].GetProperty("power_type").GetString() == "AC_1_PHASE" && connectors[2].GetProperty("format").GetString() == "CABLE", "Type 1 cable => AC 1 phase cable");
Check(ccs.GetProperty("tariff_ids").EnumerateArray().Single().GetString() == "100", "Connector points at its tariff");
foreach (ConnectorEnumType type in Enum.GetValues<ConnectorEnumType>())
    if (!connectorTypes.Contains(OcpiMapper.ConnectorKind(type).Standard)) throw new Exception("FAILED: unmapped connector type " + type);
Check(true, "Every OCPP ConnectorEnumType maps to an OCPI ConnectorType");
Check(!lj.TryGetProperty("name", out var _) || lj.GetProperty("name").GetString() == station.Name, "Optional fields serialized with their spec name");
Check(!OcpiJson.Serialize(location).Contains("\"parking_type\":null"), "Null optional fields are omitted");

Check(OcpiMapper.ToLocation(id, new ChargingStation { ID = 1, Latitude = null, Longitude = "x" }, statuses) == null, "Station without coordinates is not published");
Check(!OcpiMapper.IsPublished(new ChargingStation { Network = ChargingStationNetworkEnum.Private }), "Private network stations are not roamed");
Check(!OcpiMapper.IsPublished(new ChargePoint { ShowOnMap = false }), "Charge points hidden from the map are not roamed");
Check(OcpiMapper.EvseStatus(ChargingStationStatusEnum.UnderMaintenance, ChargePointStatusEnum.Available, new ConnectorStatusEnumType?[] { ConnectorStatusEnumType.Available }) == "INOPERATIVE", "Station under maintenance => INOPERATIVE");
Check(OcpiMapper.EvseStatus(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Offline, new ConnectorStatusEnumType?[] { ConnectorStatusEnumType.Available }) == "UNKNOWN", "Offline charge point => UNKNOWN");
Check(OcpiMapper.EvseStatus(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Available, new ConnectorStatusEnumType?[] { ConnectorStatusEnumType.Reserved }) == "RESERVED", "Reserved => RESERVED");
Check(OcpiMapper.EvseStatus(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Available, new ConnectorStatusEnumType?[] { ConnectorStatusEnumType.Faulted }) == "OUTOFORDER", "Faulted => OUTOFORDER");
Check(OcpiMapper.EvseStatus(ChargingStationStatusEnum.Available, ChargePointStatusEnum.Available, new ConnectorStatusEnumType?[] { null }) == "UNKNOWN", "No status yet => UNKNOWN");
Check(OcpiMapper.TryParseEvseUid("7-2", out var cpId, out var evseNo) && cpId == 7 && evseNo == 2 && !OcpiMapper.TryParseEvseUid("7", out _, out _), "EVSE uid round trip");

// ---- Tariff ----
var tariff = Json(OcpiMapper.ToTariff(id, station.ChargePoints!.First().Connectors!.First()));
Check(Has(tariff, "country_code", "party_id", "id", "currency", "elements", "last_updated"), "Tariff has the required OCPI fields");
Check(tariff.GetProperty("currency").GetString() == "MAD", "Tariff currency is MAD");
var components = tariff.GetProperty("elements")[0].GetProperty("price_components").EnumerateArray().ToList();
Check(components.All(c => Has(c, "type", "price", "step_size") && priceComponentTypes.Contains(c.GetProperty("type").GetString())), "Price components have OCPI types");
Check(components.Select(c => c.GetProperty("type").GetString()).SequenceEqual(new[] { "FLAT", "ENERGY", "TIME", "PARKING_TIME" }), "Flat, per kWh, per minute and idle pricing all published");
var energy = components.Single(c => c.GetProperty("type").GetString() == "ENERGY");
Check(energy.GetProperty("price").GetDecimal() == 3m && energy.GetProperty("vat").GetDecimal() == 20m, "Prices exclude VAT (3.60 incl. 20% => 3.00) and carry the VAT rate");
var time = components.Single(c => c.GetProperty("type").GetString() == "TIME");
Check(time.GetProperty("price").GetDecimal() == 6m && time.GetProperty("step_size").GetInt32() == 60, "Per minute price expressed per hour, billed per minute");
var free = Json(OcpiMapper.ToTariff(id, new Connector { ID = 5 }));
Check(free.GetProperty("elements")[0].GetProperty("price_components").GetArrayLength() == 1, "A free connector still has one price component");

// ---- Session / CDR ----
var session = new OcpiSession
{
    ID = 900, OcpiPartyID = 1, TokenCountryCode = "NL", TokenPartyId = "EMS", TokenUid = "04A1B2C3", TokenType = "RFID", ContractId = "NL-EMS-C12345678-X",
    AuthMethod = "WHITELIST", ChargingStationID = 42, ChargePointID = 7, EvseId = 1, ConnectorID = 100, TransactionUid = "tx-1",
    Status = OcpiSessionStatus.Completed, StartDateTime = updated, EndDateTime = updated.AddMinutes(30), Kwh = 10,
    PricePerKWh = 3.6, PricePerMinute = 0.12, FlatFee = 2.4, VatRate = 0.2, LastUpdated = updated.AddMinutes(31)
};
var sj = Json(OcpiMapper.ToSession(id, session, station.ChargePoints!.First().Connectors!.First()));
Check(Has(sj, "country_code", "party_id", "id", "start_date_time", "kwh", "cdr_token", "auth_method", "location_id", "evse_uid", "connector_id", "currency", "status", "last_updated"), "Session has the required OCPI fields");
Check(sj.GetProperty("status").GetString() == "COMPLETED" && sj.GetProperty("evse_uid").GetString() == "7-1" && sj.GetProperty("location_id").GetString() == "42", "Session references the location, EVSE and status");
Check(Has(sj.GetProperty("cdr_token"), "country_code", "party_id", "uid", "type", "contract_id"), "cdr_token has the required fields");
var connectorEntity = station.ChargePoints!.First().Connectors!.First();
var cj = Json(OcpiMapper.ToCdr(id, session, station, station.ChargePoints!.First(), connectorEntity));
Check(Has(cj, "country_code", "party_id", "id", "start_date_time", "end_date_time", "cdr_token", "auth_method", "cdr_location", "currency",
    "charging_periods", "total_cost", "total_energy", "total_time", "last_updated"), "CDR has the required OCPI fields");
Check(Has(cj.GetProperty("cdr_location"), "id", "address", "city", "country", "coordinates", "evse_uid", "evse_id", "connector_id",
    "connector_standard", "connector_format", "connector_power_type"), "cdr_location has the required fields");
var period = cj.GetProperty("charging_periods")[0];
Check(Has(period, "start_date_time", "dimensions") && period.GetProperty("dimensions").EnumerateArray()
    .All(d => d.GetProperty("type").GetString() is "ENERGY" or "TIME"), "Charging period dimensions use CdrDimensionType values");
// 10 kWh x 3.6 + 30 min x 0.12 + 2.4 flat = 42.00 incl. VAT, 35.00 excl.
Check(cj.GetProperty("total_cost").GetProperty("incl_vat").GetDecimal() == 42m && cj.GetProperty("total_cost").GetProperty("excl_vat").GetDecimal() == 35m, "CDR total cost follows the published tariff");
Check(cj.GetProperty("total_energy").GetDecimal() == 10m && cj.GetProperty("total_time").GetDecimal() == 0.5m, "CDR totals in kWh and hours");
Check(cj.GetProperty("tariffs").GetArrayLength() == 1, "CDR embeds the tariff snapshot used");

// ---- Response envelope and pagination ----
var envelope = (JsonResult)OcpiControllerBase.Envelope(new List<int> { 1 }, OcpiStatusCodes.Success, null, 200);
var ej = JsonDocument.Parse(JsonSerializer.Serialize(envelope.Value, envelope.Value!.GetType(), OcpiJson.Options)).RootElement;
Check(Has(ej, "data", "status_code", "status_message", "timestamp") && ej.GetProperty("status_code").GetInt32() == 1000, "Envelope has data, status_code, status_message, timestamp");
Check(dateTime.IsMatch(ej.GetProperty("timestamp").GetString()!), "Envelope timestamp format");
var error = (JsonResult)OcpiControllerBase.Envelope<object>(null, OcpiStatusCodes.UnknownLocation, "Unknown location.", 404);
var errorJson = JsonDocument.Parse(JsonSerializer.Serialize(error.Value, error.Value!.GetType(), OcpiJson.Options)).RootElement;
Check(error.StatusCode == 404 && errorJson.GetProperty("status_code").GetInt32() == 2003 && !errorJson.TryGetProperty("data", out _), "Error envelope: 2003 without data");
Check(OcpiStatusCodes.ClientError == 2000 && OcpiStatusCodes.InvalidParameters == 2001 && OcpiStatusCodes.UnknownLocation == 2003 && OcpiStatusCodes.UnknownToken == 2004, "OCPI 2.2.1 status code values");

var http = new DefaultHttpContext();
http.Request.Scheme = "https";
http.Request.Host = new HostString("api.eveon.ma");
http.Request.Path = "/ocpi/cpo/2.2.1/locations";
http.Request.QueryString = new QueryString("?date_from=2026-10-01T00:00:00Z&offset=0&limit=2");
var page = OcpiPagination.Apply(http.Request, http.Response, 5, 0, 2);
Check(page.Offset == 0 && page.Limit == 2, "Page bounds from offset/limit");
Check(http.Response.Headers["X-Total-Count"] == "5" && http.Response.Headers["X-Limit"] == "100", "X-Total-Count and X-Limit headers");
var link = http.Response.Headers["Link"].ToString();
Check(link.StartsWith("<https://api.eveon.ma/ocpi/cpo/2.2.1/locations?") && link.Contains("offset=2") && link.Contains("limit=2")
      && link.Contains("date_from=") && link.EndsWith(">; rel=\"next\""), "Link header points to the next page and keeps the filters");
var last = new DefaultHttpContext();
OcpiPagination.Apply(last.Request, last.Response, 5, 4, 2);
Check(!last.Response.Headers.ContainsKey("Link"), "No Link header on the last page");
Check(OcpiPagination.Apply(new DefaultHttpContext().Request, new DefaultHttpContext().Response, 500, -3, 1000) == new OcpiPage(0, 100), "Limit capped at 100, negative offset ignored");

// ---- Tokens and authentication ----
var token = OcpiTokens.Generate();
Check(token.Length is >= 32 and <= 64 && OcpiTokens.Generate() != token, "Generated tokens are random and fit OCPI string(64)");
Check(OcpiTokens.FromHeader(OcpiTokens.ToHeader(token)).Contains(token), "Base64 Token header (2.2.1) decodes to the token");
Check(OcpiTokens.FromHeader("Token " + token).Contains(token), "Raw Token header (pre 2.2) is accepted");
Check(OcpiTokens.FromHeader("Bearer " + token).Count == 0 && OcpiTokens.FromHeader(null).Count == 0, "Other schemes are ignored");
Check(OcpiTokens.Hash(token).Length == 64 && OcpiTokens.Hash(token) == OcpiTokens.Hash(token), "Token hash is stable SHA-256 hex");

async Task<IActionResult?> RunFilter(bool enabled, string? authorization)
{
    var context = new DefaultHttpContext();
    if (authorization != null) context.Request.Headers.Authorization = authorization;
    var executing = new ActionExecutingContext(new ActionContext(context, new RouteData(), new ActionDescriptor()),
        new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    var filter = new OcpiAuthFilter(false, null!, Options.Create(new OcpiOptions { Enabled = enabled }));
    await filter.OnActionExecutionAsync(executing, () => throw new Exception("next() must not run"));
    return executing.Result;
}
Check(await RunFilter(false, null) is NotFoundResult, "Ocpi:Enabled=false => 404");
Check(await RunFilter(true, null) is JsonResult { StatusCode: 401 }, "Missing token => 401 with OCPI envelope");
Check(await RunFilter(true, "Bearer abc") is JsonResult { StatusCode: 401 }, "Non OCPI scheme => 401");

var options = new OcpiOptions { BaseUrl = "https://api.eveon.ma/" };
var details = Json(OcpiVersionsController.BuildDetails(options));
Check(details.GetProperty("version").GetString() == "2.2.1", "Version details for 2.2.1");
Check(details.GetProperty("endpoints").EnumerateArray().All(e => Has(e, "identifier", "role", "url") && e.GetProperty("role").GetString() is "SENDER" or "RECEIVER"), "Endpoints have identifier, role and url");
Check(details.GetProperty("endpoints").EnumerateArray().Any(e => e.GetProperty("url").GetString() == "https://api.eveon.ma/ocpi/cpo/2.2.1/locations"), "Module URLs built from Ocpi:BaseUrl");

// ---- Worker and meter helpers ----
Check(OcpiPushWorker.Backoff(1) == TimeSpan.FromSeconds(30) && OcpiPushWorker.Backoff(3) == TimeSpan.FromMinutes(2) && OcpiPushWorker.Backoff(20) == TimeSpan.FromHours(1), "Outbox retry backoff is exponential, capped at 1 h");
var meter = new List<MeterValueType>
{
    new() { Timestamp = updated, SampledValue = new() { new() { Value = 12500, Measurand = MeasurandEnumType.Energy_Active_Import_Register, UnitOfMeasure = new() { Unit = "Wh" } } } },
    new() { Timestamp = updated.AddMinutes(1), SampledValue = new() { new() { Value = 13.0, Measurand = MeasurandEnumType.Energy_Active_Import_Register, UnitOfMeasure = new() { Unit = "kWh" } } } }
};
Check(OcpiTransactionObserver.EnergyKwh(meter) == 13.0, "Energy register read in kWh from Wh or kWh samples");
Check(OcppOcpiCommandExecutor.IdTokenType("RFID") == IdTokenEnumType.ISO14443 && OcppOcpiCommandExecutor.IdTokenType("APP_USER") == IdTokenEnumType.Central, "OCPI token types mapped to OCPP IdToken types");

Console.WriteLine($"{checks} checks passed");
