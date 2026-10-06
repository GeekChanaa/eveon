using Microsoft.AspNetCore.Mvc;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Ocpp16;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>Every action waits for the charger's answer (see <see cref="OcppCommandResult"/>).</summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class ConfigurationController : Controller
    {
        private readonly IConfigurationService _configService;

        private readonly IOcppCommandSender _commandSender;

        public ConfigurationController(IConfigurationService service, IOcppCommandSender commandSender)
        {
            _configService = service;
            _commandSender = commandSender;
        }

        /// <summary>OCPP version of the charger's current connection ("ocpp1.6" / "ocpp2.0.1"), null when it is offline.</summary>
        [HttpGet("ProtocolVersion/{chargePointID}")]
        public IActionResult ProtocolVersion(string chargePointID, [FromServices] ChargePointStatusManagerService statusManager)
        {
            var version = _commandSender.GetProtocolVersion(chargePointID);
            return Ok(new { chargePointId = chargePointID, protocolVersion = version, isOnline = statusManager.ChargePointExists(chargePointID) });
        }

        // OCPP 1.6 only: 400 for chargers speaking another version.
        [HttpPost("ChangeConfiguration/{chargePointID}")]
        public Task<IActionResult> ChangeConfiguration(string chargePointID, ChangeConfigurationDto request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ChangeConfiguration", () => _configService.ChangeConfiguration(chargePointID, request, cancellationToken));

        [HttpPost("GetConfiguration/{chargePointID}")]
        public Task<IActionResult> GetConfiguration(string chargePointID, GetConfigurationDto request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetConfiguration", () => _configService.GetConfiguration(chargePointID, request, cancellationToken));

        /// <summary>The charger uploads its diagnostics file to <c>location</c> (FTP/HTTP URL supplied by the caller).</summary>
        [HttpPost("GetDiagnostics/{chargePointID}")]
        public Task<IActionResult> GetDiagnostics(string chargePointID, GetDiagnostics16Request request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetDiagnostics", () => _configService.GetDiagnostics(chargePointID, request, cancellationToken));

        [HttpPost("SetNetworkProfile/{chargePointID}")]
        public Task<IActionResult> SetNetworkProfile(string chargePointID, SetNetworkProfileRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetNetworkProfile", () => _configService.SetNetworkProfile(chargePointID, request, cancellationToken));

        [HttpPost("ClearDisplayMessage/{chargePointID}")]
        public Task<IActionResult> ClearDisplayMessage(string chargePointID, ClearDisplayMessageRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ClearDisplayMessage", () => _configService.ClearDisplayMessage(chargePointID, request, cancellationToken));

        [HttpPost("GetDisplayMessages/{chargePointID}")]
        public Task<IActionResult> GetDisplayMessages(string chargePointID, GetDisplayMessagesRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetDisplayMessages", () => _configService.GetDisplayMessages(chargePointID, request, cancellationToken));

        [HttpPost("PublishFirmware/{chargePointID}")]
        public Task<IActionResult> PublishFirmware(string chargePointID, PublishFirmwareRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("PublishFirmware", () => _configService.PublishFirmware(chargePointID, request, cancellationToken));

        [HttpPost("SetDisplayMessage/{chargePointID}")]
        public Task<IActionResult> SetDisplayMessage(string chargePointID, SetDisplayMessageRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetDisplayMessage", () => _configService.SetDisplayMessage(chargePointID, request, cancellationToken));

        [HttpPost("UnpublishFirmware/{chargePointID}")]
        public Task<IActionResult> UnpublishFirmware(string chargePointID, UnpublishFirmwareRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("UnpublishFirmware", () => _configService.UnpublishFirmware(chargePointID, request, cancellationToken));

        [HttpPost("UpdateFirmware/{chargePointID}")]
        public Task<IActionResult> UpdateFirmware(string chargePointID, UpdateFirmwareRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("UpdateFirmware", () => _configService.UpdateFirmware(chargePointID, request, cancellationToken));

        [HttpPost("Reset/{chargePointID}")]
        public Task<IActionResult> Reset(string chargePointID, ResetRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("Reset", () => _configService.Reset(chargePointID, request, cancellationToken));

        [HttpPost("ChangeAvailability/{chargePointID}")]
        public Task<IActionResult> ChangeAvailability(string chargePointID, ChangeAvailabilityRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ChangeAvailability", () => _configService.ChangeAvailability(chargePointID, request, cancellationToken));

        [HttpPost("TriggerMessage/{chargePointID}")]
        public Task<IActionResult> TriggerMessage(string chargePointID, TriggerMessageRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("TriggerMessage", () => _configService.TriggerMessage(chargePointID, request, cancellationToken));

        [HttpPost("RefreshConnectors/{chargePointID}")]
        public Task<IActionResult> RefreshConnectors(string chargePointID, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetBaseReport", () => _configService.RefreshConnectors(chargePointID, cancellationToken));
    }
}
