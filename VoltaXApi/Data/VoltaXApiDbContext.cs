using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class VoltaXApiDbContext : DbContext
    {
        private IConfiguration _configuration;


        public VoltaXApiDbContext(DbContextOptions<VoltaXApiDbContext> options) : base(options){
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemReport>(entity =>
            {
                entity.HasOne(sr => sr.Resolved)
                    .WithMany()
                    .HasForeignKey(sr => sr.ResolvedByID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sr => sr.Assigned)
                    .WithMany()
                    .HasForeignKey(sr => sr.AssignedID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ChargeTag>()
                .HasIndex(e => e.TagID)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Administrator>()
                .Property(a => a.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Card>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ChargingStation>()
                .Property(cs => cs.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ChargePoint>()
                .Property(cp => cp.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ChargeTag>()
                .Property(ct => ct.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Connector>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ConnectorStatus>()
                .Property(cs => cs.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Order>()
                .Property(o => o.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Comment>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<MessageLog>()
                .Property(ml => ml.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Country>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<State>()
                .Property(s => s.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<City>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<DebitCard>()
                .Property(dc => dc.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<NotificationSetting>()
                .Property(ns => ns.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<NotificationType>()
                .Property(nt => nt.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Image>()
                .Property(nt => nt.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ChargingStationImage>()
                .Property(nt => nt.IsDeleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ConnectorStatus>()
                .HasKey(cs => new { cs.ConnectorID, cs.ChargePointID });

            modelBuilder.Entity<Connector>()
                .HasIndex(cs => new { cs.EvseID,cs.ConnectorID, cs.ChargePointID })
                .IsUnique();

            modelBuilder.Entity<ChargePoint>()
                .HasIndex(e => e.ChargePointId)
                .IsUnique();

            modelBuilder.Entity<ChargingStation>()
                .HasIndex(e => e.Name)
                .IsUnique();

            modelBuilder.Entity<ChargePointUptime>()
                .Property(c => c.ChargePointUptimeStatus)
                .HasConversion<string>();

            modelBuilder.Entity<ConnectorUptime>()
                .Property(c => c.ConnectorUptimeStatus)
                .HasConversion<string>();


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IEntity).IsAssignableFrom(entityType.ClrType))
                {
                    
                    var method = typeof(VoltaXApiDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic 
                            | System.Reflection.BindingFlags.Static)
                        .MakeGenericMethod(entityType.ClrType);

                    method.Invoke(null, new object[] { modelBuilder });
                }
            }

        }
        

        public DbSet<User> Users { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<OcppVariable> OcppVariables { get; set; }
        public DbSet<OcppComponent> OcppComponents { get; set; }
        public DbSet<OcppVariableComponent> OcppVariableComponents { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        public DbSet<SystemReport> SystemReports { get; set; }
        public DbSet<SystemReportComment> SystemReportComments { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<ChargingStation> ChargingStations { get; set; }
        public DbSet<ChargePoint> ChargePoints { get; set; }
        public DbSet<ChargePointBrand> ChargePointBrands { get; set; }
        public DbSet<ChargePointFeatures> ChargePointFeaturess { get; set; }
        public DbSet<ChargePointIntegration> ChargePointIntegrations { get; set; }
        public DbSet<ChargePointModel> ChargePointModels { get; set; }
        public DbSet<SupportedKwh> SupportedKwhs { get; set; }
        public DbSet<ChargeTag> ChargeTags { get; set; }
        public DbSet<Connector> Connectors { get; set; }
        public DbSet<ConnectorStatus> ConnectorStatuses { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<MessageLog> MessageLogs { get; set; }
        public DbSet<RatingReport> RatingReports { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<ChargingStationImage> ChargingStationImages { get; set; }
        public DbSet<DebitCard> DebitCards { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationSetting> NotificationSettings { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Automobile> Automobiles { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportReply> ReportReplies { get; set; }
        public DbSet<CommentReply> CommentReplies { get; set; }
        public DbSet<CommentImage> CommentImages { get; set; }
        public DbSet<ReportImage> ReportImages { get; set; }
        public DbSet<ChargePointUptime> ChargePointUptimes { get; set; }
        public DbSet<ConnectorUptime> ConnectorUptimes { get; set; }
        public DbSet<ChargingSession> ChargingSessions { get; set; }
        public DbSet<ChargePointConfigurationItem> ChargePointConfigurationItems { get; set; }
        public DbSet<ConfigurationItemVariableAttribute> ConfigurationItemVariableAttributes { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            SoftDelete();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            SoftDelete();
            return base.SaveChanges();
        }

        private void SoftDelete()
        {
            foreach (var entry in ChangeTracker.Entries<IEntity>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                }
            }
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<IEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Property(e => e.CreatedAt).IsModified = false;  
                }
            }
        }

        private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, IEntity
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }

        
    }
}