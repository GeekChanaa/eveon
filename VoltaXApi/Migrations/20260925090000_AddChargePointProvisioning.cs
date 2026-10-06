using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// First-connection provisioning: the default OCPP 2.0.1 settings profile and the per charge point
/// provisioning record. Hand written for the same reason as AddCardChangeHistory (the snapshot is
/// MySQL-shaped while the application runs on SQL Server).
///
/// Every charge point that already exists gets a "Legacy" record, so only charge points created
/// after this migration are held in Pending on their first BootNotification. The default profile
/// itself is seeded at startup by OcppDefaultProfileSeeder (provider-neutral, no column types).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20260925090000_AddChargePointProvisioning")]
public partial class AddChargePointProvisioning : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OcppDefaultVariables",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                GroupName = table.Column<string>(maxLength: 50, nullable: false),
                ComponentName = table.Column<string>(maxLength: 50, nullable: false),
                ComponentInstance = table.Column<string>(maxLength: 50, nullable: true),
                EvseId = table.Column<int>(nullable: true),
                ConnectorId = table.Column<int>(nullable: true),
                VariableName = table.Column<string>(maxLength: 50, nullable: false),
                VariableInstance = table.Column<string>(maxLength: 50, nullable: true),
                AttributeType = table.Column<int>(nullable: false, defaultValue: 0),
                Value = table.Column<string>(maxLength: 1000, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                Enabled = table.Column<bool>(nullable: false, defaultValue: true),
                SortOrder = table.Column<int>(nullable: false, defaultValue: 0),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OcppDefaultVariables", v => v.ID);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OcppDefaultVariables_GroupName_SortOrder",
            table: "OcppDefaultVariables",
            columns: new[] { "GroupName", "SortOrder" });

        migrationBuilder.CreateTable(
            name: "ChargePointProvisionings",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                Status = table.Column<int>(nullable: false),
                Method = table.Column<int>(nullable: false),
                ProvisionedAt = table.Column<DateTime>(nullable: false),
                ProvisionedByUserID = table.Column<int>(nullable: true),
                AcceptedCount = table.Column<int>(nullable: false, defaultValue: 0),
                FailedCount = table.Column<int>(nullable: false, defaultValue: 0),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChargePointProvisionings", p => p.ID);
                table.ForeignKey(
                    name: "FK_ChargePointProvisionings_ChargePoints_ChargePointID",
                    column: p => p.ChargePointID,
                    principalTable: "ChargePoints",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChargePointProvisionings_Users_ProvisionedByUserID",
                    column: p => p.ProvisionedByUserID,
                    principalTable: "Users",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ChargePointProvisionings_ChargePointID",
            table: "ChargePointProvisionings",
            column: "ChargePointID",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChargePointProvisionings_ProvisionedByUserID",
            table: "ChargePointProvisionings",
            column: "ProvisionedByUserID");

        // Existing charge points keep being accepted as before (Status 0 = Provisioned, Method 3 = Legacy).
        migrationBuilder.Sql(@"
            INSERT INTO ChargePointProvisionings
                (ChargePointID, Status, Method, ProvisionedAt, AcceptedCount, FailedCount, IsDeleted, CreatedAt, UpdatedAt)
            SELECT ID, 0, 3, CURRENT_TIMESTAMP, 0, 0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            FROM ChargePoints");

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ChargePointProvisionings");
        migrationBuilder.DropTable(name: "OcppDefaultVariables");
    }
}
