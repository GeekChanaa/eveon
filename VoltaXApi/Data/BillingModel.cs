using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Data
{
    /// <summary>Mapping of the billing tables (invoices, refunds, partner settlements), kept out of the main context.</summary>
    public static class BillingModel
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.Property(i => i.Kind).HasConversion<string>().HasMaxLength(16);
                entity.HasIndex(i => i.Number).IsUnique();
                entity.HasIndex(i => i.SourceReference).IsUnique();
                entity.HasIndex(i => i.ChargingSessionID);
                entity.HasIndex(i => i.OrderID);
                entity.HasIndex(i => new { i.UserID, i.IssuedAt });
                entity.HasIndex(i => i.IssuedAt);
                entity.HasOne(i => i.File).WithOne(f => f.Invoice).HasForeignKey<InvoiceFile>(f => f.InvoiceID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<InvoiceFile>().HasKey(f => f.InvoiceID);
            modelBuilder.Entity<InvoiceSequence>().HasKey(s => new { s.Series, s.Year });

            modelBuilder.Entity<Refund>(entity =>
            {
                entity.ToTable("Refunds");
                entity.Property(r => r.Target).HasConversion<string>().HasMaxLength(16);
                entity.Property(r => r.Method).HasConversion<string>().HasMaxLength(16);
                entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
                entity.HasIndex(r => r.IdempotencyKey).IsUnique();
                entity.HasIndex(r => r.OrderID);
                entity.HasIndex(r => r.ChargingSessionID);
                entity.HasIndex(r => r.CreatedAt);
            });

            modelBuilder.Entity<PartnerSettlement>(entity =>
            {
                entity.ToTable("PartnerSettlements");
                entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
                entity.HasIndex(s => new { s.PartnerID, s.Year, s.Month }).IsUnique();
                entity.HasIndex(s => new { s.Year, s.Month });
                entity.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SettlementID).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<PartnerSettlementLine>(entity =>
            {
                entity.ToTable("PartnerSettlementLines");
                entity.HasIndex(l => l.SourceKey).IsUnique();
                entity.HasIndex(l => l.ChargingSessionID);
            });
        }
    }
}
