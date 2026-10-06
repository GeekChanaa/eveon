using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>
    /// Smart charging commands for 2.0.1 and 1.6 chargers. Every action waits for the charger's answer
    /// (see <see cref="OcppCommandResult"/>); invalid input answers 400, an unknown charger or profile 404.
    /// The raw 2.0.1 actions (SetChargingProfile, ClearChargingProfile, GetChargingProfiles) serve the generic OCPP request tool.
    /// </summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class SmartChargingController : Controller
    {
        private readonly ISmartChargingService _smartChargingService;

        public SmartChargingController(ISmartChargingService service)
        {
            _smartChargingService = service;
        }

        private int? UserId => (HttpContext.Items[typeof(AccessSnapshot)] as AccessSnapshot)?.User.ID;

        [HttpPost("SendChargingProfile/{chargePointID}")]
        public Task<IActionResult> SendChargingProfile(string chargePointID, ChargingProfileInputDto request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetChargingProfile", () => _smartChargingService.SendChargingProfileAsync(
                chargePointID, request, ChargingProfileSourceEnum.Csms, UserId, null, cancellationToken));

        [HttpPost("SetChargingProfile/{chargePointID}")]
        public Task<IActionResult> SetChargingProfile(string chargePointID, SetChargingProfileRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetChargingProfile", () => _smartChargingService.SetChargingProfileAsync(chargePointID, request, UserId, cancellationToken));

        [HttpPost("ClearStoredChargingProfile/{chargePointID}")]
        public Task<IActionResult> ClearStoredChargingProfile(string chargePointID, ClearStoredChargingProfileDto request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ClearChargingProfile", () => _smartChargingService.ClearStoredChargingProfileAsync(chargePointID, request.ChargingProfileID, cancellationToken));

        [HttpPost("ClearChargingProfile/{chargePointID}")]
        public Task<IActionResult> ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ClearChargingProfile", () => _smartChargingService.ClearChargingProfileAsync(chargePointID, request, cancellationToken));

        [HttpPost("GetChargingProfiles/{chargePointID}")]
        public Task<IActionResult> GetChargingProfiles(string chargePointID, GetChargingProfilesRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetChargingProfiles", () => _smartChargingService.GetChargingProfilesAsync(chargePointID, request, cancellationToken));

        [HttpPost("GetCompositeSchedule/{chargePointID}")]
        public Task<IActionResult> GetCompositeSchedule(string chargePointID, CompositeScheduleRequestDto request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetCompositeSchedule", () => _smartChargingService.GetCompositeScheduleAsync(chargePointID, request, cancellationToken));
    }
}
