using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VoltaXApi.Data;

namespace VoltaXApi.Controllers;

// Explicit projections: never serialize charger credentials or user entities.
[ApiController]
[Route("api/mobile")]
public sealed class MobileController(VoltaXApiDbContext db) : ControllerBase
{
    public sealed record NotificationPreference(bool Active, bool Email, bool Urgent);

    [HttpGet("notification-preferences")]
    public async Task<IActionResult> NotificationPreferences()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var types = await db.NotificationTypes.AsNoTracking().Where(t => !t.IsDeleted && t.ForCustomers)
            .Select(t => new { t.ID, t.Name, t.Description }).ToListAsync();
        var preferences = await db.NotificationSettings.AsNoTracking()
            .Where(s => !s.IsDeleted && s.UserID == userId)
            .Select(s => new { s.NotificationTypeID, s.Active, s.Email, s.Urgent }).ToListAsync();
        return Ok(new { types, preferences });
    }

    [HttpPut("notification-preferences/{typeId:int}")]
    public async Task<IActionResult> SaveNotificationPreference(int typeId, NotificationPreference value)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (!await db.NotificationTypes.AnyAsync(t => t.ID == typeId && !t.IsDeleted && t.ForCustomers)) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var preference = await db.NotificationSettings.FirstOrDefaultAsync(s => s.UserID == userId && s.NotificationTypeID == typeId && !s.IsDeleted);
        if (preference == null) {
            preference = new VoltaXApi.Models.NotificationSetting { UserID = userId, NotificationTypeID = typeId };
            db.NotificationSettings.Add(preference);
        }
        preference.Active = value.Active; preference.Email = value.Email; preference.Urgent = value.Urgent;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpGet("stations/{id:int}")]
    public async Task<IActionResult> Station(int id)
    {
        var station = await db.ChargingStations.AsNoTracking().Where(s => s.ID == id && !s.IsDeleted)
            .Select(s => new { s.ID, s.Name, s.Address, s.City, s.Country, s.Latitude, s.Longitude,
                s.Network, s.Status, s.WifiAmenity, s.ParkingAmenity, s.RestaurantsAmenity,
                s.WashroomAmenity, s.SittingAreaAmenity, s.UpdatedAt }).SingleOrDefaultAsync();
        if (station == null) return NotFound();
        var points = await db.ChargePoints.AsNoTracking().Where(p => p.ChargingStationID == id && !p.IsDeleted && p.ShowOnMap == true)
            .Select(p => new { p.ID, p.ChargePointId, p.Status, p.HasChargeCable, QrValue = p.QrValue ?? "" }).ToListAsync();
        var ids = points.Select(p => p.ID).ToArray();
        var connectors = await db.Connectors.AsNoTracking().Where(c => ids.Contains(c.ChargePointID ?? 0) && !c.IsDeleted)
            .Select(c => new { c.ID, c.ChargePointID, c.ConnectorID, c.EvseID, c.ConnectorType,
                PowerKw = c.Power, c.PricePerKWh, c.PricePerMinute, c.PricePerIdleMinute, c.FlatFee }).ToListAsync();
        var connectorIds = connectors.Select(c => c.ID).ToArray();
        var statuses = await db.ConnectorStatuses.AsNoTracking()
            .Where(s => s.ConnectorID != null && connectorIds.Contains(s.ConnectorID.Value) && !s.IsDeleted)
            .OrderByDescending(s => s.LastStatusTime).ThenByDescending(s => s.ID)
            .Select(s => new { s.ConnectorID, s.LastStatus }).ToListAsync();
        var latestStatus = statuses.GroupBy(s => s.ConnectorID!.Value)
            .ToDictionary(g => g.Key, g => g.First().LastStatus.ToString());
        var photos = await db.ChargingStationImages.AsNoTracking()
            .Where(p => p.ChargingStationID == id && !p.IsDeleted && p.Image != null && !p.Image.IsDeleted && p.Image.IsActive)
            .OrderBy(p => p.ID).Select(p => new { p.ID, p.Image!.Url, p.Image.AltText }).ToListAsync();
        return Ok(new { station, chargePoints = points,
            connectors = connectors.Select(c => new { c.ID, c.ChargePointID, c.ConnectorID, c.EvseID,
                c.ConnectorType, c.PowerKw, c.PricePerKWh, c.PricePerMinute, c.PricePerIdleMinute, c.FlatFee,
                Status = latestStatus.GetValueOrDefault(c.ID) }), photos, currency = "MAD" });
    }

    [HttpGet("chargepoints/{id:int}/reviews")]
    public async Task<IActionResult> Reviews(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 50 || page > 100000) return BadRequest();
        if (!await db.ChargePoints.AnyAsync(p => p.ID == id && !p.IsDeleted && p.ShowOnMap == true)) return NotFound();
        var query = db.Ratings.AsNoTracking().Where(r => r.Entity == "ChargePoint" && r.EntityID == id && !r.IsDeleted);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.ID)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new { r.ID, Rating = r.Score, r.Comment, UserName = r.User.FirstName, r.CreatedAt }).ToListAsync();
        return Ok(new { items, page, pageSize, total, hasMore = page * pageSize < total });
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> Wallet()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var cards = await db.Cards.AsNoTracking().Where(c => c.UserID == userId && !c.IsDeleted)
            .Select(c => new { c.ID, c.CardNumber, c.Balance, c.Status, c.ExpirationDate }).ToListAsync();
        var methods = await db.DebitCards.AsNoTracking().Where(c => c.UserID == userId && !c.IsDeleted)
            .Select(c => new { c.ID, brand = c.Brand, last4 = c.Last4, c.ExpiryMonth, c.ExpiryYear }).ToListAsync();
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var completedTopUps = await db.Orders.Where(o => !o.IsDeleted && o.Card != null && o.Card.UserID == userId && o.Status == VoltaXApi.Models.RechargeOrderStatus.Completed && o.RechargeDate >= start && o.RechargeDate < start.AddMonths(1)).SumAsync(o => o.Amount);
        return Ok(new { currency = "MAD", balance = cards.Sum(c => c.Balance), paymentMethods = methods, monthlyTopUps = completedTopUps,
            cards = cards.Select(c => new { c.ID, last4 = c.CardNumber?.Length >= 4 ? c.CardNumber[^4..] : null, c.Balance, c.Status, c.ExpirationDate }),
            paymentMethodsAvailable = false, topUpAvailable = false });
    }

    [HttpGet("wallet/orders")]
    public async Task<IActionResult> Orders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (page < 1 || pageSize < 1 || pageSize > 50 || page > 100000) return BadRequest();
        var query = db.Orders.AsNoTracking().Where(o => !o.IsDeleted && o.Card != null && !o.Card.IsDeleted && o.Card.UserID == userId);
        if (!string.IsNullOrWhiteSpace(status)) {
            if (!Enum.TryParse<VoltaXApi.Models.RechargeOrderStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed)) return BadRequest();
            query = query.Where(o => o.Status == parsed);
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.RechargeDate).ThenByDescending(o => o.ID)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(o => new { o.ID, o.Amount, o.Status, o.RechargeDate }).ToListAsync();
        return Ok(new { items, page, pageSize, total, hasMore = page * pageSize < total, currency = "MAD" });
    }
}
