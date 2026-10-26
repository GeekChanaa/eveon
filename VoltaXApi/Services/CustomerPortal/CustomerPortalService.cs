using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services.CustomerPortal
{
    /// <summary>
    /// Views of one customer's account for the customer portal, plus saving their optional profile details. Every query is filtered on the user id
    /// passed in, which callers take from the authenticated request, never from input.
    /// </summary>
    public sealed class CustomerPortalService
    {
        public const string Currency = "MAD";
        private const int MaxPageSize = 100;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly GlobalConfigurations _pricing;

        public CustomerPortalService(VoltaXApiDbContext db, IBusinessClock clock, GlobalConfigurations pricing)
        {
            _db = db;
            _clock = clock;
            _pricing = pricing;
        }

        public async Task<MyProfileDto?> ProfileAsync(int userId, CancellationToken ct = default) =>
            await _db.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => new MyProfileDto(u.FirstName, u.LastName, u.Email, u.Phone, u.Birthday,
                    u.Image != null ? u.Image.Url : u.ExternalPictureUrl, u.IsEmailVerified, u.IsPhoneNumberVerified, u.CreatedAt,
                    u.Gender, u.City, u.ElectricVehicleModelID,
                    u.ElectricVehicleModel != null ? u.ElectricVehicleModel.Make + " " + u.ElectricVehicleModel.Model : null))
                .FirstOrDefaultAsync(ct);

        /// <summary>
        /// Saves the caller's optional details. Returns an error message for input the model binder cannot catch
        /// (a date of birth in the future, an unknown vehicle), or null once saved.
        /// </summary>
        public async Task<string?> UpdateProfileAsync(int userId, MyProfileUpdateDto update, CancellationToken ct = default)
        {
            var user = await _db.Users.SingleAsync(u => u.ID == userId, ct);
            var birthday = update.Birthday?.Date;
            if (birthday != null && (birthday > _clock.Today || birthday.Value.Year < 1900))
                return "Enter a valid date of birth.";
            if (update.ElectricVehicleModelId is { } modelId
                && !await _db.ElectricVehicleModels.AnyAsync(m => m.ID == modelId && !m.IsDeleted, ct))
                return "Unknown vehicle model.";

            user.Birthday = birthday;
            user.Gender = string.IsNullOrWhiteSpace(update.Gender) ? null : update.Gender;
            user.City = string.IsNullOrWhiteSpace(update.City) ? null : update.City.Trim();
            user.ElectricVehicleModelID = update.ElectricVehicleModelId;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return null;
        }

        public async Task<IReadOnlyList<MyCardDto>> CardsAsync(int userId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var cards = await _db.Cards.AsNoTracking().Where(c => c.UserID == userId).OrderBy(c => c.ID)
                .Select(c => new { c.ID, c.CardNumber, c.Balance, c.ExpirationDate, c.Status, c.Blocked })
                .ToListAsync(ct);
            return cards.Select(c => new MyCardDto(c.ID, Mask(c.CardNumber), Last4(c.CardNumber), c.Balance, c.ExpirationDate, c.Status,
                c.Blocked == true, c.ExpirationDate < now)).ToList();
        }

        public async Task<MyOverviewDto?> OverviewAsync(int userId, CancellationToken ct = default)
        {
            var firstName = await _db.Users.AsNoTracking().Where(u => u.ID == userId).Select(u => u.FirstName).FirstOrDefaultAsync(ct);
            if (firstName == null)
                return null;
            var cards = await _db.Cards.AsNoTracking().Where(c => c.UserID == userId).Select(c => c.Balance).ToListAsync(ct);
            var sessions = (await Rows(userId).ToListAsync(ct)).Select(ToDto).ToList();
            var monthStart = _clock.StartOfMonthUtc(_clock.Today.Year, _clock.Today.Month);
            var month = sessions.Where(s => s.StartDate >= monthStart).ToList();
            var active = await ActiveSessionAsync(userId, ct);
            return new MyOverviewDto(firstName, cards.Sum(), cards.Count, active,
                month.Count, month.Sum(s => s.ChargedKwhs), month.Sum(s => s.Total),
                sessions.Count, sessions.Sum(s => s.ChargedKwhs), sessions.Sum(s => s.Total), Currency);
        }

        public async Task<MyActiveSessionDto?> ActiveSessionAsync(int userId, CancellationToken ct = default)
        {
            var session = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                .Include(s => s.Connector).ThenInclude(c => c!.ChargePoint).ThenInclude(p => p!.ChargingStation)
                .Where(s => s.UserID == userId && !s.IsDeleted && s.EndDate == null)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(ct);
            if (session == null)
                return null;
            var minutes = session.ChargedMinutes ?? 0;
            var kwh = session.ChargedKwhs ?? 0;
            var cost = CostCalculator.Instance.RunningCost(Tariff.Of(session, session.Connector, _pricing.Vat), kwh, minutes);
            var transactionUid = await _db.Transactions.AsNoTracking()
                .Where(t => t.ChargingSessionID == session.ID && t.StopTime == null && t.Status == TransactionStatusEnum.Current)
                .OrderByDescending(t => t.ID).Select(t => t.Uid).FirstOrDefaultAsync(ct);
            var balance = await _db.Cards.AsNoTracking().Where(c => c.ID == session.CardID).Select(c => (decimal?)c.Balance).FirstOrDefaultAsync(ct);
            return new MyActiveSessionDto(session.ID, session.Connector?.ChargePoint?.ChargingStation?.Name, session.Connector?.ChargePoint?.ChargePointId,
                ConnectorLabel(session.Connector?.EvseID, session.Connector?.ConnectorID), session.StartDate, minutes, kwh, cost, transactionUid, balance);
        }

        public async Task<PagedResult<MySessionDto>> SessionsAsync(int userId, MySessionFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
            var query = Filtered(Rows(userId), filter);
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(r => r.StartDate).ThenByDescending(r => r.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<MySessionDto>(rows.Select(ToDto).ToList(), page, pageSize, total);
        }

        /// <summary>Every session matching <paramref name="filter"/> (no paging), oldest first, for exports.</summary>
        public async Task<IReadOnlyList<MySessionDto>> ExportAsync(int userId, MySessionFilter filter, CancellationToken ct = default) =>
            (await Filtered(Rows(userId), filter).OrderBy(r => r.StartDate).ThenBy(r => r.Id).Take(10_000).ToListAsync(ct)).Select(ToDto).ToList();

        public async Task<MySessionDetailDto?> SessionAsync(int userId, int sessionId, CancellationToken ct = default)
        {
            var row = await Rows(userId).FirstOrDefaultAsync(r => r.Id == sessionId, ct);
            if (row == null)
                return null;
            var dto = ToDto(row);
            var vatRate = row.VatRate ?? _pricing.Vat;
            var (excl, vat) = CostCalculator.SplitVat(dto.Total, vatRate);
            return new MySessionDetailDto(dto, row.PricePerKWh, row.PricePerMinute, row.PricePerIdleMinute, row.FlatFee, vatRate, excl, vat,
                row.BalanceBefore, row.BalanceAfter, row.CardNumber == null ? null : Last4(row.CardNumber), Currency);
        }

        public async Task<PagedResult<MyWalletMovementDto>> WalletAsync(int userId, int page, int pageSize, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var query = _db.WalletTransactions.AsNoTracking().Where(w => w.Card!.UserID == userId);
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(w => w.CreatedAt).ThenByDescending(w => w.ID)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(w => new { w.ID, w.CreatedAt, w.Type, w.Amount, w.BalanceAfter, w.Description, w.ChargingSessionID, w.OrderID, w.Card!.CardNumber })
                .ToListAsync(ct);
            return new PagedResult<MyWalletMovementDto>(rows.Select(w => new MyWalletMovementDto(w.ID, w.CreatedAt, w.Type, w.Amount, w.BalanceAfter,
                w.Description, w.ChargingSessionID, w.OrderID, Last4(w.CardNumber))).ToList(), page, pageSize, total);
        }

        public async Task<PagedResult<MyOrderDto>> OrdersAsync(int userId, int page, int pageSize, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var query = _db.Orders.AsNoTracking().Where(o => o.Card!.UserID == userId);
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.ID)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new { o.ID, o.CreatedAt, o.Amount, o.Status, o.Provider, o.PaidAt, o.FailureReason, o.Card!.CardNumber })
                .ToListAsync(ct);
            return new PagedResult<MyOrderDto>(rows.Select(o => new MyOrderDto(o.ID, o.CreatedAt, o.Amount, o.Status, o.Provider, o.PaidAt,
                o.FailureReason, Last4(o.CardNumber), o.Status == RechargeOrderStatus.Completed)).ToList(), page, pageSize, total);
        }

        public Task<bool> OwnsSessionAsync(int userId, int sessionId, CancellationToken ct = default) =>
            _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters().AnyAsync(s => s.ID == sessionId && s.UserID == userId && !s.IsDeleted, ct);

        public Task<bool> OwnsCompletedOrderAsync(int userId, int orderId, CancellationToken ct = default) =>
            _db.Orders.AsNoTracking().AnyAsync(o => o.ID == orderId && o.Card!.UserID == userId && o.Status == RechargeOrderStatus.Completed, ct);

        // -------------------------------------------------------------------------------- internals

        /// <summary>Flat projection of a session; a class with setters so EF can keep filtering and paging after it.</summary>
        private sealed class Row
        {
            public int Id { get; init; }
            public string? StationName { get; init; }
            public string? StationAddress { get; init; }
            public string? ChargePointName { get; init; }
            public int? EvseId { get; init; }
            public int? ConnectorNumber { get; init; }
            public DateTime StartDate { get; init; }
            public DateTime? EndDate { get; init; }
            public double? ChargedMinutes { get; init; }
            public double? IdleMinutes { get; init; }
            public double? ChargedKwhs { get; init; }
            public decimal? PricePerKWh { get; init; }
            public decimal? PricePerMinute { get; init; }
            public decimal? PricePerIdleMinute { get; init; }
            public decimal? FlatFee { get; init; }
            public decimal? VatRate { get; init; }
            public decimal? EnergyAmount { get; init; }
            public decimal? TimeAmount { get; init; }
            public decimal? FlatFeeAmount { get; init; }
            public decimal? IdleAmount { get; init; }
            public decimal? TotalAmount { get; init; }
            public decimal? BalanceBefore { get; init; }
            public decimal? BalanceAfter { get; init; }
            public ReasonEnumType StoppedReason { get; init; }
            public ChargingSessionStatusEnum Status { get; init; }
            public string? CardNumber { get; init; }
        }

        /// <summary>
        /// The user's sessions, including those on connectors or stations deleted since (the soft-delete filters would
        /// turn those joins into inner joins and hide history).
        /// </summary>
        private IQueryable<Row> Rows(int userId) =>
            _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                .Where(s => s.UserID == userId && !s.IsDeleted)
                .Select(s => new Row
                {
                    Id = s.ID,
                    StationName = s.Connector!.ChargePoint!.ChargingStation!.Name,
                    StationAddress = s.Connector!.ChargePoint!.ChargingStation!.Address,
                    ChargePointName = s.Connector!.ChargePoint!.ChargePointId,
                    EvseId = (int?)s.Connector!.EvseID,
                    ConnectorNumber = s.Connector!.ConnectorID,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    ChargedMinutes = s.ChargedMinutes,
                    IdleMinutes = s.IdleMinutes,
                    ChargedKwhs = s.ChargedKwhs,
                    PricePerKWh = s.PricePerKWh,
                    PricePerMinute = s.PricePerMinute,
                    PricePerIdleMinute = s.PricePerIdleMinute,
                    FlatFee = s.FlatFee,
                    VatRate = s.VatRate,
                    EnergyAmount = s.EnergyAmount,
                    TimeAmount = s.TimeAmount,
                    FlatFeeAmount = s.FlatFeeAmount,
                    IdleAmount = s.IdleAmount,
                    TotalAmount = s.TotalAmount,
                    BalanceBefore = s.BalanceBefore,
                    BalanceAfter = s.BalanceAfter,
                    StoppedReason = s.StoppedReason,
                    Status = s.ChargingSessionStatus,
                    CardNumber = s.Card!.CardNumber
                });

        private IQueryable<Row> Filtered(IQueryable<Row> rows, MySessionFilter filter)
        {
            if (filter.From.HasValue)
            {
                var from = _clock.StartOfDayUtc(filter.From.Value.Date);
                rows = rows.Where(r => r.StartDate >= from);
            }
            if (filter.To.HasValue)
            {
                var to = _clock.StartOfDayUtc(filter.To.Value.Date.AddDays(1));
                rows = rows.Where(r => r.StartDate < to);
            }
            if (string.Equals(filter.Status, "active", StringComparison.OrdinalIgnoreCase))
                rows = rows.Where(r => r.EndDate == null);
            else if (string.Equals(filter.Status, "completed", StringComparison.OrdinalIgnoreCase))
                rows = rows.Where(r => r.EndDate != null);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                rows = rows.Where(r => (r.StationName != null && r.StationName.Contains(term)) || (r.ChargePointName != null && r.ChargePointName.Contains(term)));
            }
            return rows;
        }

        private MySessionDto ToDto(Row r)
        {
            var priced = new ChargingSession
            {
                ChargedMinutes = r.ChargedMinutes, IdleMinutes = r.IdleMinutes, PricePerMinute = r.PricePerMinute,
                PricePerIdleMinute = r.PricePerIdleMinute, EnergyAmount = r.EnergyAmount, TimeAmount = r.TimeAmount,
                FlatFeeAmount = r.FlatFeeAmount, IdleAmount = r.IdleAmount, TotalAmount = r.TotalAmount, VatRate = r.VatRate
            };
            var billed = r.TotalAmount.HasValue;
            var idle = priced.IdleChargingPriceWithVAT((int)_pricing.GracePeriod);
            // Sessions billed before amounts were stored were billed on time only.
            var energy = billed ? r.EnergyAmount ?? 0 : 0;
            var time = billed ? r.TimeAmount ?? 0 : priced.ChargingPriceWithVAT;
            var flat = billed ? r.FlatFeeAmount ?? 0 : 0;
            var active = r.EndDate == null;
            return new MySessionDto(r.Id, r.StationName, r.StationAddress, r.ChargePointName, ConnectorLabel(r.EvseId, r.ConnectorNumber),
                r.StartDate, r.EndDate, r.ChargedMinutes ?? 0, r.IdleMinutes ?? 0, r.ChargedKwhs ?? 0,
                energy, time, flat, idle, energy + time + flat + idle, r.StoppedReason, r.Status, active, !active);
        }

        private static string? ConnectorLabel(int? evseId, int? connectorId) =>
            evseId == null ? null : connectorId is null or 1 ? $"EVSE {evseId}" : $"EVSE {evseId} / {connectorId}";

        public static string Last4(string? number) => string.IsNullOrEmpty(number) ? "" : number.Length <= 4 ? number : number[^4..];

        public static string Mask(string? number) => "•••• " + Last4(number);
    }
}
