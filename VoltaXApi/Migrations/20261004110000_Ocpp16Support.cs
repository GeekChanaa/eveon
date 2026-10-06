using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// OCPP 1.6J support and reservations. Hand written for the same reason as SecurityHardening (the
/// snapshot is MySQL-shaped while the application runs on SQL Server).
///
///  - Reservations: ReserveNow sent to 1.6 / 2.0.1 chargers and their lifecycle (Active/Used/Cancelled/Expired/Rejected).
///  - Ocpp16Transactions: integer transactionId issued to 1.6 chargers; Transactions.Uid is "ocpp16-{Id}".
///  - OcppPendingTransactions: 2.0.1 Started events without EVSE, waiting for the event that carries it.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261004110000_Ocpp16Support")]
public partial class Ocpp16Support : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Reservations",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ReservationId = table.Column<int>(nullable: false),
                ChargePointID = table.Column<int>(nullable: false),
                ConnectorID = table.Column<int>(nullable: true),
                EvseId = table.Column<int>(nullable: true),
                IdToken = table.Column<string>(maxLength: 36, nullable: false),
                UserID = table.Column<int>(nullable: true),
                ExpiresAt = table.Column<DateTime>(nullable: false),
                Status = table.Column<int>(nullable: false, defaultValue: 0),
                TransactionUid = table.Column<string>(maxLength: 36, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reservations", r => r.ID);
                table.ForeignKey("FK_Reservations_ChargePoints_ChargePointID", r => r.ChargePointID, "ChargePoints", "ID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Reservations_Connectors_ConnectorID", r => r.ConnectorID, "Connectors", "ID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Reservations_ChargePointID_ReservationId", table: "Reservations", columns: new[] { "ChargePointID", "ReservationId" });
        migrationBuilder.CreateIndex(name: "IX_Reservations_Status_ExpiresAt", table: "Reservations", columns: new[] { "Status", "ExpiresAt" });
        migrationBuilder.CreateIndex(name: "IX_Reservations_ConnectorID", table: "Reservations", column: "ConnectorID");

        migrationBuilder.CreateTable(
            name: "Ocpp16Transactions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                ConnectorId = table.Column<int>(nullable: false),
                IdTag = table.Column<string>(maxLength: 36, nullable: true),
                ReservationId = table.Column<int>(nullable: true),
                MeterStartWh = table.Column<int>(nullable: false),
                StartTimestamp = table.Column<DateTime>(nullable: false),
                MeterStopWh = table.Column<int>(nullable: true),
                StopTimestamp = table.Column<DateTime>(nullable: true),
                StopReason = table.Column<string>(maxLength: 40, nullable: true),
                AuthorizationStatus = table.Column<string>(maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Ocpp16Transactions", t => t.Id));

        migrationBuilder.CreateIndex(name: "IX_Ocpp16Transactions_ChargePointID_StopTimestamp", table: "Ocpp16Transactions", columns: new[] { "ChargePointID", "StopTimestamp" });

        migrationBuilder.CreateTable(
            name: "OcppPendingTransactions",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                TransactionUid = table.Column<string>(maxLength: 36, nullable: false),
                IdTag = table.Column<string>(maxLength: 36, nullable: true),
                Timestamp = table.Column<string>(maxLength: 40, nullable: true),
                MeterStartKWh = table.Column<double>(nullable: true),
                TriggerReason = table.Column<string>(maxLength: 40, nullable: false),
                ReservationId = table.Column<int>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OcppPendingTransactions", p => p.ID));

        migrationBuilder.CreateIndex(name: "IX_OcppPendingTransactions_ChargePointID_TransactionUid", table: "OcppPendingTransactions",
            columns: new[] { "ChargePointID", "TransactionUid" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OcppPendingTransactions");
        migrationBuilder.DropTable(name: "Ocpp16Transactions");
        migrationBuilder.DropTable(name: "Reservations");
    }
}
