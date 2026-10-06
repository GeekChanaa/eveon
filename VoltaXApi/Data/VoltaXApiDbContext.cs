using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class VoltaXApiDbContext : DbContext, Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.IDataProtectionKeyContext
    {
        // Data Protection key ring (protects TOTP secrets); persisted so restarts / instances share it.
        public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<PhoneLoginChallenge> PhoneLoginChallenges { get; set; }
        private IConfiguration _configuration;
        private readonly IHttpContextAccessor? _httpContextAccessor;


        public VoltaXApiDbContext(
            DbContextOptions<VoltaXApiDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
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

            modelBuilder.Entity<CardChangeHistory>(entity =>
            {
                entity.HasIndex(history => new { history.CardID, history.ChangedAtUtc });
                entity.HasIndex(history => history.ChangeSetID);

                entity.HasOne(history => history.Card)
                    .WithMany()
                    .HasForeignKey(history => history.CardID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(history => history.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(history => history.ChangedByUserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ChargePointProvisioning>(entity =>
            {
                entity.HasIndex(p => p.ChargePointID).IsUnique();

                entity.HasOne(p => p.ChargePoint)
                    .WithMany()
                    .HasForeignKey(p => p.ChargePointID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.ProvisionedByUser)
                    .WithMany()
                    .HasForeignKey(p => p.ProvisionedByUserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OcppDefaultVariable>()
                .HasIndex(v => new { v.GroupName, v.SortOrder });

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
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.AuthProvider)
                .HasConversion<string>()
                .HasDefaultValue(AuthProviderEnum.Local);

            // Unique only across the rows that actually carry a Google identity.
            // SQL Server counts NULLs as duplicates in a unique index, hence the filter;
            // MySQL / MariaDB already allow several NULLs and reject filtered indexes.
            var googleIdIndex = modelBuilder.Entity<User>()
                .HasIndex(u => u.GoogleId)
                .IsUnique();

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
                googleIdIndex.HasFilter("[GoogleId] IS NOT NULL");

            // A phone number signs a user in on its own, so it identifies exactly one
            // account. Always stored in the "+212XXXXXXXXX" shape (see PhoneHelper), which
            // is what makes comparing them - and this index - meaningful.
            modelBuilder.Entity<User>()
                .Property(u => u.Phone)
                .HasMaxLength(20);

            var phoneIndex = modelBuilder.Entity<User>()
                .HasIndex(u => u.Phone)
                .IsUnique();

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
                phoneIndex.HasFilter("[Phone] IS NOT NULL");

            modelBuilder.Entity<Partner>()
                .HasIndex(u => u.PartnerIdentificationNumber)
                .IsUnique();

            // Refresh tokens: looked up by hash on every refresh, and wiped with the account.
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(t => t.IsDeleted).HasDefaultValue(false);
                entity.Property(t => t.RevokedAt).IsConcurrencyToken();

                entity.HasIndex(t => t.TokenHash).IsUnique();
                entity.HasIndex(t => t.UserID);

                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // GDPR / audit / retention
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.OccurredAt);
                entity.HasIndex(a => new { a.EntityType, a.EntityID });
                entity.HasIndex(a => a.UserID);
            });

            modelBuilder.Entity<AccountDeletionRequest>(entity =>
            {
                entity.HasIndex(d => new { d.Status, d.ScheduledFor });
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Retention deletes by age; without these the sweeps scan the whole table.
            modelBuilder.Entity<MessageLog>()
                .HasIndex(ml => ml.LogTime);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.Read, n.CreatedAt });

            // OCPI roaming
            modelBuilder.Entity<VoltaXApi.Ocpi.Models.OcpiParty>(entity =>
            {
                entity.HasIndex(p => p.IncomingTokenHash);
                entity.HasIndex(p => p.TokenAHash);
                entity.HasIndex(p => new { p.CountryCode, p.PartyId });
            });
            modelBuilder.Entity<VoltaXApi.Ocpi.Models.OcpiToken>(entity =>
            {
                entity.HasIndex(t => new { t.OcpiPartyID, t.CountryCode, t.PartyId, t.Uid, t.Type }).IsUnique();
                entity.HasIndex(t => t.Uid);
                entity.HasOne(t => t.OcpiParty).WithMany().HasForeignKey(t => t.OcpiPartyID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VoltaXApi.Ocpi.Models.OcpiSession>(entity =>
            {
                entity.HasIndex(s => new { s.OcpiPartyID, s.LastUpdated });
                entity.HasIndex(s => new { s.ChargePointID, s.TransactionUid });
                entity.HasOne(s => s.OcpiParty).WithMany().HasForeignKey(s => s.OcpiPartyID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VoltaXApi.Ocpi.Models.OcpiReservation>()
                .HasIndex(r => new { r.OcpiPartyID, r.ReservationId });
            modelBuilder.Entity<VoltaXApi.Ocpi.Models.OcpiOutboxMessage>(entity =>
            {
                entity.ToTable("OcpiOutbox");
                entity.HasIndex(m => new { m.Status, m.NextAttemptAt });
                entity.HasIndex(m => m.OcpiPartyID);
                entity.HasOne(m => m.OcpiParty).WithMany().HasForeignKey(m => m.OcpiPartyID).OnDelete(DeleteBehavior.Restrict);
            });

            // OCPP 2.0.1 device data
            modelBuilder.Entity<VoltaXApi.Models.Ocpp201.ChargerEvent>(entity =>
            {
                entity.HasIndex(e => new { e.ChargePointID, e.Timestamp });
                entity.HasIndex(e => e.Timestamp);
            });
            modelBuilder.Entity<VoltaXApi.Models.Ocpp201.VariableMonitor>()
                .HasIndex(m => new { m.ChargePointID, m.MonitoringId });
            modelBuilder.Entity<VoltaXApi.Models.Ocpp201.CustomerInformationReport>()
                .HasIndex(r => new { r.ChargePointID, r.RequestId });
            modelBuilder.Entity<VoltaXApi.Models.Ocpp201.DisplayMessageSnapshot>()
                .HasIndex(m => new { m.ChargePointID, m.RequestId });
            modelBuilder.Entity<VoltaXApi.Models.Ocpp201.LogUploadTicket>(entity =>
            {
                entity.HasIndex(t => t.TokenHash).IsUnique();
                entity.HasIndex(t => new { t.ChargePointID, t.RequestId });
            });
            modelBuilder.Entity<VoltaXApi.Models.Ocpp201.UploadedChargerLog>(entity =>
            {
                entity.HasIndex(l => new { l.ChargePointID, l.UploadedAt });
                entity.HasOne(l => l.LogUploadTicket).WithMany().HasForeignKey(l => l.LogUploadTicketID).OnDelete(DeleteBehavior.Restrict);
            });

            // OCPP reservations (1.6 and 2.0.1), 1.6 transaction ids, 2.0.1 transactions waiting for their EVSE
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasIndex(r => new { r.ChargePointID, r.ReservationId });
                entity.HasIndex(r => new { r.Status, r.ExpiresAt });
                entity.HasOne(r => r.ChargePoint).WithMany().HasForeignKey(r => r.ChargePointID).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Connector).WithMany().HasForeignKey(r => r.ConnectorID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VoltaXApi.OCPP.Ocpp16.Ocpp16Transaction>(entity =>
            {
                entity.Ignore(t => t.Uid);
                entity.HasIndex(t => new { t.ChargePointID, t.StopTimestamp });
            });
            modelBuilder.Entity<OcppPendingTransaction>()
                .HasIndex(p => new { p.ChargePointID, p.TransactionUid }).IsUnique();

            // Charger PKI (OCPP security profile 3)
            modelBuilder.Entity<ChargePoint>()
                .Property(cp => cp.SecurityProfile)
                .HasDefaultValue(1);
            modelBuilder.Entity<ChargerCertificate>(entity =>
            {
                entity.Property(c => c.CertificateType).HasConversion<string>().HasMaxLength(32);
                entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);
                entity.HasIndex(c => new { c.ChargePointID, c.CertificateType, c.Status });
                entity.HasIndex(c => c.ThumbprintSha256);
                entity.HasIndex(c => new { c.Status, c.NotAfter });
                entity.HasOne(c => c.ChargePoint).WithMany().HasForeignKey(c => c.ChargePointID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<InstalledCertificateRecord>(entity =>
            {
                entity.HasIndex(r => r.ChargePointID);
                entity.HasOne(r => r.ChargePoint).WithMany().HasForeignKey(r => r.ChargePointID).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<PkiCertificateAuthority>()
                .HasIndex(a => a.Name)
                .IsUnique();

            // Smart charging: profiles, EV charging needs, station load balancing, strategies
            modelBuilder.Entity<ChargingProfile>(entity =>
            {
                entity.HasIndex(p => new { p.ChargePointID, p.EvseId, p.Status });
                entity.HasIndex(p => new { p.ChargePointID, p.OcppProfileId });
                entity.HasIndex(p => new { p.Source, p.Status });
                entity.HasOne(p => p.ChargePoint).WithMany().HasForeignKey(p => p.ChargePointID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<EvChargingNeeds>(entity =>
            {
                entity.HasIndex(n => new { n.ChargePointID, n.EvseId, n.ReceivedAt });
                entity.HasOne(n => n.ChargePoint).WithMany().HasForeignKey(n => n.ChargePointID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<StationLoadLimit>(entity =>
            {
                entity.HasIndex(l => l.ChargingStationID).IsUnique();
                entity.HasOne(l => l.ChargingStation).WithMany().HasForeignKey(l => l.ChargingStationID).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<StationLoadAllocation>(entity =>
            {
                entity.HasIndex(a => new { a.ChargingStationID, a.CreatedAt });
                entity.HasIndex(a => new { a.TransactionID, a.CreatedAt });
            });

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
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<OcppVariable> OcppVariables { get; set; }
        public DbSet<OcppComponent> OcppComponents { get; set; }
        public DbSet<OcppVariableComponent> OcppVariableComponents { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        public DbSet<SystemReport> SystemReports { get; set; }
        public DbSet<SystemReportComment> SystemReportComments { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardChangeHistory> CardChangeHistories { get; set; }
        public DbSet<CardExpirationNotification> CardExpirationNotifications { get; set; }
        public DbSet<ChargingStation> ChargingStations { get; set; }
        public DbSet<ElectricVehicleModel> ElectricVehicleModels { get; set; }
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
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportReply> ReportReplies { get; set; }
        public DbSet<CommentReply> CommentReplies { get; set; }
        public DbSet<CommentImage> CommentImages { get; set; }
        public DbSet<ReportImage> ReportImages { get; set; }
        public DbSet<ChargePointUptime> ChargePointUptimes { get; set; }
        public DbSet<ConnectorUptime> ConnectorUptimes { get; set; }
        public DbSet<ChargingSession> ChargingSessions { get; set; }
        public DbSet<UserInfoDownloadRequest> UserInfoDownloadRequests { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AccountDeletionRequest> AccountDeletionRequests { get; set; }
        public DbSet<ChargePointConfigurationItem> ChargePointConfigurationItems { get; set; }
        public DbSet<ConfigurationItemVariableAttribute> ConfigurationItemVariableAttributes { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        // OCPP Configurations
        public DbSet<OCPPConfigurationItem> OCPPConfigurationItems { get; set; }
        public DbSet<OCPPConfigurationComponent> OCPPConfigurationComponents { get; set; }
        public DbSet<OCPPConfigurationEVSE> OCPPConfigurationEVSEs { get; set; }
        public DbSet<OCPPConfigurationVariable> OCPPConfigurationVariables { get; set; }
        public DbSet<OCPPConfigurationVariableAttribute> OCPPConfigurationVariableAttributes { get; set; }
        public DbSet<OCPPConfigurationVariableCharacteristic> OCPPConfigurationVariableCharacteristics { get; set; }
        public DbSet<OcppDefaultVariable> OcppDefaultVariables { get; set; }
        public DbSet<ChargePointProvisioning> ChargePointProvisionings { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<VoltaXApi.OCPP.Ocpp16.Ocpp16Transaction> Ocpp16Transactions { get; set; }
        public DbSet<OcppPendingTransaction> OcppPendingTransactions { get; set; }

        // OCPI roaming
        public DbSet<VoltaXApi.Ocpi.Models.OcpiParty> OcpiParties { get; set; }
        public DbSet<VoltaXApi.Ocpi.Models.OcpiToken> OcpiTokens { get; set; }
        public DbSet<VoltaXApi.Ocpi.Models.OcpiSession> OcpiSessions { get; set; }
        public DbSet<VoltaXApi.Ocpi.Models.OcpiReservation> OcpiReservations { get; set; }
        public DbSet<VoltaXApi.Ocpi.Models.OcpiOutboxMessage> OcpiOutbox { get; set; }
        public DbSet<VoltaXApi.Ocpi.Models.OcpiSyncCursor> OcpiSyncCursors { get; set; }

        // Charger PKI
        public DbSet<ChargerCertificate> ChargerCertificates { get; set; }
        public DbSet<InstalledCertificateRecord> InstalledCertificateRecords { get; set; }
        public DbSet<PkiCertificateAuthority> PkiCertificateAuthorities { get; set; }

        // OCPP Display Messages
        public DbSet<OCPPDisplayMessageInfo> OCPPDisplayMessageInfos { get; set; }
        public DbSet<OCPPDisplayMessageContent> OCPPDisplayMessageContents { get; set; }

        // OCPP Local List
        public DbSet<OCPPLocalListItem> OCPPLocalListItems { get; set; }
        public DbSet<OCPPLocalListVersion> OCPPLocalListVersions { get; set; }

        // Smart charging
        public DbSet<ChargingProfile> ChargingProfiles { get; set; }
        public DbSet<EvChargingNeeds> EvChargingNeeds { get; set; }
        public DbSet<StationLoadLimit> StationLoadLimits { get; set; }
        public DbSet<StationLoadAllocation> StationLoadAllocations { get; set; }
        public DbSet<ChargingStrategy> ChargingStrategies { get; set; }

        // OCPP 2.0.1 device data: events, monitors, customer information, display messages, log uploads
        public DbSet<VoltaXApi.Models.Ocpp201.ChargerEvent> ChargerEvents { get; set; }
        public DbSet<VoltaXApi.Models.Ocpp201.VariableMonitor> VariableMonitors { get; set; }
        public DbSet<VoltaXApi.Models.Ocpp201.CustomerInformationReport> CustomerInformationReports { get; set; }
        public DbSet<VoltaXApi.Models.Ocpp201.DisplayMessageSnapshot> DisplayMessageSnapshots { get; set; }
        public DbSet<VoltaXApi.Models.Ocpp201.LogUploadTicket> LogUploadTickets { get; set; }
        public DbSet<VoltaXApi.Models.Ocpp201.UploadedChargerLog> UploadedChargerLogs { get; set; }
        

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnsureCardHistoryIsImmutable();
            UpdateTimestamps();
            SoftDelete();
            await AddCardChangeHistoryAsync(cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            EnsureCardHistoryIsImmutable();
            UpdateTimestamps();
            SoftDelete();
            AddCardChangeHistory();
            return base.SaveChanges();
        }

        private async Task AddCardChangeHistoryAsync(CancellationToken cancellationToken)
        {
            var entries = ChangeTracker.Entries<Card>()
                .Where(entry => entry.State == EntityState.Modified)
                .ToList();

            foreach (var entry in entries)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues != null)
                    AddCardChangeHistory(entry, databaseValues);
            }
        }

        private void AddCardChangeHistory()
        {
            var entries = ChangeTracker.Entries<Card>()
                .Where(entry => entry.State == EntityState.Modified)
                .ToList();

            foreach (var entry in entries)
            {
                var databaseValues = entry.GetDatabaseValues();
                if (databaseValues != null)
                    AddCardChangeHistory(entry, databaseValues);
            }
        }

        private void AddCardChangeHistory(
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Card> entry,
            Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues databaseValues)
        {
            var changes = entry.Properties
                .Where(property => property.Metadata.Name is not nameof(IEntity.CreatedAt) and not nameof(IEntity.UpdatedAt))
                .Select(property => new
                {
                    property.Metadata.Name,
                    OldValue = databaseValues[property.Metadata],
                    NewValue = property.CurrentValue
                })
                .Where(change => !Equals(change.OldValue, change.NewValue))
                .ToList();

            if (changes.Count == 0)
                return;

            var principal = _httpContextAccessor.HttpContext?.User;
            var changedByUserID = principal?.Identity?.IsAuthenticated == true &&
                int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserID)
                ? parsedUserID
                : (int?)null;
            var hasUser = changedByUserID.HasValue;
            var changedByName = hasUser
                ? string.Join(" ", new[]
                {
                    principal!.FindFirstValue(ClaimTypes.GivenName),
                    principal.FindFirstValue(ClaimTypes.Surname)
                }.Where(value => !string.IsNullOrWhiteSpace(value)))
                : "System";

            if (string.IsNullOrWhiteSpace(changedByName))
                changedByName = principal?.FindFirstValue(ClaimTypes.Name) ?? "System";

            var changeSetID = Guid.NewGuid();
            var changedAtUtc = DateTime.UtcNow;
            foreach (var change in changes)
            {
                CardChangeHistories.Add(new CardChangeHistory
                {
                    ChangeSetID = changeSetID,
                    CardID = entry.Entity.ID,
                    ChangedByUserID = changedByUserID,
                    ChangedByName = changedByName,
                    Source = hasUser ? "User" : "System",
                    PropertyName = change.Name,
                    OldValue = FormatAuditValue(change.OldValue),
                    NewValue = FormatAuditValue(change.NewValue),
                    ChangedAtUtc = changedAtUtc
                });
            }
        }

        private void EnsureCardHistoryIsImmutable()
        {
            if (ChangeTracker.Entries<CardChangeHistory>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            {
                throw new InvalidOperationException("Charging card history is immutable.");
            }
        }

        private static string? FormatAuditValue(object? value) => value switch
        {
            null => null,
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Enum enumValue => enumValue.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

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
