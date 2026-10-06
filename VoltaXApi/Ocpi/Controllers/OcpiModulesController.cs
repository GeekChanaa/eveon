using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Services;

namespace VoltaXApi.Ocpi.Controllers
{
    // CPO Sender interfaces: Locations, Tariffs, Sessions, CDRs.
    [Route("ocpi/cpo/" + OcpiOptions.VersionNumber)]
    [OcpiAuthorize]
    public class OcpiCpoSenderController : OcpiControllerBase
    {
        private readonly OcpiDataService _data;

        public OcpiCpoSenderController(OcpiDataService data)
        {
            _data = data;
        }

        [HttpGet("locations")]
        public async Task<IActionResult> Locations([FromQuery(Name = "date_from")] string? dateFrom, [FromQuery(Name = "date_to")] string? dateTo,
            [FromQuery] int? offset, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            if (!TryDates(dateFrom, dateTo, out var from, out var to)) return InvalidDates();
            return Paged(await _data.LocationsAsync(from, to, cancellationToken), offset, limit);
        }

        [HttpGet("locations/{locationId}")]
        public async Task<IActionResult> Location(string locationId, CancellationToken cancellationToken)
        {
            var location = await _data.LocationAsync(locationId, cancellationToken);
            return location == null ? UnknownLocation() : Ocpi(location);
        }

        [HttpGet("locations/{locationId}/{evseUid}")]
        public async Task<IActionResult> Evse(string locationId, string evseUid, CancellationToken cancellationToken)
        {
            var evse = (await _data.LocationAsync(locationId, cancellationToken))?.Evses?.FirstOrDefault(e => e.Uid == evseUid);
            return evse == null ? UnknownLocation() : Ocpi(evse);
        }

        [HttpGet("locations/{locationId}/{evseUid}/{connectorId}")]
        public async Task<IActionResult> Connector(string locationId, string evseUid, string connectorId, CancellationToken cancellationToken)
        {
            var connector = (await _data.LocationAsync(locationId, cancellationToken))?.Evses?.FirstOrDefault(e => e.Uid == evseUid)
                ?.Connectors.FirstOrDefault(c => c.Id == connectorId);
            return connector == null ? UnknownLocation() : Ocpi(connector);
        }

        [HttpGet("tariffs")]
        public async Task<IActionResult> Tariffs([FromQuery(Name = "date_from")] string? dateFrom, [FromQuery(Name = "date_to")] string? dateTo,
            [FromQuery] int? offset, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            if (!TryDates(dateFrom, dateTo, out var from, out var to)) return InvalidDates();
            return Paged(await _data.TariffsAsync(from, to, cancellationToken), offset, limit);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> Sessions([FromQuery(Name = "date_from")] string? dateFrom, [FromQuery(Name = "date_to")] string? dateTo,
            [FromQuery] int? offset, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            if (!TryDates(dateFrom, dateTo, out var from, out var to)) return InvalidDates();
            var query = _data.SessionsOf(Party, from, to);
            var page = OcpiPagination.Apply(Request, Response, await query.CountAsync(cancellationToken), offset, limit);
            var rows = await query.Skip(page.Offset).Take(page.Limit).ToListAsync(cancellationToken);
            return Ocpi(await _data.ToSessionsAsync(rows, cancellationToken));
        }

        // Charging preferences are not supported by EVEON chargers.
        [HttpPut("sessions/{sessionId}/charging_preferences")]
        public IActionResult ChargingPreferences(string sessionId) => Ocpi("NOT_POSSIBLE");

        [HttpGet("cdrs")]
        public async Task<IActionResult> Cdrs([FromQuery(Name = "date_from")] string? dateFrom, [FromQuery(Name = "date_to")] string? dateTo,
            [FromQuery] int? offset, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            if (!TryDates(dateFrom, dateTo, out var from, out var to)) return InvalidDates();
            var query = _data.SessionsOf(Party, from, to).Where(s => s.Status == Models.OcpiSessionStatus.Completed && s.CdrQueuedAt != null);
            var page = OcpiPagination.Apply(Request, Response, await query.CountAsync(cancellationToken), offset, limit);
            var rows = await query.Skip(page.Offset).Take(page.Limit).ToListAsync(cancellationToken);
            return Ocpi(await _data.ToCdrsAsync(rows, cancellationToken));
        }

        private IActionResult UnknownLocation() => OcpiError(StatusCodes.Status404NotFound, OcpiStatusCodes.UnknownLocation, "Unknown location.");
        private IActionResult InvalidDates() => OcpiError(StatusCodes.Status400BadRequest, OcpiStatusCodes.InvalidParameters, "Invalid date_from or date_to.");
    }

    // CPO Receiver interface: Tokens pushed by eMSPs.
    [Route("ocpi/cpo/" + OcpiOptions.VersionNumber + "/tokens")]
    [OcpiAuthorize]
    public class OcpiTokensController : OcpiControllerBase
    {
        private readonly OcpiTokenStore _tokens;

        public OcpiTokensController(OcpiTokenStore tokens)
        {
            _tokens = tokens;
        }

        [HttpGet("{countryCode}/{partyId}/{tokenUid}")]
        public async Task<IActionResult> Get(string countryCode, string partyId, string tokenUid, [FromQuery] string? type, CancellationToken cancellationToken)
        {
            if (!OcpiTokenStore.Owns(Party, countryCode, partyId)) return UnknownToken();
            var token = await _tokens.FindAsync(Party, countryCode.ToUpperInvariant(), partyId.ToUpperInvariant(), tokenUid, type ?? "RFID", cancellationToken);
            return token == null ? UnknownToken() : Ocpi(OcpiTokenStore.ToDto(token));
        }

        [HttpPut("{countryCode}/{partyId}/{tokenUid}")]
        public Task<IActionResult> Put(string countryCode, string partyId, string tokenUid, [FromQuery] string? type, [FromBody] TokenDto? body, CancellationToken cancellationToken) =>
            Save(countryCode, partyId, tokenUid, type ?? body?.Type ?? "RFID", body, partial: false, cancellationToken);

        [HttpPatch("{countryCode}/{partyId}/{tokenUid}")]
        public Task<IActionResult> Patch(string countryCode, string partyId, string tokenUid, [FromQuery] string? type, [FromBody] TokenDto? body, CancellationToken cancellationToken) =>
            Save(countryCode, partyId, tokenUid, type ?? "RFID", body, partial: true, cancellationToken);

        private async Task<IActionResult> Save(string countryCode, string partyId, string tokenUid, string type, TokenDto? body, bool partial, CancellationToken cancellationToken)
        {
            if (body == null) return OcpiError(StatusCodes.Status400BadRequest, OcpiStatusCodes.InvalidParameters, "Token object expected.");
            try
            {
                await _tokens.UpsertAsync(Party, countryCode, partyId, tokenUid, type, body, partial, cancellationToken);
                return Ocpi<object?>(null);
            }
            catch (OcpiValidationException ex)
            {
                return OcpiError(StatusCodes.Status400BadRequest, OcpiStatusCodes.InvalidParameters, ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return UnknownToken();
            }
        }

        private IActionResult UnknownToken() => OcpiError(StatusCodes.Status404NotFound, OcpiStatusCodes.UnknownToken, "Unknown token.");
    }

    // CPO Receiver interface: Commands.
    [Route("ocpi/cpo/" + OcpiOptions.VersionNumber + "/commands")]
    [OcpiAuthorize]
    public class OcpiCommandsController : OcpiControllerBase
    {
        private readonly OcpiCommandService _commands;

        public OcpiCommandsController(OcpiCommandService commands)
        {
            _commands = commands;
        }

        [HttpPost("{command}")]
        public async Task<IActionResult> Post(string command, [FromBody] CommandRequestDto? body, CancellationToken cancellationToken)
        {
            command = command.ToUpperInvariant();
            if (!OcpiCommandService.Commands.Contains(command))
                return Ocpi(new CommandResponseDto { Result = "NOT_SUPPORTED", Timeout = OcpiCommandService.ResultTimeoutSeconds });
            if (body == null) return OcpiError(StatusCodes.Status400BadRequest, OcpiStatusCodes.InvalidParameters, "Command object expected.");
            try
            {
                return Ocpi(await _commands.HandleAsync(Party, command, body, cancellationToken));
            }
            catch (OcpiValidationException ex)
            {
                return OcpiError(StatusCodes.Status400BadRequest, OcpiStatusCodes.InvalidParameters, ex.Message);
            }
        }
    }
}
