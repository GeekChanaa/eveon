using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Security and compliance hardening. Hand written for the same reason as AddCardChangeHistory
/// (the snapshot is MySQL-shaped while the application runs on SQL Server; a generated migration
/// would re-type every column).
///
///  - Users: password reset attempt/expiry, TOTP 2FA, GDPR deletion and terms acceptance columns.
///  - DebitCards: card number, CVV and expiry date are dropped. Last4/brand/expiry are derived from
///    the old card number first, then only provider tokens and display metadata remain.
///  - New tables: AuditLogs, AccountDeletionRequests, DataProtectionKeys (key ring that encrypts
///    TOTP secrets, shared by restarts and instances).
///  - UserInfoDownloadRequests: generated archive and single-use download token.
///  - Indexes used by the retention job on MessageLogs and Notifications.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261003150000_SecurityHardening")]
public partial class SecurityHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Users
        migrationBuilder.AddColumn<int>(name: "ResetPasswordCodeAttempts", table: "Users", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>(name: "ResetPasswordTokenExpiresAt", table: "Users", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "TwoFactorEnabled", table: "Users", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "TwoFactorSecret", table: "Users", maxLength: 512, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TwoFactorRecoveryCodes", table: "Users", maxLength: 1024, nullable: true);
        migrationBuilder.AddColumn<long>(name: "TwoFactorLastUsedStep", table: "Users", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "DeletionRequestedAt", table: "Users", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "DeletedAt", table: "Users", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "TermsAcceptedAt", table: "Users", nullable: true);
        migrationBuilder.AddColumn<string>(name: "TermsVersion", table: "Users", maxLength: 32, nullable: true);

        if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            // The mobile reset code columns were added to the model in 013a880 without a migration;
            // create them only where nobody added them by hand.
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'ResetPasswordCode') IS NULL ALTER TABLE Users ADD ResetPasswordCode nvarchar(max) NULL;
IF COL_LENGTH('Users', 'ResetPasswordCodeExpiresAt') IS NULL ALTER TABLE Users ADD ResetPasswordCodeExpiresAt datetime2 NULL;");

            // Reset codes and tokens are now stored hashed: plain values left from before can never
            // match, so they are cleared rather than left as dead data.
            migrationBuilder.Sql("EXEC('UPDATE Users SET ResetPasswordToken = NULL, ResetPasswordCode = NULL');");
        }

        // DebitCards: keep display metadata, drop the card number, expiry date and CVV.
        migrationBuilder.AddColumn<int>(name: "Brand", table: "DebitCards", nullable: false, defaultValue: 2);
        migrationBuilder.AddColumn<string>(name: "Last4", table: "DebitCards", maxLength: 4, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>(name: "ExpiryMonth", table: "DebitCards", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "ExpiryYear", table: "DebitCards", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "ProviderToken", table: "DebitCards", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Provider", table: "DebitCards", maxLength: 50, nullable: true);

        if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            // Brand values: Mastercard = 0, Visa = 1, Generic = 2.
            migrationBuilder.Sql(@"
UPDATE DebitCards SET
  Last4 = CASE WHEN LEN(REPLACE(REPLACE(CardNumber,' ',''),'-','')) >= 4
               THEN RIGHT(REPLACE(REPLACE(CardNumber,' ',''),'-',''), 4) ELSE '' END,
  Brand = CASE WHEN LEFT(REPLACE(CardNumber,' ',''),1) = '4' THEN 1
               WHEN LEFT(REPLACE(CardNumber,' ',''),2) BETWEEN '51' AND '55'
                 OR LEFT(REPLACE(CardNumber,' ',''),4) BETWEEN '2221' AND '2720' THEN 0
               ELSE 2 END,
  ExpiryMonth = MONTH(ExpirationDate),
  ExpiryYear = YEAR(ExpirationDate);");
        }

        migrationBuilder.DropColumn(name: "CVV", table: "DebitCards");
        migrationBuilder.DropColumn(name: "CardNumber", table: "DebitCards");
        migrationBuilder.DropColumn(name: "ExpirationDate", table: "DebitCards");
        migrationBuilder.AlterColumn<string>(name: "Name", table: "DebitCards", maxLength: 100, nullable: true,
            oldClrType: typeof(string), oldNullable: false);

        // GDPR data export
        migrationBuilder.AddColumn<string>(name: "ExportFileName", table: "UserInfoDownloadRequests", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<string>(name: "DownloadTokenHash", table: "UserInfoDownloadRequests", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "CompletedAt", table: "UserInfoDownloadRequests", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ExpiresAt", table: "UserInfoDownloadRequests", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "DownloadedAt", table: "UserInfoDownloadRequests", nullable: true);

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                OccurredAt = table.Column<DateTime>(nullable: false),
                UserID = table.Column<int>(nullable: true),
                UserEmail = table.Column<string>(maxLength: 256, nullable: true),
                Role = table.Column<string>(maxLength: 64, nullable: true),
                Action = table.Column<string>(maxLength: 64, nullable: false),
                EntityType = table.Column<string>(maxLength: 128, nullable: true),
                EntityID = table.Column<string>(maxLength: 64, nullable: true),
                ChangesJson = table.Column<string>(nullable: true),
                IpAddress = table.Column<string>(maxLength: 64, nullable: true),
                CorrelationID = table.Column<string>(maxLength: 64, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AuditLogs", a => a.ID));

        migrationBuilder.CreateIndex(name: "IX_AuditLogs_OccurredAt", table: "AuditLogs", column: "OccurredAt");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_EntityType_EntityID", table: "AuditLogs", columns: new[] { "EntityType", "EntityID" });
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_UserID", table: "AuditLogs", column: "UserID");

        migrationBuilder.CreateTable(
            name: "AccountDeletionRequests",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserID = table.Column<int>(nullable: false),
                RequestedAt = table.Column<DateTime>(nullable: false),
                ScheduledFor = table.Column<DateTime>(nullable: false),
                Status = table.Column<int>(nullable: false, defaultValue: 0),
                CompletedAt = table.Column<DateTime>(nullable: true),
                CancelledAt = table.Column<DateTime>(nullable: true),
                CancelledByUserID = table.Column<int>(nullable: true),
                WalletBalance = table.Column<double>(nullable: false, defaultValue: 0d),
                RefundRequired = table.Column<bool>(nullable: false, defaultValue: false),
                BlockedCardIDs = table.Column<string>(maxLength: 1024, nullable: true),
                RequestedFromIp = table.Column<string>(maxLength: 64, nullable: true),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountDeletionRequests", d => d.ID);
                table.ForeignKey(
                    name: "FK_AccountDeletionRequests_Users_UserID",
                    column: d => d.UserID,
                    principalTable: "Users",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_AccountDeletionRequests_Status_ScheduledFor", table: "AccountDeletionRequests", columns: new[] { "Status", "ScheduledFor" });
        migrationBuilder.CreateIndex(name: "IX_AccountDeletionRequests_UserID", table: "AccountDeletionRequests", column: "UserID");

        migrationBuilder.CreateTable(
            name: "DataProtectionKeys",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                FriendlyName = table.Column<string>(nullable: true),
                Xml = table.Column<string>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_DataProtectionKeys", k => k.Id));

        // Retention sweeps delete by age.
        migrationBuilder.CreateIndex(name: "IX_MessageLogs_LogTime", table: "MessageLogs", column: "LogTime");
        migrationBuilder.CreateIndex(name: "IX_Notifications_Read_CreatedAt", table: "Notifications", columns: new[] { "Read", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Notifications_Read_CreatedAt", table: "Notifications");
        migrationBuilder.DropIndex(name: "IX_MessageLogs_LogTime", table: "MessageLogs");
        migrationBuilder.DropTable(name: "DataProtectionKeys");
        migrationBuilder.DropTable(name: "AccountDeletionRequests");
        migrationBuilder.DropTable(name: "AuditLogs");

        migrationBuilder.DropColumn(name: "DownloadedAt", table: "UserInfoDownloadRequests");
        migrationBuilder.DropColumn(name: "ExpiresAt", table: "UserInfoDownloadRequests");
        migrationBuilder.DropColumn(name: "CompletedAt", table: "UserInfoDownloadRequests");
        migrationBuilder.DropColumn(name: "DownloadTokenHash", table: "UserInfoDownloadRequests");
        migrationBuilder.DropColumn(name: "ExportFileName", table: "UserInfoDownloadRequests");

        // The card numbers and CVVs are gone for good; the old columns come back empty.
        migrationBuilder.AlterColumn<string>(name: "Name", table: "DebitCards", nullable: false, defaultValue: "",
            oldClrType: typeof(string), oldMaxLength: 100, oldNullable: true);
        migrationBuilder.AddColumn<string>(name: "CardNumber", table: "DebitCards", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "CVV", table: "DebitCards", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>(name: "ExpirationDate", table: "DebitCards", nullable: false,
            defaultValue: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        migrationBuilder.DropColumn(name: "Provider", table: "DebitCards");
        migrationBuilder.DropColumn(name: "ProviderToken", table: "DebitCards");
        migrationBuilder.DropColumn(name: "ExpiryYear", table: "DebitCards");
        migrationBuilder.DropColumn(name: "ExpiryMonth", table: "DebitCards");
        migrationBuilder.DropColumn(name: "Last4", table: "DebitCards");
        migrationBuilder.DropColumn(name: "Brand", table: "DebitCards");

        migrationBuilder.DropColumn(name: "TermsVersion", table: "Users");
        migrationBuilder.DropColumn(name: "TermsAcceptedAt", table: "Users");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Users");
        migrationBuilder.DropColumn(name: "DeletionRequestedAt", table: "Users");
        migrationBuilder.DropColumn(name: "TwoFactorLastUsedStep", table: "Users");
        migrationBuilder.DropColumn(name: "TwoFactorRecoveryCodes", table: "Users");
        migrationBuilder.DropColumn(name: "TwoFactorSecret", table: "Users");
        migrationBuilder.DropColumn(name: "TwoFactorEnabled", table: "Users");
        migrationBuilder.DropColumn(name: "ResetPasswordTokenExpiresAt", table: "Users");
        migrationBuilder.DropColumn(name: "ResetPasswordCodeAttempts", table: "Users");
    }
}
