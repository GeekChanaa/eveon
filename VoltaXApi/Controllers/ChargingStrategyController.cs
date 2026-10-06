using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.Services.Audit;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.Controllers
{
    /// <summary>Charging strategies: reusable profile templates applied to charge points or whole stations.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChargingStrategyController : ControllerBase
    {
        private readonly IChargingStrategyService _strategies;
        private readonly IAuditLogger _audit;

        public ChargingStrategyController(IChargingStrategyService strategies, IAuditLogger audit)
        {
            _strategies = strategies;
            _audit = audit;
        }

        private int? UserId => (HttpContext.Items[typeof(AccessSnapshot)] as AccessSnapshot)?.User.ID;

        [HttpGet("GetChargingStrategies")]
        public async Task<ActionResult<List<ChargingStrategyDto>>> GetChargingStrategies(CancellationToken cancellationToken) =>
            await _strategies.GetAllAsync(cancellationToken);

        [HttpGet("GetChargingStrategy")]
        public async Task<ActionResult<ChargingStrategyDto>> GetChargingStrategy([FromQuery] int id, CancellationToken cancellationToken) =>
            await _strategies.GetAsync(id, cancellationToken);

        [HttpPost("CreateChargingStrategy")]
        public async Task<ActionResult<ChargingStrategyDto>> CreateChargingStrategy(ChargingStrategyInputDto request, CancellationToken cancellationToken)
        {
            var created = await _strategies.CreateAsync(request, UserId, cancellationToken);
            await _audit.LogAsync("SmartCharging.CreateChargingStrategy", "ChargingStrategy", created.ID.ToString(), request);
            return created;
        }

        [HttpPut("UpdateChargingStrategy")]
        public async Task<ActionResult<ChargingStrategyDto>> UpdateChargingStrategy([FromQuery] int id, ChargingStrategyInputDto request, CancellationToken cancellationToken)
        {
            var updated = await _strategies.UpdateAsync(id, request, cancellationToken);
            await _audit.LogAsync("SmartCharging.UpdateChargingStrategy", "ChargingStrategy", id.ToString(), request);
            return updated;
        }

        [HttpDelete("DeleteChargingStrategy")]
        public async Task<IActionResult> DeleteChargingStrategy([FromQuery] int id, CancellationToken cancellationToken)
        {
            await _strategies.DeleteAsync(id, cancellationToken);
            await _audit.LogAsync("SmartCharging.DeleteChargingStrategy", "ChargingStrategy", id.ToString());
            return NoContent();
        }

        /// <summary>Sends the strategy's profile to the chargers and returns each charger's real answer.</summary>
        [HttpPost("ApplyChargingStrategy")]
        public async Task<ActionResult<List<StrategyApplyResult>>> ApplyChargingStrategy([FromQuery] int id, ApplyChargingStrategyDto request,
            CancellationToken cancellationToken)
        {
            var results = await _strategies.ApplyAsync(id, request, UserId, cancellationToken);
            await _audit.LogAsync("SmartCharging.ApplyChargingStrategy", "ChargingStrategy", id.ToString(),
                new { request.ChargePointIDs, request.ChargingStationIDs, Accepted = results.Count(r => r.Status == "Accepted"), Total = results.Count });
            return results;
        }
    }
}
