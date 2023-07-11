using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class VoltaXApiDbContext : DbContext
    {
        private IConfiguration _configuration;


        public VoltaXApiDbContext(DbContextOptions<VoltaXApiDbContext> options) : base(options)
        {

        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Card)
                .WithMany(u => u.Orders)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChargeTag>()
                .HasIndex(e => e.TagID)
                .IsUnique();
            modelBuilder.Entity<ConnectorStatus>()
                .HasKey(cs => new { cs.ConnectorId, cs.ChargePointId });
            modelBuilder.Entity<Connector>()
                .HasIndex(cs => new { cs.ConnectorID, cs.ChargePointID })
                .IsUnique();
            
            modelBuilder.Entity<Transaction>()
                    .HasOne(t => t.ChargePoint)
                    .WithMany(cp => cp.Transactions)
                    .HasForeignKey(t => t.ChargePointID)
                    .HasPrincipalKey(cp => cp.ChargePointId); // new line
        }

            public DbSet<User> Users { get; set; }
            public DbSet<Administrator> Administrators { get; set; }

            public DbSet<Card> Cards { get; set; }
            public DbSet<ChargingStation> ChargingStations { get; set; }
            public DbSet<ChargePoint> ChargePoints { get; set; }
            public DbSet<ChargeTag> ChargeTags { get; set; }
            public DbSet<Connector> Connectors { get; set; }
            public DbSet<ConnectorTarif> ConnectorTarifs { get; set; }
            public DbSet<ConnectorStatus> ConnectorStatuses { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<Transaction> Transactions { get; set; }
            public DbSet<Comment> Comments { get; set; }
            public DbSet<MessageLog> MessageLogs { get; set; }
            public DbSet<Country> Countries { get; set; }
            public DbSet<State> States { get; set; }
            public DbSet<City> Cities { get; set; }
            public DbSet<DebitCard> DebitCards { get; set; }
            public DbSet<Notification> Notifications { get; set; }
            public DbSet<NotificationSetting> NotificationSettings { get; set; }
            public DbSet<NotificationType> NotificationTypes { get; set; }
            // Add any Dbset configurations here
    }
}