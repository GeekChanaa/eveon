using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Charge point alarms: one row per fault occurrence with its severity, scope (charge point, EVSE, connector), first and
/// last time seen, count, and the operator's acknowledgement, assignment and resolution. OpenKey is unique: it equals
/// DedupKey while the alarm is active, so the same fault cannot be raised twice at once. The ViewAlarms / EditAlarms
/// permissions are added by the permission seeder at startup. Written by hand: column types are left to the provider
/// (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261025000100_Alarms")]
public partial class Alarms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Alarms",
            columns: table => new
            {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Type = table.Column<string>(maxLength: 32, nullable: false),
                Code = table.Column<string>(maxLength: 100, nullable: false),
                Severity = table.Column<int>(nullable: false),
                Status = table.Column<string>(maxLength: 16, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: false),
                Details = table.Column<string>(maxLength: 1000, nullable: true),
                ChargePointID = table.Column<int>(nullable: false),
                ChargePointIdentity = table.Column<string>(maxLength: 64, nullable: false),
                EvseId = table.Column<int>(nullable: true),
                ConnectorNumber = table.Column<int>(nullable: true),
                ConnectorID = table.Column<int>(nullable: true),
                FirstSeenAt = table.Column<DateTime>(nullable: false),
                LastSeenAt = table.Column<DateTime>(nullable: false),
                Count = table.Column<int>(nullable: false),
                AcknowledgedAt = table.Column<DateTime>(nullable: true),
                AcknowledgedByID = table.Column<int>(nullable: true),
                AssignedAt = table.Column<DateTime>(nullable: true),
                AssignedToID = table.Column<int>(nullable: true),
                ResolvedAt = table.Column<DateTime>(nullable: true),
                ResolvedByID = table.Column<int>(nullable: true),
                ResolutionComment = table.Column<string>(maxLength: 1000, nullable: true),
                ClearedAt = table.Column<DateTime>(nullable: true),
                DedupKey = table.Column<string>(maxLength: 200, nullable: false),
                OpenKey = table.Column<string>(maxLength: 230, nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Alarms", a => a.ID);
                table.ForeignKey(name: "FK_Alarms_ChargePoints_ChargePointID", column: a => a.ChargePointID,
                    principalTable: "ChargePoints", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Alarms_Users_AcknowledgedByID", column: a => a.AcknowledgedByID,
                    principalTable: "Users", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Alarms_Users_AssignedToID", column: a => a.AssignedToID,
                    principalTable: "Users", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Alarms_Users_ResolvedByID", column: a => a.ResolvedByID,
                    principalTable: "Users", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Alarms_OpenKey", table: "Alarms", column: "OpenKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Alarms_Status_LastSeenAt", table: "Alarms", columns: new[] { "Status", "LastSeenAt" });
        migrationBuilder.CreateIndex(name: "IX_Alarms_ChargePointID_Status", table: "Alarms", columns: new[] { "ChargePointID", "Status" });
        migrationBuilder.CreateIndex(name: "IX_Alarms_AssignedToID", table: "Alarms", column: "AssignedToID");
        migrationBuilder.CreateIndex(name: "IX_Alarms_AcknowledgedByID", table: "Alarms", column: "AcknowledgedByID");
        migrationBuilder.CreateIndex(name: "IX_Alarms_ResolvedByID", table: "Alarms", column: "ResolvedByID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Alarms");
    }
}
