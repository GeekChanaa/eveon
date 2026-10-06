using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Services.Audit;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.Controllers
{
    public class StationLoadLimitDto
    {
        public bool Enabled { get; set; }

        [Range(0.1, 10_000)]
        public double? MaxCurrentA { get; set; }

        [Range(0.1, 10_000)]
        public double? MaxPowerKW { get; set; }

        [Range(1, 3)]
        public int Phases { get; set; } = 3;

        [Range(100, 480)]
        public double Voltage { get; set; } = 230;

        [Range(0, 80)]
        public double MinPerSessionA { get; set; } = 6;

        public LoadBalancingStrategyEnum Strategy { get; set; } = LoadBalancingStrategyEnum.EqualShare;

        [Range(0, 50)]
        public double SafetyMarginPercent { get; set; }
    }

    /// <summary>Per-station load balancing. Partners manage the stations of their own partner account (see DashboardAccessFilter).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class StationLoadBalancingController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;
        private readonly ILoadBalancingService _loadBalancing;
        private readonly ILoadBalancingTrigger _trigger;
        private readonly IAuditLogger _audit;

        public StationLoadBalancingController(VoltaXApiDbContext db, ILoadBalancingService loadBalancing, ILoadBalancingTrigger trigger, IAuditLogger audit)
        {
            _db = db;
            _loadBalancing = loadBalancing;
            _trigger = trigger;
            _audit = audit;
        }

        private AccessSnapshot? Access => HttpContext.Items[typeof(AccessSnapshot)] as AccessSnapshot;

        /// <summary>Stations the caller may balance (a partner's own stations) with their settings.</summary>
        [HttpGet("GetLoadBalancingStations")]
        public async Task<IActionResult> GetLoadBalancingStations(CancellationToken cancellationToken)
        {
            var access = Access;
            var query = _db.ChargingStations.AsNoTracking();
            if (access == null || !access.Can("ViewChargingStations"))
            {
                var partnerId = access?.User.Role.Name == "Partner" ? access.User.PartnerID : null;
                if (partnerId == null) return Forbid();
                query = query.Where(s => s.PartnerID == partnerId);
            }
            var stations = await query.OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.ID,
                    s.Name,
                    s.City,
                    ChargePointCount = s.ChargePoints!.Count(c => !c.IsDeleted),
                    Limit = _db.StationLoadLimits.Where(l => l.ChargingStationID == s.ID)
                        .Select(l => new { l.Enabled, l.MaxCurrentA, l.MaxPowerKW, l.Strategy, l.LastRebalancedAt })
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
            return Ok(stations);
        }

        [HttpGet("GetStationLoadLimit")]
        public async Task<ActionResult<StationLoadLimit>> GetStationLoadLimit([FromQuery] int chargingStationID, CancellationToken cancellationToken)
        {
            if (!await _db.ChargingStations.AnyAsync(s => s.ID == chargingStationID, cancellationToken)) return NotFound();
            return await _db.StationLoadLimits.AsNoTracking().SingleOrDefaultAsync(l => l.ChargingStationID == chargingStationID, cancellationToken)
                ?? new StationLoadLimit { ChargingStationID = chargingStationID };
        }

        [HttpPut("SaveStationLoadLimit")]
        public async Task<ActionResult<StationLoadLimit>> SaveStationLoadLimit([FromQuery] int chargingStationID, StationLoadLimitDto request,
            CancellationToken cancellationToken)
        {
            if (!await _db.ChargingStations.AnyAsync(s => s.ID == chargingStationID, cancellationToken)) return NotFound();
            if (request.Enabled && request.MaxCurrentA == null && request.MaxPowerKW == null)
                return BadRequest(new { error = "Set a maximum current or a maximum power to enable load balancing." });
            if (request.Phases == 2)
                return BadRequest(new { error = "A station supply has 1 or 3 phases." });

            var limit = await _db.StationLoadLimits.SingleOrDefaultAsync(l => l.ChargingStationID == chargingStationID, cancellationToken);
            if (limit == null)
            {
                limit = new StationLoadLimit { ChargingStationID = chargingStationID };
                _db.StationLoadLimits.Add(limit);
            }
            limit.Enabled = request.Enabled;
            limit.MaxCurrentA = request.MaxCurrentA;
            limit.MaxPowerKW = request.MaxPowerKW;
            limit.Phases = request.Phases;
            limit.Voltage = request.Voltage;
            limit.MinPerSessionA = request.MinPerSessionA;
            limit.Strategy = request.Strategy;
            limit.SafetyMarginPercent = request.SafetyMarginPercent;
            limit.UpdatedByUserID = Access?.User.ID;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync("SmartCharging.SaveStationLoadLimit", "ChargingStation", chargingStationID.ToString(), request);
            _trigger.RequestRebalance(chargingStationID);
            return limit;
        }

        [HttpGet("GetStationAllocations")]
        public async Task<ActionResult<RebalanceSummary>> GetStationAllocations([FromQuery] int chargingStationID, CancellationToken cancellationToken) =>
            await _loadBalancing.GetLiveAllocationsAsync(chargingStationID, cancellationToken);

        [HttpGet("GetStationAllocationHistory")]
        public async Task<ActionResult<List<StationLoadAllocation>>> GetStationAllocationHistory([FromQuery] int chargingStationID, [FromQuery] int take,
            CancellationToken cancellationToken) =>
            await _db.StationLoadAllocations.AsNoTracking()
                .Where(a => a.ChargingStationID == chargingStationID)
                .OrderByDescending(a => a.ID)
                .Take(Math.Clamp(take <= 0 ? 50 : take, 1, 500))
                .ToListAsync(cancellationToken);

        [HttpPost("RebalanceStation")]
        public async Task<ActionResult<RebalanceSummary>> RebalanceStation([FromQuery] int chargingStationID, CancellationToken cancellationToken)
        {
            if (!await _db.ChargingStations.AnyAsync(s => s.ID == chargingStationID, cancellationToken)) return NotFound();
            await _audit.LogAsync("SmartCharging.RebalanceStation", "ChargingStation", chargingStationID.ToString());
            return await _trigger.RebalanceNowAsync(chargingStationID, "manual", cancellationToken);
        }
    }
}
