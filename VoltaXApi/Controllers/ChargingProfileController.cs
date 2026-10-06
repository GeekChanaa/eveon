using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Services;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.Controllers
{
    /// <summary>Stored charging profiles (read only; commands go through ocpp/SmartCharging).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChargingProfileController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;
        private readonly ISmartChargingService _smartCharging;
        private readonly IOcppCommandSender _commandSender;

        public ChargingProfileController(VoltaXApiDbContext db, ISmartChargingService smartCharging, IOcppCommandSender commandSender)
        {
            _db = db;
            _smartCharging = smartCharging;
            _commandSender = commandSender;
        }

        /// <summary>Charge points with their EVSE ids and the OCPP version they are connected with (null = offline).</summary>
        [HttpGet("GetChargePointsForSmartCharging")]
        public async Task<IActionResult> GetChargePointsForSmartCharging(CancellationToken cancellationToken)
        {
            var points = await _db.ChargePoints.AsNoTracking()
                .OrderBy(c => c.ChargePointId)
                .Select(c => new
                {
                    c.ID,
                    c.ChargePointId,
                    c.ChargingStationID,
                    ChargingStationName = c.ChargingStation!.Name,
                    EvseIds = c.Connectors!.Where(k => !k.IsDeleted).Select(k => k.EvseID).Distinct().ToList()
                })
                .ToListAsync(cancellationToken);
            return Ok(points.Select(p => new
            {
                p.ID,
                p.ChargePointId,
                p.ChargingStationID,
                p.ChargingStationName,
                EvseIds = p.EvseIds.OrderBy(e => e).ToList(),
                Protocol = _commandSender.GetProtocolVersion(p.ChargePointId)
            }));
        }

        [HttpGet("GetChargePointChargingProfiles")]
        public async Task<ActionResult<List<ChargingProfileDto>>> GetChargePointChargingProfiles([FromQuery] int chargePointID, [FromQuery] bool includeCleared,
            CancellationToken cancellationToken) =>
            await _smartCharging.GetStoredProfilesAsync(chargePointID, includeCleared, cancellationToken);

        [HttpGet("GetChargePointEvChargingNeeds")]
        public async Task<IActionResult> GetChargePointEvChargingNeeds([FromQuery] int chargePointID, CancellationToken cancellationToken) =>
            Ok(await _db.EvChargingNeeds.AsNoTracking()
                .Where(n => n.ChargePointID == chargePointID)
                .OrderByDescending(n => n.ReceivedAt)
                .Take(20)
                .ToListAsync(cancellationToken));
    }
}
