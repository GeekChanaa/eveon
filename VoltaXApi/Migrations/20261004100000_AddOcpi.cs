using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// OCPI 2.2.1 roaming (CPO role). Hand written for the same reason as SecurityHardening (the
/// snapshot is MySQL-shaped while the application runs on SQL Server).
///
///  - OcpiParties: registered eMSPs / hubs, hashed incoming token, Data Protection encrypted outgoing token.
///  - OcpiTokens: tokens pushed by eMSPs (Tokens receiver), used by the Authorize hook.
///  - OcpiSessions: sessions started with roaming tokens (Sessions and CDRs modules).
///  - OcpiReservations: OCPI reservation id to the integer id sent to the charger.
///  - OcpiOutbox: persisted outgoing calls (pushes, CDRs, command results) with retry state.
///  - OcpiSyncCursors: change-scan watermarks of the push worker.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261004100000_AddOcpi")]
public partial class AddOcpi : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OcpiParties",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                CountryCode = table.Column<string>(maxLength: 2, nullable: true),
                PartyId = table.Column<string>(maxLength: 3, nullable: true),
                Role = table.Column<string>(maxLength: 16, nullable: true),
                RolesJson = table.Column<string>(nullable: true),
                Status = table.Column<int>(nullable: false, defaultValue: 0),
                VersionsUrl = table.Column<string>(maxLength: 512, nullable: true),
                Version = table.Column<string>(maxLength: 10, nullable: true),
                EndpointsJson = table.Column<string>(nullable: true),
                TokenAHash = table.Column<string>(maxLength: 64, nullable: true),
                IncomingTokenHash = table.Column<string>(maxLength: 64, nullable: true),
                OutgoingTokenProtected = table.Column<string>(nullable: true),
                LastError = table.Column<string>(maxLength: 1024, nullable: true),
                RegisteredAt = table.Column<DateTime>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OcpiParties", p => p.ID));

        migrationBuilder.CreateIndex(name: "IX_OcpiParties_IncomingTokenHash", table: "OcpiParties", column: "IncomingTokenHash");
        migrationBuilder.CreateIndex(name: "IX_OcpiParties_TokenAHash", table: "OcpiParties", column: "TokenAHash");
        migrationBuilder.CreateIndex(name: "IX_OcpiParties_CountryCode_PartyId", table: "OcpiParties", columns: new[] { "CountryCode", "PartyId" });

        migrationBuilder.CreateTable(
            name: "OcpiTokens",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                OcpiPartyID = table.Column<int>(nullable: false),
                CountryCode = table.Column<string>(maxLength: 2, nullable: false),
                PartyId = table.Column<string>(maxLength: 3, nullable: false),
                Uid = table.Column<string>(maxLength: 36, nullable: false),
                Type = table.Column<string>(maxLength: 16, nullable: false),
                ContractId = table.Column<string>(maxLength: 36, nullable: false),
                VisualNumber = table.Column<string>(maxLength: 64, nullable: true),
                Issuer = table.Column<string>(maxLength: 64, nullable: false),
                GroupId = table.Column<string>(maxLength: 36, nullable: true),
                Valid = table.Column<bool>(nullable: false),
                Whitelist = table.Column<string>(maxLength: 16, nullable: false),
                Language = table.Column<string>(maxLength: 2, nullable: true),
                DefaultProfileType = table.Column<string>(maxLength: 16, nullable: true),
                LastUpdated = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OcpiTokens", t => t.ID);
                table.ForeignKey(
                    name: "FK_OcpiTokens_OcpiParties_OcpiPartyID",
                    column: t => t.OcpiPartyID,
                    principalTable: "OcpiParties",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_OcpiTokens_OcpiPartyID_CountryCode_PartyId_Uid_Type", table: "OcpiTokens",
            columns: new[] { "OcpiPartyID", "CountryCode", "PartyId", "Uid", "Type" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_OcpiTokens_Uid", table: "OcpiTokens", column: "Uid");

        migrationBuilder.CreateTable(
            name: "OcpiSessions",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                OcpiPartyID = table.Column<int>(nullable: false),
                TokenCountryCode = table.Column<string>(maxLength: 2, nullable: false),
                TokenPartyId = table.Column<string>(maxLength: 3, nullable: false),
                TokenUid = table.Column<string>(maxLength: 36, nullable: false),
                TokenType = table.Column<string>(maxLength: 16, nullable: false),
                ContractId = table.Column<string>(maxLength: 36, nullable: false),
                AuthMethod = table.Column<string>(maxLength: 16, nullable: false),
                AuthorizationReference = table.Column<string>(maxLength: 36, nullable: true),
                ChargingStationID = table.Column<int>(nullable: false),
                ChargePointID = table.Column<int>(nullable: false),
                EvseId = table.Column<int>(nullable: false),
                ConnectorID = table.Column<int>(nullable: true),
                TransactionUid = table.Column<string>(maxLength: 36, nullable: true),
                Status = table.Column<int>(nullable: false),
                StartDateTime = table.Column<DateTime>(nullable: false),
                EndDateTime = table.Column<DateTime>(nullable: true),
                MeterStartKwh = table.Column<double>(nullable: true),
                Kwh = table.Column<double>(nullable: false, defaultValue: 0d),
                PricePerKWh = table.Column<double>(nullable: false, defaultValue: 0d),
                PricePerMinute = table.Column<double>(nullable: false, defaultValue: 0d),
                PricePerIdleMinute = table.Column<double>(nullable: false, defaultValue: 0d),
                FlatFee = table.Column<double>(nullable: false, defaultValue: 0d),
                VatRate = table.Column<double>(nullable: false, defaultValue: 0d),
                CdrQueuedAt = table.Column<DateTime>(nullable: true),
                LastUpdated = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OcpiSessions", s => s.ID);
                table.ForeignKey(
                    name: "FK_OcpiSessions_OcpiParties_OcpiPartyID",
                    column: s => s.OcpiPartyID,
                    principalTable: "OcpiParties",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_OcpiSessions_OcpiPartyID_LastUpdated", table: "OcpiSessions", columns: new[] { "OcpiPartyID", "LastUpdated" });
        migrationBuilder.CreateIndex(name: "IX_OcpiSessions_ChargePointID_TransactionUid", table: "OcpiSessions", columns: new[] { "ChargePointID", "TransactionUid" });

        migrationBuilder.CreateTable(
            name: "OcpiReservations",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                OcpiPartyID = table.Column<int>(nullable: false),
                ReservationId = table.Column<string>(maxLength: 36, nullable: false),
                TokenUid = table.Column<string>(maxLength: 36, nullable: false),
                ChargePointID = table.Column<int>(nullable: false),
                EvseId = table.Column<int>(nullable: true),
                ExpiryDate = table.Column<DateTime>(nullable: false),
                Cancelled = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OcpiReservations", r => r.ID));

        migrationBuilder.CreateIndex(name: "IX_OcpiReservations_OcpiPartyID_ReservationId", table: "OcpiReservations", columns: new[] { "OcpiPartyID", "ReservationId" });

        migrationBuilder.CreateTable(
            name: "OcpiOutbox",
            columns: table => new
            {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                OcpiPartyID = table.Column<int>(nullable: false),
                Module = table.Column<string>(maxLength: 16, nullable: false),
                Method = table.Column<string>(maxLength: 8, nullable: false),
                Url = table.Column<string>(maxLength: 1024, nullable: false),
                PayloadJson = table.Column<string>(nullable: true),
                Status = table.Column<int>(nullable: false, defaultValue: 0),
                Attempts = table.Column<int>(nullable: false, defaultValue: 0),
                NextAttemptAt = table.Column<DateTime>(nullable: false),
                LastError = table.Column<string>(maxLength: 1024, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                SentAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OcpiOutbox", m => m.ID);
                table.ForeignKey(
                    name: "FK_OcpiOutbox_OcpiParties_OcpiPartyID",
                    column: m => m.OcpiPartyID,
                    principalTable: "OcpiParties",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_OcpiOutbox_Status_NextAttemptAt", table: "OcpiOutbox", columns: new[] { "Status", "NextAttemptAt" });
        migrationBuilder.CreateIndex(name: "IX_OcpiOutbox_OcpiPartyID", table: "OcpiOutbox", column: "OcpiPartyID");

        migrationBuilder.CreateTable(
            name: "OcpiSyncCursors",
            columns: table => new
            {
                Name = table.Column<string>(maxLength: 64, nullable: false),
                Position = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OcpiSyncCursors", c => c.Name));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OcpiSyncCursors");
        migrationBuilder.DropTable(name: "OcpiOutbox");
        migrationBuilder.DropTable(name: "OcpiReservations");
        migrationBuilder.DropTable(name: "OcpiSessions");
        migrationBuilder.DropTable(name: "OcpiTokens");
        migrationBuilder.DropTable(name: "OcpiParties");
    }
}
