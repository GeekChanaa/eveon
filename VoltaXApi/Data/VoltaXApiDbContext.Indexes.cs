using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public partial class VoltaXApiDbContext
    {
        /// <summary>
        /// Indexes for the hot lookups and the statistics windows (migration QueryIndexes; see docs/performance.md).
        /// The string columns get a length so they can be index keys on SQL Server and MariaDB.
        /// </summary>
        private static void ConfigureQueryIndexes(ModelBuilder modelBuilder)
        {
            // OCPP Authorize / StartTransaction, OCPI and reservations look cards up by number.
            modelBuilder.Entity<Card>(entity =>
            {
                entity.Property(c => c.CardNumber).HasMaxLength(64);
                entity.HasIndex(c => c.CardNumber);
            });

            // TransactionEvent / StopTransaction find the transaction by the charger's id; energy series by start.
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Uid).HasMaxLength(64);
                entity.HasIndex(t => t.Uid);
                entity.HasIndex(t => t.StartTime);
            });

            // A driver's current session (polled by the app), a connector's sessions by start (station and
            // partner windows join through the connectors), and the operator-wide windows by start.
            modelBuilder.Entity<ChargingSession>(entity =>
            {
                entity.HasIndex(s => new { s.UserID, s.EndDate });
                entity.HasIndex(s => new { s.ConnectorID, s.StartDate });
                entity.HasIndex(s => s.StartDate);
            });

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.RechargeDate);

            // A charger's message log, newest first (same length as ChargePoints.ChargePointId).
            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.Property(m => m.ChargePointId).HasMaxLength(450);
                entity.HasIndex(m => new { m.ChargePointId, m.LogTime });
            });
        }
    }
}
