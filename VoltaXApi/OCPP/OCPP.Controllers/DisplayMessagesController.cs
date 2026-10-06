using Microsoft.AspNetCore.Mvc;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>
    /// Display message commands that keep DisplayMessageSnapshots current (read them with api/ChargerDisplayMessage).
    /// Every action waits for the charger's answer (see <see cref="OcppCommandResult"/>).
    /// </summary>
    [Route("ocpp/[controller]")]
    [ApiController]
    public class DisplayMessagesController : Controller
    {
        private readonly IDisplayMessageService _displayMessages;

        public DisplayMessagesController(IDisplayMessageService displayMessages) => _displayMessages = displayMessages;

        [HttpPost("Refresh/{chargePointID}")]
        public Task<IActionResult> Refresh(string chargePointID, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("GetDisplayMessages", () => _displayMessages.RefreshAsync(chargePointID, cancellationToken));

        [HttpPost("Set/{chargePointID}")]
        public Task<IActionResult> Set(string chargePointID, SetDisplayMessageRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("SetDisplayMessage", () => _displayMessages.SetAsync(chargePointID, request, cancellationToken));

        [HttpPost("Clear/{chargePointID}")]
        public Task<IActionResult> Clear(string chargePointID, ClearDisplayMessageRequest request, CancellationToken cancellationToken) =>
            OcppCommandResult.Run("ClearDisplayMessage", () => _displayMessages.ClearAsync(chargePointID, request, cancellationToken));
    }
}
