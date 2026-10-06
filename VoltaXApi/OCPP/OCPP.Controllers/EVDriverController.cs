using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Messages;
using Microsoft.AspNetCore.Authorization;
using VoltaXApi.OCPP.Exceptions;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>EV driver commands. Every action waits for the charger's answer (see <see cref="OcppCommandResult"/>).</summary>
    [ApiController]
    [Route("ocpp/[controller]")]
    public class EVDriverController : Controller
    {
        private readonly IEVDriverService _EVDriverService;
        private readonly ILogger<EVDriverController> _logger;

        public EVDriverController(
            IEVDriverService EVDriverService,
            ILogger<EVDriverController> logger
        ){
            _EVDriverService = EVDriverService;
            _logger = logger;
        }

        [HttpPost("RequestStartTransaction/{chargePointID}")]
        public Task<IActionResult> RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("RequestStartTransaction", () => _EVDriverService.RequestStartTransaction(chargePointID, request, cancellationToken));

        [Authorize]
        [HttpPost("RequestStartTransactionMobile/{chargePointID}")]
        public async Task<IActionResult> RequestStartTransactionMobile(string chargePointID, RequestStartTransactionRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Invalid or missing user authentication." });

            _logger.LogInformation("RequestStartTransactionMobile for {ChargePointId} by user {UserId}", chargePointID, userId);
            try
            {
                return await OcppCommandResult.Run("RequestStartTransaction",
                    () => _EVDriverService.RequestStartTransactionMobile(chargePointID, request, userId, cancellationToken));
            }
            catch (ChargingOwnershipException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
        }

        [HttpPost("RequestStopTransaction/{chargePointID}")]
        public Task<IActionResult> RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("RequestStopTransaction", () => _EVDriverService.RequestStopTransaction(chargePointID, request, cancellationToken));

        [Authorize]
        [HttpPost("RequestStopTransactionMobile/{chargePointID}")]
        public Task<IActionResult> RequestStopTransactionMobile(string chargePointID, RequestStopTransactionRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Task.FromResult<IActionResult>(Unauthorized(new { Message = "Invalid or missing user authentication." }));

            return OcppCommandResult.Run("RequestStopTransaction",
                () => _EVDriverService.RequestStopTransactionMobile(chargePointID, request, userId, cancellationToken));
        }

        [HttpPost("CancelReservation/{chargePointID}")]
        public Task<IActionResult> CancelReservation(string chargePointID, CancelReservationRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("CancelReservation", () => _EVDriverService.CancelReservation(chargePointID, request, cancellationToken));

        [HttpPost("ReserveNow/{chargePointID}")]
        public Task<IActionResult> ReserveNow(string chargePointID, ReserveNowRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ReserveNow", () => _EVDriverService.ReserveNow(chargePointID, request, cancellationToken));

        [HttpPost("UnlockConnector/{chargePointID}")]
        public Task<IActionResult> UnlockConnector(string chargePointID, UnlockConnectorRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("UnlockConnector", () => _EVDriverService.UnlockConnector(chargePointID, request, cancellationToken));

        [HttpPost("ClearCache/{chargePointID}")]
        public Task<IActionResult> ClearCache(string chargePointID, ClearCacheRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ClearCache", () => _EVDriverService.ClearCache(chargePointID, request, cancellationToken));

        [HttpPost("SendLocalList/{chargePointID}")]
        public Task<IActionResult> SendLocalList(string chargePointID, SendLocalListRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SendLocalList", () => _EVDriverService.SendLocalList(chargePointID, request, cancellationToken));

        [HttpPost("GetLocalListVersion/{chargePointID}")]
        public Task<IActionResult> GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetLocalListVersion", () => _EVDriverService.GetLocalListVersion(chargePointID, request, cancellationToken));

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out userId);
        }
    }
}
