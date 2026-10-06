using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Persistence of ReserveNow reservations (OCPP 1.6 and 2.0.1) and their lifecycle.</summary>
    public class ReservationService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(VoltaXApiDbContext db, ILogger<ReservationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Records an Active reservation before ReserveNow is sent. The charger id is <paramref name="requestedId"/>
        /// when positive (OCPI and the dashboard choose their own), otherwise the row id.
        /// Throws <see cref="InvalidOperationException"/> for an unknown charge point or an id that is already active on it.
        /// </summary>
        public async Task<Reservation> CreateAsync(string chargePointId, int? requestedId, int? evseId, string idToken, DateTime expiresAtUtc,
            CancellationToken cancellationToken = default)
        {
            var chargePoint = await _db.ChargePoints.AsNoTracking().FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId, cancellationToken)
                ?? throw new InvalidOperationException($"Unknown charge point {chargePointId}.");

            if (requestedId is > 0 && await _db.Reservations.AnyAsync(r => r.ChargePointID == chargePoint.ID
                    && r.ReservationId == requestedId && r.Status == ReservationStatusEnum.Active, cancellationToken))
                throw new InvalidOperationException($"Reservation {requestedId} is already active on {chargePointId}.");

            int? connectorId = null;
            if (evseId is > 0)
                connectorId = await _db.Connectors.AsNoTracking()
                    .Where(c => c.ChargePointID == chargePoint.ID && c.EvseID == evseId)
                    .OrderBy(c => c.ConnectorID).Select(c => (int?)c.ID).FirstOrDefaultAsync(cancellationToken);

            var userId = await _db.Cards.AsNoTracking().Where(c => c.CardNumber == idToken).Select(c => c.UserID).FirstOrDefaultAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var reservation = new Reservation
            {
                ReservationId = requestedId is > 0 ? requestedId.Value : 0,
                ChargePointID = chargePoint.ID,
                ConnectorID = connectorId,
                EvseId = evseId,
                IdToken = idToken,
                UserID = userId,
                ExpiresAt = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc),
                Status = ReservationStatusEnum.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync(cancellationToken);
            if (reservation.ReservationId == 0)
            {
                reservation.ReservationId = reservation.ID;
                await _db.SaveChangesAsync(cancellationToken);
            }
            return reservation;
        }

        public async Task SetStatusAsync(int reservationRowId, ReservationStatusEnum status, CancellationToken cancellationToken = default)
        {
            var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.ID == reservationRowId, cancellationToken);
            if (reservation == null) return;
            reservation.Status = status;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Moves the Active reservation <paramref name="reservationId"/> of the charger to <paramref name="status"/>; false when none.</summary>
        public async Task<bool> UpdateActiveAsync(string chargePointId, int reservationId, ReservationStatusEnum status,
            string? transactionUid = null, CancellationToken cancellationToken = default)
        {
            var reservation = await _db.Reservations
                .Where(r => r.ChargePoint!.ChargePointId == chargePointId && r.ReservationId == reservationId && r.Status == ReservationStatusEnum.Active)
                .OrderByDescending(r => r.ID)
                .FirstOrDefaultAsync(cancellationToken);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation {ReservationId} of {ChargePointId} is not active; {Status} ignored", reservationId, chargePointId, status);
                return false;
            }

            reservation.Status = status;
            reservation.TransactionUid = transactionUid ?? reservation.TransactionUid;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reservation {ReservationId} of {ChargePointId} is now {Status}", reservationId, chargePointId, status);
            return true;
        }

        public Task<bool> MarkUsedAsync(string chargePointId, int reservationId, string transactionUid, CancellationToken cancellationToken = default) =>
            UpdateActiveAsync(chargePointId, reservationId, ReservationStatusEnum.Used, transactionUid, cancellationToken);

        /// <summary>Marks every Active reservation past its expiry as Expired; returns how many.</summary>
        public async Task<int> ExpireDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var due = await _db.Reservations
                .Where(r => r.Status == ReservationStatusEnum.Active && r.ExpiresAt <= nowUtc)
                .Take(500)
                .ToListAsync(cancellationToken);
            foreach (var reservation in due)
            {
                reservation.Status = ReservationStatusEnum.Expired;
                reservation.UpdatedAt = nowUtc;
            }
            if (due.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);
            return due.Count;
        }
    }

    /// <summary>Expires reservations whose expiry passed without a transaction (the charger frees them on its own).</summary>
    public class ReservationExpiryService : BackgroundService
    {
        private static readonly TimeSpan Period = TimeSpan.FromMinutes(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationExpiryService> _logger;
        private readonly IClusterJobLease _lease;

        public ReservationExpiryService(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryService> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Period);
            do
            {
                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (await _lease.TryAcquireAsync("reservation-expiry", Period * 3, stoppingToken))
                    {
                        await using var scope = _scopeFactory.CreateAsyncScope();
                        var expired = await scope.ServiceProvider.GetRequiredService<ReservationService>().ExpireDueAsync(DateTime.UtcNow, stoppingToken);
                        if (expired > 0)
                            _logger.LogInformation("{Count} reservations expired", expired);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reservation expiry sweep failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
