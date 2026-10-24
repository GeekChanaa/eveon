using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Sessions
{
    /// <summary>
    /// Network-wide search over charging sessions and OCPP transactions for the operator dashboard: filters, stable
    /// ordering, bounded pages and the rows of CSV / PDF exports. Sessions on equipment deleted since are kept.
    /// </summary>
    public sealed class OperatorSearchService
    {
        public const int MaxPageSize = 100;
        public const int MaxExportRows = 20_000;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly GlobalConfigurations _pricing;

        public OperatorSearchService(VoltaXApiDbContext db, IBusinessClock clock, GlobalConfigurations pricing)
        {
            _db = db;
            _clock = clock;
            _pricing = pricing;
        }

        // -------------------------------------------------------------------------------- sessions

        public async Task<PagedResult<OperatorSessionDto>> SessionsAsync(OperatorSessionFilter filter, CancellationToken ct = default)
        {
            var (page, pageSize) = Paging(filter.Page, filter.PageSize);
            var query = FilteredSessions(filter);
            var total = await query.CountAsync(ct);
            var rows = await Project(query.OrderByDescending(s => s.StartDate).ThenByDescending(s => s.ID).Skip((page - 1) * pageSize).Take(pageSize))
                .ToListAsync(ct);
            return new PagedResult<OperatorSessionDto>(rows.Select(ToDto).ToList(), page, pageSize, total);
        }

        /// <summary>Sessions of the filter as statement rows (oldest first), at most <see cref="MaxExportRows"/>.</summary>
        public async Task<IReadOnlyList<MySessionDto>> SessionStatementAsync(OperatorSessionFilter filter, CancellationToken ct = default)
        {
            var rows = await Project(FilteredSessions(filter).OrderBy(s => s.StartDate).ThenBy(s => s.ID).Take(MaxExportRows)).ToListAsync(ct);
            return rows.Select(r =>
            {
                var billed = r.Session.TotalAmount.HasValue;
                return new MySessionDto(r.Session.ID, r.Station, null, r.ChargePoint, r.Connector, r.Session.StartDate, r.Session.EndDate,
                    r.Session.ChargedMinutes ?? 0, r.Session.IdleMinutes ?? 0, r.Session.ChargedKwhs ?? 0,
                    billed ? r.Session.EnergyAmount ?? 0 : 0, billed ? r.Session.TimeAmount ?? 0 : r.Session.ChargingPriceWithVAT,
                    billed ? r.Session.FlatFeeAmount ?? 0 : 0, r.Session.IdleChargingPriceWithVAT((int)_pricing.GracePeriod),
                    Total(r.Session), r.Session.StoppedReason, r.Session.ChargingSessionStatus, r.Session.EndDate == null, false);
            }).ToList();
        }

        private IQueryable<ChargingSession> FilteredSessions(OperatorSessionFilter filter)
        {
            var sessions = _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters().Where(s => !s.IsDeleted);
            if (filter.From.HasValue)
            {
                var from = _clock.StartOfDayUtc(filter.From.Value.Date);
                sessions = sessions.Where(s => s.StartDate >= from);
            }
            if (filter.To.HasValue)
            {
                var to = _clock.StartOfDayUtc(filter.To.Value.Date.AddDays(1));
                sessions = sessions.Where(s => s.StartDate < to);
            }
            if (filter.StationId.HasValue)
                sessions = sessions.Where(s => s.Connector!.ChargePoint!.ChargingStationID == filter.StationId);
            if (filter.ChargePointId.HasValue)
                sessions = sessions.Where(s => s.Connector!.ChargePointID == filter.ChargePointId);
            if (filter.UserId.HasValue)
                sessions = sessions.Where(s => s.UserID == filter.UserId);
            if (string.Equals(filter.Status, "active", StringComparison.OrdinalIgnoreCase))
                sessions = sessions.Where(s => s.EndDate == null);
            else if (string.Equals(filter.Status, "completed", StringComparison.OrdinalIgnoreCase))
                sessions = sessions.Where(s => s.EndDate != null);
            var search = filter.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                // "#123" is a session id; a plain number may be an id or part of a card number / charge point name.
                var exactId = search.StartsWith('#') && int.TryParse(search[1..], out var hashId) ? hashId : (int?)null;
                var id = int.TryParse(search, out var plainId) ? plainId : -1;
                if (exactId.HasValue)
                    sessions = sessions.Where(s => s.ID == exactId.Value);
                else
                    sessions = sessions.Where(s => s.ID == id ||
                        s.Connector!.ChargePoint!.ChargePointId.Contains(search) ||
                        s.Connector.ChargePoint.ChargingStation!.Name.Contains(search) ||
                        s.Card!.CardNumber.Contains(search) ||
                        s.User!.Email.Contains(search) ||
                        (s.User.FirstName + " " + s.User.LastName).Contains(search));
            }
            return sessions;
        }

        private sealed class SessionRow
        {
            public ChargingSession Session { get; init; } = null!;
            public int? ChargePointId { get; init; }
            public string? Station { get; init; }
            public string? ChargePoint { get; init; }
            public string? Connector { get; init; }
            public string? UserName { get; init; }
            public string? CardNumber { get; init; }
        }

        /// <summary>The columns a row needs, without loading users (password hashes) or whole entities.</summary>
        private static IQueryable<SessionRow> Project(IQueryable<ChargingSession> sessions) => sessions.Select(s => new SessionRow
        {
            Session = new ChargingSession
            {
                ID = s.ID, ConnectorID = s.ConnectorID, UserID = s.UserID, StartDate = s.StartDate, EndDate = s.EndDate,
                ChargedMinutes = s.ChargedMinutes, IdleMinutes = s.IdleMinutes, ChargedKwhs = s.ChargedKwhs,
                PricePerMinute = s.PricePerMinute, PricePerIdleMinute = s.PricePerIdleMinute, EnergyAmount = s.EnergyAmount,
                TimeAmount = s.TimeAmount, FlatFeeAmount = s.FlatFeeAmount, IdleAmount = s.IdleAmount, TotalAmount = s.TotalAmount,
                VatRate = s.VatRate, StoppedReason = s.StoppedReason, ChargingSessionStatus = s.ChargingSessionStatus
            },
            ChargePointId = s.Connector != null ? s.Connector.ChargePointID : null,
            Station = s.Connector!.ChargePoint!.ChargingStation!.Name,
            ChargePoint = s.Connector.ChargePoint.ChargePointId,
            Connector = s.Connector.ConnectorID == null || s.Connector.ConnectorID == 1
                ? "EVSE " + s.Connector.EvseID
                : "EVSE " + s.Connector.EvseID + " / " + s.Connector.ConnectorID,
            UserName = s.User != null ? s.User.FirstName + " " + s.User.LastName : null,
            CardNumber = s.Card != null ? s.Card.CardNumber : null
        });

        private OperatorSessionDto ToDto(SessionRow r) => new(r.Session.ID, r.ChargePointId, r.Station, r.ChargePoint, r.Connector,
            r.Session.UserID, r.UserName, Last4(r.CardNumber), r.Session.StartDate, r.Session.EndDate, r.Session.ChargedMinutes ?? 0,
            r.Session.IdleMinutes ?? 0, r.Session.ChargedKwhs ?? 0, Total(r.Session), r.Session.StoppedReason,
            r.Session.ChargingSessionStatus, r.Session.EndDate == null, r.Session.TotalAmount.HasValue);

        /// <summary>The billed total; the running cost while the session is live; the legacy formula for old sessions.</summary>
        private decimal Total(ChargingSession s) => s.TotalPriceWithVAT(_pricing.Vat, (int)_pricing.GracePeriod);

        // -------------------------------------------------------------------------------- transactions

        public async Task<PagedResult<OperatorTransactionDto>> TransactionsAsync(OperatorTransactionFilter filter, CancellationToken ct = default)
        {
            var (page, pageSize) = Paging(filter.Page, filter.PageSize);
            var query = FilteredTransactions(filter);
            var total = await query.CountAsync(ct);
            var rows = await ProjectTransactions(query.OrderByDescending(t => t.StartTime).ThenByDescending(t => t.ID).Skip((page - 1) * pageSize).Take(pageSize))
                .ToListAsync(ct);
            return new PagedResult<OperatorTransactionDto>(rows, page, pageSize, total);
        }

        public Task<List<OperatorTransactionDto>> TransactionExportAsync(OperatorTransactionFilter filter, CancellationToken ct = default) =>
            ProjectTransactions(FilteredTransactions(filter).OrderBy(t => t.StartTime).ThenBy(t => t.ID).Take(MaxExportRows)).ToListAsync(ct);

        private IQueryable<Transaction> FilteredTransactions(OperatorTransactionFilter filter)
        {
            var transactions = _db.Transactions.AsNoTracking().IgnoreQueryFilters().Where(t => !t.IsDeleted);
            if (filter.From.HasValue)
            {
                var from = _clock.StartOfDayUtc(filter.From.Value.Date);
                transactions = transactions.Where(t => t.StartTime >= from);
            }
            if (filter.To.HasValue)
            {
                var to = _clock.StartOfDayUtc(filter.To.Value.Date.AddDays(1));
                transactions = transactions.Where(t => t.StartTime < to);
            }
            if (filter.ChargePointId.HasValue)
                transactions = transactions.Where(t => t.Connector!.ChargePointID == filter.ChargePointId);
            if (filter.Status.HasValue)
                transactions = transactions.Where(t => t.Status == filter.Status);
            var search = filter.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                transactions = transactions.Where(t =>
                    t.Uid == search ||
                    t.Connector!.ChargePoint!.ChargePointId.Contains(search) ||
                    t.StartCard!.CardNumber.Contains(search) ||
                    t.StopCard!.CardNumber.Contains(search) ||
                    t.StopReason!.Contains(search));
            return transactions;
        }

        private static IQueryable<OperatorTransactionDto> ProjectTransactions(IQueryable<Transaction> transactions) => transactions.Select(t =>
            new OperatorTransactionDto(t.ID, t.Uid, t.ChargingSessionID, t.Connector != null ? t.Connector.ChargePointID : null,
                t.Connector!.ChargePoint!.ChargePointId, t.Connector.ChargePoint.ChargingStation!.Name,
                t.Connector.ConnectorID == null || t.Connector.ConnectorID == 1
                    ? "EVSE " + t.Connector.EvseID
                    : "EVSE " + t.Connector.EvseID + " / " + t.Connector.ConnectorID,
                t.StartTime, t.StopTime, t.MeterStart, t.MeterStop, t.StartCard != null ? t.StartCard.CardNumber : null,
                t.StartResult, t.StopReason, t.Status, t.Amount));

        // -------------------------------------------------------------------------------- helpers

        private static (int Page, int PageSize) Paging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize));

        private static string? Last4(string? number) => string.IsNullOrEmpty(number) ? null : number.Length <= 4 ? number : number[^4..];
    }
}
