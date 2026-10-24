using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.OCPP.Controllers;
using VoltaXApi.Services.PartnerPortal;

namespace VoltaXApi.Controllers.PartnerPortal
{
    /// <summary>
    /// Remote actions of the partner portal on the partner's own chargers. Other partners' ids answer 404 and nothing
    /// is sent. The answer is the charger's: 200 { message, status, response } (status Accepted, Rejected, Scheduled,
    /// Unlocked...), 409 when the charger is not connected or the session is no longer running, 504 when it did not
    /// answer in time. Every attempt is audited with the signed-in user.
    /// </summary>
    [ApiController]
    [Route("api/partner-portal")]
    [PartnerPortal]
    public sealed class PartnerRemoteCommandsController : ControllerBase
    {
        private readonly PartnerRemoteCommandService _commands;

        public PartnerRemoteCommandsController(PartnerRemoteCommandService commands)
        {
            _commands = commands;
        }

        private int PartnerId => this.PartnerId();

        /// <summary>Restarts the charger: Soft once its sessions are over, Hard right away.</summary>
        [HttpPost("charge-points/{id:int}/reset")]
        public Task<IActionResult> Reset(int id, [FromBody] PartnerResetRequest request, CancellationToken ct) =>
            Run("Reset", () => _commands.ResetAsync(PartnerId, id, request.Type!.Value, ct));

        /// <summary>Asks the charger to report the status of every connector again.</summary>
        [HttpPost("charge-points/{id:int}/refresh-status")]
        public Task<IActionResult> RefreshStatus(int id, CancellationToken ct) =>
            Run("TriggerMessage", () => _commands.RefreshStatusAsync(PartnerId, id, ct));

        /// <summary>Releases the cable locked in a connector.</summary>
        [HttpPost("connectors/{id:int}/unlock")]
        public Task<IActionResult> Unlock(int id, CancellationToken ct) =>
            Run("UnlockConnector", () => _commands.UnlockAsync(PartnerId, id, ct));

        /// <summary>Stops a running session; the driver pays what was charged until the charger stops.</summary>
        [HttpPost("sessions/{id:int}/stop")]
        public Task<IActionResult> StopSession(int id, CancellationToken ct) =>
            Run("RequestStopTransaction", () => _commands.StopSessionAsync(PartnerId, id, ct));

        private static async Task<IActionResult> Run<T>(string action, Func<Task<PartnerCommandOutcome<T>?>> send) where T : class
        {
            try
            {
                var outcome = await send();
                if (outcome == null)
                    return new NotFoundResult();
                if (outcome.Refusal != null)
                    return new ConflictObjectResult(new { message = $"{action}: the session is no longer running.", error = outcome.Refusal, status = outcome.Refusal });
                return OcppCommandResult.Ok(action, outcome.Answer);
            }
            catch (Exception ex) when (OcppCommandResult.ToError(ex) is { } error)
            {
                return error;
            }
        }
    }
}
