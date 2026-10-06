using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// OCPP 2.0.1 coverage. Hand written for the same reason as SecurityHardening (the snapshot is
/// MySQL-shaped while the application runs on SQL Server).
///
///  - ChargerEvents: NotifyEvent eventData (alarm feed).
///  - VariableMonitors: monitors reported by NotifyMonitoringReport (replaced per completed report).
///  - CustomerInformationReports: CustomerInformation requests and their assembled NotifyCustomerInformation data.
///  - DisplayMessageSnapshots: NotifyDisplayMessages results per GetDisplayMessages requestId.
///  - LogUploadTickets / UploadedChargerLogs: one-time GetLog upload URLs (hashed token) and the stored files.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261004120000_Ocpp201Coverage")]
public partial class Ocpp201Coverage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ChargerEvents",
            columns: table => new
            {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<string>(maxLength: 64, nullable: false),
                EventId = table.Column<int>(nullable: false),
                Timestamp = table.Column<DateTime>(nullable: false),
                Trigger = table.Column<string>(maxLength: 16, nullable: false),
                ActualValue = table.Column<string>(maxLength: 2500, nullable: true),
                TechCode = table.Column<string>(maxLength: 50, nullable: true),
                TechInfo = table.Column<string>(maxLength: 500, nullable: true),
                Cleared = table.Column<bool>(nullable: true),
                Cause = table.Column<int>(nullable: true),
                TransactionId = table.Column<string>(maxLength: 36, nullable: true),
                ComponentName = table.Column<string>(maxLength: 50, nullable: false),
                ComponentInstance = table.Column<string>(maxLength: 50, nullable: true),
                VariableName = table.Column<string>(maxLength: 50, nullable: false),
                VariableInstance = table.Column<string>(maxLength: 50, nullable: true),
                EvseId = table.Column<int>(nullable: true),
                ConnectorId = table.Column<int>(nullable: true),
                VariableMonitoringId = table.Column<int>(nullable: true),
                EventNotificationType = table.Column<string>(maxLength: 32, nullable: false),
                Severity = table.Column<int>(nullable: true),
                ReceivedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ChargerEvents", e => e.ID));

        migrationBuilder.CreateIndex(name: "IX_ChargerEvents_ChargePointID_Timestamp", table: "ChargerEvents", columns: new[] { "ChargePointID", "Timestamp" });
        migrationBuilder.CreateIndex(name: "IX_ChargerEvents_Timestamp", table: "ChargerEvents", column: "Timestamp");

        migrationBuilder.CreateTable(
            name: "VariableMonitors",
            columns: table => new
            {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<string>(maxLength: 64, nullable: false),
                ComponentName = table.Column<string>(maxLength: 50, nullable: false),
                ComponentInstance = table.Column<string>(maxLength: 50, nullable: true),
                VariableName = table.Column<string>(maxLength: 50, nullable: false),
                VariableInstance = table.Column<string>(maxLength: 50, nullable: true),
                EvseId = table.Column<int>(nullable: true),
                ConnectorId = table.Column<int>(nullable: true),
                MonitoringId = table.Column<int>(nullable: false),
                Type = table.Column<string>(maxLength: 32, nullable: false),
                Value = table.Column<double>(nullable: false),
                Severity = table.Column<int>(nullable: false),
                Transaction = table.Column<bool>(nullable: false),
                RequestId = table.Column<int>(nullable: false),
                ReportedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_VariableMonitors", m => m.ID));

        migrationBuilder.CreateIndex(name: "IX_VariableMonitors_ChargePointID_MonitoringId", table: "VariableMonitors", columns: new[] { "ChargePointID", "MonitoringId" });

        migrationBuilder.CreateTable(
            name: "CustomerInformationReports",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<string>(maxLength: 64, nullable: false),
                RequestId = table.Column<int>(nullable: false),
                Report = table.Column<bool>(nullable: false),
                Clear = table.Column<bool>(nullable: false),
                CustomerIdentifier = table.Column<string>(maxLength: 64, nullable: true),
                IdToken = table.Column<string>(maxLength: 36, nullable: true),
                CommandStatus = table.Column<string>(maxLength: 32, nullable: true),
                RequestedAt = table.Column<DateTime>(nullable: false),
                PartsJson = table.Column<string>(nullable: true),
                PartsReceived = table.Column<int>(nullable: false, defaultValue: 0),
                Data = table.Column<string>(nullable: true),
                Complete = table.Column<bool>(nullable: false, defaultValue: false),
                CompletedAt = table.Column<DateTime>(nullable: true),
                LastPartAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_CustomerInformationReports", r => r.ID));

        migrationBuilder.CreateIndex(name: "IX_CustomerInformationReports_ChargePointID_RequestId", table: "CustomerInformationReports", columns: new[] { "ChargePointID", "RequestId" });

        migrationBuilder.CreateTable(
            name: "DisplayMessageSnapshots",
            columns: table => new
            {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<string>(maxLength: 64, nullable: false),
                RequestId = table.Column<int>(nullable: false),
                MessageId = table.Column<int>(nullable: true),
                Priority = table.Column<string>(maxLength: 16, nullable: true),
                State = table.Column<string>(maxLength: 16, nullable: true),
                StartDateTime = table.Column<DateTime>(nullable: true),
                EndDateTime = table.Column<DateTime>(nullable: true),
                TransactionId = table.Column<string>(maxLength: 36, nullable: true),
                Content = table.Column<string>(maxLength: 1024, nullable: true),
                Format = table.Column<string>(maxLength: 8, nullable: true),
                Language = table.Column<string>(maxLength: 8, nullable: true),
                DisplayComponentName = table.Column<string>(maxLength: 50, nullable: true),
                DisplayComponentInstance = table.Column<string>(maxLength: 50, nullable: true),
                DisplayEvseId = table.Column<int>(nullable: true),
                ReceivedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DisplayMessageSnapshots", m => m.ID));

        migrationBuilder.CreateIndex(name: "IX_DisplayMessageSnapshots_ChargePointID_RequestId", table: "DisplayMessageSnapshots", columns: new[] { "ChargePointID", "RequestId" });

        migrationBuilder.CreateTable(
            name: "LogUploadTickets",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                TokenHash = table.Column<string>(maxLength: 64, nullable: false),
                ChargePointID = table.Column<string>(maxLength: 64, nullable: false),
                RequestId = table.Column<int>(nullable: false),
                Purpose = table.Column<string>(maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                ExpiresAt = table.Column<DateTime>(nullable: false),
                UsedAt = table.Column<DateTime>(nullable: true),
                CreatedByUserID = table.Column<int>(nullable: true),
                AnnouncedFileName = table.Column<string>(maxLength: 255, nullable: true),
                Status = table.Column<string>(maxLength: 32, nullable: true),
                StatusAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_LogUploadTickets", t => t.ID));

        migrationBuilder.CreateIndex(name: "IX_LogUploadTickets_TokenHash", table: "LogUploadTickets", column: "TokenHash", unique: true);
        migrationBuilder.CreateIndex(name: "IX_LogUploadTickets_ChargePointID_RequestId", table: "LogUploadTickets", columns: new[] { "ChargePointID", "RequestId" });

        migrationBuilder.CreateTable(
            name: "UploadedChargerLogs",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                LogUploadTicketID = table.Column<int>(nullable: false),
                ChargePointID = table.Column<string>(maxLength: 64, nullable: false),
                FileName = table.Column<string>(maxLength: 255, nullable: false),
                StoragePath = table.Column<string>(maxLength: 300, nullable: false),
                ContentType = table.Column<string>(maxLength: 100, nullable: true),
                SizeBytes = table.Column<long>(nullable: false),
                Sha256 = table.Column<string>(maxLength: 64, nullable: false),
                UploadedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UploadedChargerLogs", l => l.ID);
                table.ForeignKey(
                    name: "FK_UploadedChargerLogs_LogUploadTickets_LogUploadTicketID",
                    column: l => l.LogUploadTicketID,
                    principalTable: "LogUploadTickets",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_UploadedChargerLogs_ChargePointID_UploadedAt", table: "UploadedChargerLogs", columns: new[] { "ChargePointID", "UploadedAt" });
        migrationBuilder.CreateIndex(name: "IX_UploadedChargerLogs_LogUploadTicketID", table: "UploadedChargerLogs", column: "LogUploadTicketID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UploadedChargerLogs");
        migrationBuilder.DropTable(name: "LogUploadTickets");
        migrationBuilder.DropTable(name: "DisplayMessageSnapshots");
        migrationBuilder.DropTable(name: "CustomerInformationReports");
        migrationBuilder.DropTable(name: "VariableMonitors");
        migrationBuilder.DropTable(name: "ChargerEvents");
    }
}
