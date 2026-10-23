using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Services.Billing
{
    /// <summary>Settlement settings (section "Settlements").</summary>
    public sealed class SettlementOptions
    {
        /// <summary>Business day of the month from which the monthly job settles the previous month.</summary>
        public int GenerateOnDay { get; set; } = 2;
    }

    public sealed record PartnerCommissionDto(int PartnerId, string Name, decimal CommissionPercent, decimal CommissionPerKwh);

    public sealed record UpdateCommissionRequest(decimal CommissionPercent, decimal CommissionPerKwh);

    public sealed record SettlementDto(int Id, int PartnerId, string PartnerName, int Year, int Month, DateTime PeriodStart, DateTime PeriodEnd,
        int SessionCount, decimal EnergyKwh, decimal GrossAmount, decimal NetExclVat, decimal RevenueVat, decimal CommissionExclVat,
        decimal CommissionVat, decimal NetPayout, decimal CommissionPercent, decimal CommissionPerKwh, decimal VatRate, SettlementStatus Status,
        DateTime? PaidAt, string? PaymentReference, DateTime GeneratedAt);

    public sealed record SettlementLineDto(int Id, string SourceKey, int? ChargingSessionId, int? RefundId, DateTime Date, string? Station,
        string? ChargePoint, decimal Kwh, decimal Gross, decimal NetExclVat, decimal Vat, decimal CommissionExclVat, decimal CommissionVat, decimal NetPayout);

    public sealed record SettlementDetailDto(SettlementDto Settlement, IReadOnlyList<SettlementLineDto> Lines);

    public sealed record GenerateSettlementsRequest(int Year, int Month, int? PartnerId = null);

    public sealed record MarkSettlementPaidRequest(string Reference, DateTime? PaidAt = null);

    public sealed class SettlementFilter
    {
        public int? PartnerId { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public SettlementStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    /// <summary>
    /// Monthly partner settlements. For a month, a partner's settlement takes the final sessions billed on its
    /// stations that ended before the month's end and are not settled yet (late ones from earlier months since
    /// its first settlement included), and the refunds of settled sessions. Per line: net revenue excl. VAT and
    /// VAT split from the amount billed, commission = net × rate + kWh × fixed fee (excl. VAT), VAT on the
    /// commission, net payout = gross − commission − its VAT. Totals are the sums of the lines. A pending
    /// settlement can be regenerated; a paid one is frozen.
    /// </summary>
    public sealed class PartnerSettlementService
    {
        private const int MaxPageSize = 100;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly GlobalConfigurations _pricing;
        private readonly InvoicingOptions _invoicing;
        private readonly ILogger<PartnerSettlementService> _logger;

        public PartnerSettlementService(VoltaXApiDbContext db, IBusinessClock clock, GlobalConfigurations pricing, IOptions<InvoicingOptions> invoicing,
            ILogger<PartnerSettlementService> logger)
        {
            _db = db;
            _clock = clock;
            _pricing = pricing;
            _invoicing = invoicing.Value;
            _logger = logger;
        }

        // -------------------------------------------------------------------------------- commission terms

        public Task<List<PartnerCommissionDto>> CommissionsAsync(CancellationToken ct = default) =>
            _db.Partners.AsNoTracking().OrderBy(p => p.Name)
                .Select(p => new PartnerCommissionDto(p.ID, p.Name, p.CommissionRate * 100, p.CommissionPerKwh))
                .ToListAsync(ct);

        public async Task<PartnerCommissionDto?> UpdateCommissionAsync(int partnerId, UpdateCommissionRequest request, CancellationToken ct = default)
        {
            if (request.CommissionPercent is < 0 or > 100 || request.CommissionPerKwh is < 0 or > 100)
                throw new Exceptions.ValidationException("The commission must be between 0 and 100 % and the fee per kWh between 0 and 100 MAD.");
            var partner = await _db.Partners.FirstOrDefaultAsync(p => p.ID == partnerId, ct);
            if (partner == null)
                return null;
            partner.CommissionRate = Math.Round(request.CommissionPercent / 100, 4);
            partner.CommissionPerKwh = Math.Round(request.CommissionPerKwh, 4);
            await _db.SaveChangesAsync(ct); // Partner changes are audited
            return new PartnerCommissionDto(partner.ID, partner.Name, partner.CommissionRate * 100, partner.CommissionPerKwh);
        }

        // -------------------------------------------------------------------------------- generation

        /// <summary>
        /// Computes the settlement of <paramref name="year"/>/<paramref name="month"/> for one partner: creates it,
        /// recomputes it when it is pending, leaves it when paid. Null when there is nothing to settle.
        /// </summary>
        public async Task<PartnerSettlement?> GenerateAsync(int partnerId, int year, int month, int? userId = null, CancellationToken ct = default)
        {
            if (month is < 1 or > 12 || year is < 2000 or > 2100)
                throw new Exceptions.ValidationException("Give a month between 1 and 12 and a valid year.");
            var periodStart = _clock.StartOfMonthUtc(year, month);
            var next = new DateTime(year, month, 1).AddMonths(1);
            var periodEnd = _clock.StartOfMonthUtc(next.Year, next.Month);
            if (periodStart >= _clock.UtcNow)
                throw new Exceptions.ValidationException("A month is settled once it has started.");

            var partner = await _db.Partners.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ID == partnerId, ct)
                ?? throw new Exceptions.NotFoundException("There is no such partner.");
            var settlement = await _db.Set<PartnerSettlement>().Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.PartnerID == partnerId && s.Year == year && s.Month == month, ct);
            if (settlement?.Status == SettlementStatus.Paid)
                return settlement;

            var ownIds = settlement?.ID;
            // Late sessions are taken back to the partner's first settled month, never before it.
            var firstPeriod = await _db.Set<PartnerSettlement>().AsNoTracking().Where(s => s.PartnerID == partnerId)
                .Select(s => (DateTime?)s.PeriodStart).MinAsync(ct);
            var lowerBound = firstPeriod.HasValue && firstPeriod < periodStart ? firstPeriod.Value : periodStart;
            var unsettledBefore = _clock.UtcNow.AddHours(-Math.Max(1, _invoicing.UnsettledSessionDelayHours));

            var settledElsewhere = _db.Set<PartnerSettlementLine>().Where(l => ownIds == null || l.SettlementID != ownIds);
            var sessions = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                .Where(s => !s.IsDeleted && s.Connector!.ChargePoint!.ChargingStation!.PartnerID == partnerId
                    && s.EndDate != null && s.EndDate >= lowerBound && s.EndDate < periodEnd
                    && (s.ChargingSessionStatus == ChargingSessionStatusEnum.Completed || s.EndDate < unsettledBefore)
                    && !settledElsewhere.Any(l => l.ChargingSessionID == s.ID && l.RefundID == null))
                .Select(s => new
                {
                    Session = s,
                    Station = s.Connector!.ChargePoint!.ChargingStation!.Name,
                    ChargePoint = s.Connector!.ChargePoint!.ChargePointId
                })
                .ToListAsync(ct);
            var includedIds = sessions.Select(s => s.Session.ID).ToList();
            var refunds = await _db.Set<Refund>().AsNoTracking()
                .Where(r => r.Target == RefundTarget.Session && r.Status == RefundStatus.Succeeded && r.CompletedAt != null
                    && r.CompletedAt >= lowerBound && r.CompletedAt < periodEnd
                    && (includedIds.Contains(r.ChargingSessionID!.Value) || settledElsewhere.Any(l => l.ChargingSessionID == r.ChargingSessionID && l.RefundID == null))
                    && !settledElsewhere.Any(l => l.RefundID == r.ID))
                .Select(r => new
                {
                    Refund = r,
                    Session = _db.ChargingSessions.IgnoreQueryFilters().Where(s => s.ID == r.ChargingSessionID)
                        .Select(s => new { s.VatRate, Station = s.Connector!.ChargePoint!.ChargingStation!.Name, ChargePoint = s.Connector!.ChargePoint!.ChargePointId })
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            var terms = new Terms(partner.CommissionRate, partner.CommissionPerKwh, _pricing.Vat);
            var lines = new List<PartnerSettlementLine>();
            foreach (var row in sessions.OrderBy(s => s.Session.StartDate).ThenBy(s => s.Session.ID))
            {
                var s = row.Session;
                var gross = s.TotalPriceWithVAT(s.VatRate ?? _pricing.Vat, (int)_pricing.GracePeriod);
                if (gross <= 0 && (s.ChargedKwhs ?? 0) <= 0)
                    continue;
                lines.Add(Line($"session:{s.ID}", s.ID, null, s.StartDate, row.Station, row.ChargePoint,
                    Math.Round((decimal)(s.ChargedKwhs ?? 0), 3, MidpointRounding.AwayFromZero), gross, s.VatRate ?? _pricing.Vat, terms));
            }
            foreach (var row in refunds.OrderBy(r => r.Refund.CompletedAt))
                lines.Add(Line($"refund:{row.Refund.ID}", row.Refund.ChargingSessionID, row.Refund.ID, row.Refund.CompletedAt!.Value,
                    row.Session?.Station, row.Session?.ChargePoint, 0, -row.Refund.Amount, row.Session?.VatRate ?? _pricing.Vat, terms));

            if (lines.Count == 0 && settlement == null)
                return null;

            if (settlement == null)
            {
                settlement = new PartnerSettlement { PartnerID = partnerId, Year = year, Month = month };
                _db.Set<PartnerSettlement>().Add(settlement);
            }
            else
                _db.Set<PartnerSettlementLine>().RemoveRange(settlement.Lines);
            settlement.PeriodStart = periodStart;
            settlement.PeriodEnd = periodEnd;
            settlement.Lines = lines;
            settlement.SessionCount = lines.Count(l => l.RefundID == null);
            settlement.EnergyKwh = lines.Sum(l => l.Kwh);
            settlement.GrossAmount = lines.Sum(l => l.Gross);
            settlement.NetExclVat = lines.Sum(l => l.NetExclVat);
            settlement.RevenueVat = lines.Sum(l => l.Vat);
            settlement.CommissionExclVat = lines.Sum(l => l.CommissionExclVat);
            settlement.CommissionVat = lines.Sum(l => l.CommissionVat);
            settlement.NetPayout = lines.Sum(l => l.NetPayout);
            settlement.CommissionRate = terms.Rate;
            settlement.CommissionPerKwh = terms.PerKwh;
            settlement.VatRate = terms.VatRate;
            settlement.Status = SettlementStatus.Pending;
            settlement.GeneratedAt = _clock.UtcNow;
            settlement.GeneratedByUserID = userId;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Settlement {Year}-{Month:00} of partner {PartnerId}: {Sessions} sessions, gross {Gross}, payout {Payout}",
                year, month, partnerId, settlement.SessionCount, settlement.GrossAmount, settlement.NetPayout);
            return settlement;
        }

        /// <summary>Generates the month for every partner; <paramref name="onlyMissing"/> leaves existing settlements as they are (monthly job).</summary>
        public async Task<List<PartnerSettlement>> GenerateAllAsync(int year, int month, int? userId = null, bool onlyMissing = false, CancellationToken ct = default)
        {
            var partners = await _db.Partners.AsNoTracking().Select(p => p.ID).ToListAsync(ct);
            var existing = onlyMissing
                ? await _db.Set<PartnerSettlement>().AsNoTracking().Where(s => s.Year == year && s.Month == month).Select(s => s.PartnerID).ToListAsync(ct)
                : new List<int>();
            var result = new List<PartnerSettlement>();
            foreach (var partnerId in partners.Except(existing))
            {
                try
                {
                    if (await GenerateAsync(partnerId, year, month, userId, ct) is { } settlement)
                        result.Add(settlement);
                }
                catch (DbUpdateException ex)
                {
                    // Generated at the same moment by another instance: theirs stands.
                    _logger.LogWarning(ex, "Settlement {Year}-{Month:00} of partner {PartnerId} was generated concurrently", year, month, partnerId);
                    _db.ChangeTracker.Clear();
                }
            }
            return result;
        }

        public async Task<SettlementDto?> MarkPaidAsync(int settlementId, MarkSettlementPaidRequest request, int userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > 128)
                throw new Exceptions.ValidationException("Give the payment reference (bank transfer, cheque...), up to 128 characters.");
            var settlement = await _db.Set<PartnerSettlement>().FirstOrDefaultAsync(s => s.ID == settlementId, ct);
            if (settlement == null)
                return null;
            if (settlement.Status == SettlementStatus.Paid)
                throw new SettlementConflictException("This settlement is already marked paid.");
            var paidAt = request.PaidAt?.ToUniversalTime() ?? _clock.UtcNow;
            if (paidAt > _clock.UtcNow.AddDays(1))
                throw new Exceptions.ValidationException("The payment date cannot be in the future.");
            settlement.Status = SettlementStatus.Paid;
            settlement.PaidAt = paidAt;
            settlement.PaymentReference = request.Reference.Trim();
            settlement.PaidByUserID = userId;
            await _db.SaveChangesAsync(ct);
            return await GetAsync(settlementId, null, ct);
        }

        // -------------------------------------------------------------------------------- reads

        public async Task<PagedResult<SettlementDto>> SearchAsync(SettlementFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
            var query = _db.Set<PartnerSettlement>().AsNoTracking();
            if (filter.PartnerId.HasValue) query = query.Where(s => s.PartnerID == filter.PartnerId);
            if (filter.Year.HasValue) query = query.Where(s => s.Year == filter.Year);
            if (filter.Month.HasValue) query = query.Where(s => s.Month == filter.Month);
            if (filter.Status.HasValue) query = query.Where(s => s.Status == filter.Status);
            var total = await query.CountAsync(ct);
            var items = await Project(query.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ThenBy(s => s.PartnerID)
                .Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
            return new PagedResult<SettlementDto>(items, page, pageSize, total);
        }

        /// <summary>One settlement, only when it belongs to <paramref name="partnerId"/> (null: any partner).</summary>
        public Task<SettlementDto?> GetAsync(int settlementId, int? partnerId, CancellationToken ct = default) =>
            Project(_db.Set<PartnerSettlement>().AsNoTracking().Where(s => s.ID == settlementId && (partnerId == null || s.PartnerID == partnerId)))
                .FirstOrDefaultAsync(ct);

        public async Task<SettlementDetailDto?> DetailAsync(int settlementId, int? partnerId, CancellationToken ct = default)
        {
            var settlement = await GetAsync(settlementId, partnerId, ct);
            if (settlement == null)
                return null;
            var lines = await _db.Set<PartnerSettlementLine>().AsNoTracking().Where(l => l.SettlementID == settlementId)
                .OrderBy(l => l.Date).ThenBy(l => l.ID)
                .Select(l => new SettlementLineDto(l.ID, l.SourceKey, l.ChargingSessionID, l.RefundID, l.Date, l.Station, l.ChargePoint, l.Kwh, l.Gross,
                    l.NetExclVat, l.Vat, l.CommissionExclVat, l.CommissionVat, l.NetPayout))
                .ToListAsync(ct);
            return new SettlementDetailDto(settlement, lines);
        }

        // -------------------------------------------------------------------------------- internals

        private sealed record Terms(decimal Rate, decimal PerKwh, decimal VatRate);

        private static PartnerSettlementLine Line(string key, int? sessionId, int? refundId, DateTime date, string? station, string? chargePoint,
            decimal kwh, decimal gross, decimal salesVatRate, Terms terms)
        {
            var (net, vat) = CostCalculator.SplitVat(gross, salesVatRate);
            var commission = CostCalculator.Round(net * terms.Rate) + CostCalculator.Round(kwh * terms.PerKwh);
            var commissionVat = CostCalculator.Round(commission * terms.VatRate);
            return new PartnerSettlementLine
            {
                SourceKey = key,
                ChargingSessionID = sessionId,
                RefundID = refundId,
                Date = date,
                Station = Truncate(station, 128),
                ChargePoint = Truncate(chargePoint, 64),
                Kwh = kwh,
                Gross = gross,
                NetExclVat = net,
                Vat = vat,
                CommissionExclVat = commission,
                CommissionVat = commissionVat,
                NetPayout = gross - commission - commissionVat
            };
        }

        private IQueryable<SettlementDto> Project(IQueryable<PartnerSettlement> query) =>
            query.Select(s => new SettlementDto(s.ID, s.PartnerID,
                _db.Partners.IgnoreQueryFilters().Where(p => p.ID == s.PartnerID).Select(p => p.Name).FirstOrDefault() ?? "",
                s.Year, s.Month, s.PeriodStart, s.PeriodEnd, s.SessionCount, s.EnergyKwh, s.GrossAmount, s.NetExclVat, s.RevenueVat,
                s.CommissionExclVat, s.CommissionVat, s.NetPayout, s.CommissionRate * 100, s.CommissionPerKwh, s.VatRate, s.Status, s.PaidAt,
                s.PaymentReference, s.GeneratedAt));

        private static string? Truncate(string? value, int length) => value == null || value.Length <= length ? value : value[..length];

        public static string PeriodLabel(int year, int month, string language) =>
            new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo(language switch { "ar" => "ar-MA", "en" => "en-GB", _ => "fr-FR" }));
    }

    public sealed class SettlementConflictException(string message) : Exception(message);

    /// <summary>
    /// Settles the previous month for every partner that has no settlement for it yet, from the configured day of
    /// the month (business time zone). Checks every 6 hours; idempotent, safe on several instances (unique
    /// settlement per partner and month, unique line per session).
    /// </summary>
    public sealed class PartnerSettlementJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<PartnerSettlementJob> _logger;

        public PartnerSettlementJob(IServiceScopeFactory scopes, ILogger<PartnerSettlementJob> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            do
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    await RunAsync(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Monthly partner settlement failed");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        /// <summary>One pass: returns the settlements created.</summary>
        public static async Task<List<PartnerSettlement>> RunAsync(IServiceProvider services, CancellationToken ct = default)
        {
            var clock = services.GetRequiredService<IBusinessClock>();
            var options = services.GetRequiredService<IOptions<SettlementOptions>>().Value;
            var today = clock.Today;
            if (today.Day < Math.Clamp(options.GenerateOnDay, 1, 28))
                return new List<PartnerSettlement>();
            var previous = today.AddMonths(-1);
            return await services.GetRequiredService<PartnerSettlementService>().GenerateAllAsync(previous.Year, previous.Month, onlyMissing: true, ct: ct);
        }
    }
}
