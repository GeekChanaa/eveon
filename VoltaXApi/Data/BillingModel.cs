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
        }
    }
}
