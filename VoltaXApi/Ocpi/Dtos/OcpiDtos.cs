using System.Text.Json.Serialization;

namespace VoltaXApi.Ocpi.Dtos
{
    // OCPI 2.2.1 objects. Enumerations are strings so the wire values are exactly the spec values.
    public static class OcpiStatusCodes
    {
        public const int Success = 1000;
        public const int ClientError = 2000;
        public const int InvalidParameters = 2001;
        public const int NotEnoughInformation = 2002;
        public const int UnknownLocation = 2003;
        public const int UnknownToken = 2004;
        public const int ServerError = 3000;
        public const int UnableToUseClientApi = 3001;
        public const int UnsupportedVersion = 3002;
        public const int NoMatchingEndpoints = 3003;
    }

    public class OcpiResponse<T>
    {
        [JsonPropertyName("data")] public T? Data { get; set; }
        [JsonPropertyName("status_code")] public int StatusCode { get; set; }
        [JsonPropertyName("status_message")] public string? StatusMessage { get; set; }
        [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
    }

    public class VersionDto
    {
        [JsonPropertyName("version")] public string Version { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
    }

    public class VersionDetailsDto
    {
        [JsonPropertyName("version")] public string Version { get; set; } = "";
        [JsonPropertyName("endpoints")] public List<EndpointDto> Endpoints { get; set; } = new();
    }

    public class EndpointDto
    {
        [JsonPropertyName("identifier")] public string Identifier { get; set; } = "";
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
    }

    public class CredentialsDto
    {
        [JsonPropertyName("token")] public string Token { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
        [JsonPropertyName("roles")] public List<CredentialsRoleDto> Roles { get; set; } = new();
    }

    public class CredentialsRoleDto
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("business_details")] public BusinessDetailsDto BusinessDetails { get; set; } = new();
        [JsonPropertyName("party_id")] public string PartyId { get; set; } = "";
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
    }

    public class BusinessDetailsDto
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("website")] public string? Website { get; set; }
    }

    public class GeoLocationDto
    {
        [JsonPropertyName("latitude")] public string Latitude { get; set; } = "";
        [JsonPropertyName("longitude")] public string Longitude { get; set; } = "";
    }

    public class DisplayTextDto
    {
        [JsonPropertyName("language")] public string Language { get; set; } = "en";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    public class HoursDto
    {
        [JsonPropertyName("twentyfourseven")] public bool TwentyFourSeven { get; set; } = true;
    }

    public class LocationDto
    {
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
        [JsonPropertyName("party_id")] public string PartyId { get; set; } = "";
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("publish")] public bool Publish { get; set; } = true;
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; } = "";
        [JsonPropertyName("city")] public string City { get; set; } = "";
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; } = "";
        [JsonPropertyName("coordinates")] public GeoLocationDto Coordinates { get; set; } = new();
        [JsonPropertyName("evses")] public List<EvseDto>? Evses { get; set; }
        [JsonPropertyName("operator")] public BusinessDetailsDto? Operator { get; set; }
        [JsonPropertyName("facilities")] public List<string>? Facilities { get; set; }
        [JsonPropertyName("time_zone")] public string TimeZone { get; set; } = "";
        [JsonPropertyName("opening_times")] public HoursDto? OpeningTimes { get; set; }
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class EvseDto
    {
        [JsonPropertyName("uid")] public string Uid { get; set; } = "";
        [JsonPropertyName("evse_id")] public string? EvseId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = "UNKNOWN";
        [JsonPropertyName("capabilities")] public List<string>? Capabilities { get; set; }
        [JsonPropertyName("connectors")] public List<ConnectorDto> Connectors { get; set; } = new();
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class ConnectorDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("standard")] public string Standard { get; set; } = "";
        [JsonPropertyName("format")] public string Format { get; set; } = "";
        [JsonPropertyName("power_type")] public string PowerType { get; set; } = "";
        [JsonPropertyName("max_voltage")] public int MaxVoltage { get; set; }
        [JsonPropertyName("max_amperage")] public int MaxAmperage { get; set; }
        [JsonPropertyName("max_electric_power")] public int? MaxElectricPower { get; set; }
        [JsonPropertyName("tariff_ids")] public List<string>? TariffIds { get; set; }
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class EvseStatusPatchDto
    {
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class PriceComponentDto
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("price")] public decimal Price { get; set; }
        [JsonPropertyName("vat")] public decimal? Vat { get; set; }
        [JsonPropertyName("step_size")] public int StepSize { get; set; }
    }

    public class TariffElementDto
    {
        [JsonPropertyName("price_components")] public List<PriceComponentDto> PriceComponents { get; set; } = new();
    }

    public class TariffDto
    {
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
        [JsonPropertyName("party_id")] public string PartyId { get; set; } = "";
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("currency")] public string Currency { get; set; } = "";
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("tariff_alt_text")] public List<DisplayTextDto>? TariffAltText { get; set; }
        [JsonPropertyName("elements")] public List<TariffElementDto> Elements { get; set; } = new();
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class PriceDto
    {
        [JsonPropertyName("excl_vat")] public decimal ExclVat { get; set; }
        [JsonPropertyName("incl_vat")] public decimal? InclVat { get; set; }
    }

    public class CdrTokenDto
    {
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
        [JsonPropertyName("party_id")] public string PartyId { get; set; } = "";
        [JsonPropertyName("uid")] public string Uid { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("contract_id")] public string ContractId { get; set; } = "";
    }

    public class CdrDimensionDto
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("volume")] public decimal Volume { get; set; }
    }

    public class ChargingPeriodDto
    {
        [JsonPropertyName("start_date_time")] public DateTime StartDateTime { get; set; }
        [JsonPropertyName("dimensions")] public List<CdrDimensionDto> Dimensions { get; set; } = new();
        [JsonPropertyName("tariff_id")] public string? TariffId { get; set; }
    }

    public class SessionDto
    {
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
        [JsonPropertyName("party_id")] public string PartyId { get; set; } = "";
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("start_date_time")] public DateTime StartDateTime { get; set; }
        [JsonPropertyName("end_date_time")] public DateTime? EndDateTime { get; set; }
        [JsonPropertyName("kwh")] public decimal Kwh { get; set; }
        [JsonPropertyName("cdr_token")] public CdrTokenDto CdrToken { get; set; } = new();
        [JsonPropertyName("auth_method")] public string AuthMethod { get; set; } = "";
        [JsonPropertyName("authorization_reference")] public string? AuthorizationReference { get; set; }
        [JsonPropertyName("location_id")] public string LocationId { get; set; } = "";
        [JsonPropertyName("evse_uid")] public string EvseUid { get; set; } = "";
        [JsonPropertyName("connector_id")] public string ConnectorId { get; set; } = "";
        [JsonPropertyName("currency")] public string Currency { get; set; } = "";
        [JsonPropertyName("charging_periods")] public List<ChargingPeriodDto>? ChargingPeriods { get; set; }
        [JsonPropertyName("total_cost")] public PriceDto? TotalCost { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class CdrLocationDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; } = "";
        [JsonPropertyName("city")] public string City { get; set; } = "";
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; } = "";
        [JsonPropertyName("coordinates")] public GeoLocationDto Coordinates { get; set; } = new();
        [JsonPropertyName("evse_uid")] public string EvseUid { get; set; } = "";
        [JsonPropertyName("evse_id")] public string EvseId { get; set; } = "";
        [JsonPropertyName("connector_id")] public string ConnectorId { get; set; } = "";
        [JsonPropertyName("connector_standard")] public string ConnectorStandard { get; set; } = "";
        [JsonPropertyName("connector_format")] public string ConnectorFormat { get; set; } = "";
        [JsonPropertyName("connector_power_type")] public string ConnectorPowerType { get; set; } = "";
    }

    public class CdrDto
    {
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
        [JsonPropertyName("party_id")] public string PartyId { get; set; } = "";
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("start_date_time")] public DateTime StartDateTime { get; set; }
        [JsonPropertyName("end_date_time")] public DateTime EndDateTime { get; set; }
        [JsonPropertyName("session_id")] public string? SessionId { get; set; }
        [JsonPropertyName("cdr_token")] public CdrTokenDto CdrToken { get; set; } = new();
        [JsonPropertyName("auth_method")] public string AuthMethod { get; set; } = "";
        [JsonPropertyName("authorization_reference")] public string? AuthorizationReference { get; set; }
        [JsonPropertyName("cdr_location")] public CdrLocationDto CdrLocation { get; set; } = new();
        [JsonPropertyName("currency")] public string Currency { get; set; } = "";
        [JsonPropertyName("tariffs")] public List<TariffDto>? Tariffs { get; set; }
        [JsonPropertyName("charging_periods")] public List<ChargingPeriodDto> ChargingPeriods { get; set; } = new();
        [JsonPropertyName("total_cost")] public PriceDto TotalCost { get; set; } = new();
        [JsonPropertyName("total_fixed_cost")] public PriceDto? TotalFixedCost { get; set; }
        [JsonPropertyName("total_energy")] public decimal TotalEnergy { get; set; }
        [JsonPropertyName("total_energy_cost")] public PriceDto? TotalEnergyCost { get; set; }
        [JsonPropertyName("total_time")] public decimal TotalTime { get; set; }
        [JsonPropertyName("total_time_cost")] public PriceDto? TotalTimeCost { get; set; }
        [JsonPropertyName("total_parking_time")] public decimal? TotalParkingTime { get; set; }
        [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    }

    public class TokenDto
    {
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("party_id")] public string? PartyId { get; set; }
        [JsonPropertyName("uid")] public string? Uid { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("contract_id")] public string? ContractId { get; set; }
        [JsonPropertyName("visual_number")] public string? VisualNumber { get; set; }
        [JsonPropertyName("issuer")] public string? Issuer { get; set; }
        [JsonPropertyName("group_id")] public string? GroupId { get; set; }
        [JsonPropertyName("valid")] public bool? Valid { get; set; }
        [JsonPropertyName("whitelist")] public string? Whitelist { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("default_profile_type")] public string? DefaultProfileType { get; set; }
        [JsonPropertyName("last_updated")] public DateTime? LastUpdated { get; set; }
    }

    public class AuthorizationInfoDto
    {
        [JsonPropertyName("allowed")] public string? Allowed { get; set; }
        [JsonPropertyName("token")] public TokenDto? Token { get; set; }
        [JsonPropertyName("authorization_reference")] public string? AuthorizationReference { get; set; }
    }

    public class LocationReferencesDto
    {
        [JsonPropertyName("location_id")] public string LocationId { get; set; } = "";
        [JsonPropertyName("evse_uids")] public List<string>? EvseUids { get; set; }
    }

    // Union of the five command request bodies; the command in the path says which fields apply.
    public class CommandRequestDto
    {
        [JsonPropertyName("response_url")] public string? ResponseUrl { get; set; }
        [JsonPropertyName("token")] public TokenDto? Token { get; set; }
        [JsonPropertyName("location_id")] public string? LocationId { get; set; }
        [JsonPropertyName("evse_uid")] public string? EvseUid { get; set; }
        [JsonPropertyName("connector_id")] public string? ConnectorId { get; set; }
        [JsonPropertyName("authorization_reference")] public string? AuthorizationReference { get; set; }
        [JsonPropertyName("session_id")] public string? SessionId { get; set; }
        [JsonPropertyName("expiry_date")] public DateTime? ExpiryDate { get; set; }
        [JsonPropertyName("reservation_id")] public string? ReservationId { get; set; }
    }

    public class CommandResponseDto
    {
        [JsonPropertyName("result")] public string Result { get; set; } = "";
        [JsonPropertyName("timeout")] public int Timeout { get; set; }
        [JsonPropertyName("message")] public List<DisplayTextDto>? Message { get; set; }
    }

    public class CommandResultDto
    {
        [JsonPropertyName("result")] public string Result { get; set; } = "";
        [JsonPropertyName("message")] public List<DisplayTextDto>? Message { get; set; }
    }
}
