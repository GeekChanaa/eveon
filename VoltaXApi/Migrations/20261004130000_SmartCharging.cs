using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Smart charging. Hand written for the same reason as SecurityHardening (the snapshot is MySQL-shaped
/// while the application runs on SQL Server).
///
///  - ChargingProfiles: profiles sent to chargers (dashboard, load balancer, strategies) or reported by them,
///    with the charger's real answer.
///  - EvChargingNeeds: NotifyEVChargingNeeds / NotifyEVChargingSchedule of ISO 15118 EVs.
///  - StationLoadLimits / StationLoadAllocations: per-station load balancing settings and the allocations sent.
///  - ChargingStrategies: reusable profile templates, seeded with three predefined strategies.
/// Enums are stored as int (see Models/SmartCharging/SmartChargingEnums.cs).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261004130000_SmartCharging")]
public partial class SmartCharging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ChargingProfiles",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                EvseId = table.Column<int>(nullable: false),
                OcppProfileId = table.Column<int>(nullable: false),
                StackLevel = table.Column<int>(nullable: false),
                Purpose = table.Column<int>(nullable: false),
                Kind = table.Column<int>(nullable: false),
                RecurrencyKind = table.Column<int>(nullable: true),
                ValidFrom = table.Column<DateTime>(nullable: true),
                ValidTo = table.Column<DateTime>(nullable: true),
                TransactionId = table.Column<string>(maxLength: 36, nullable: true),
                ChargingRateUnit = table.Column<int>(nullable: false),
                StartSchedule = table.Column<DateTime>(nullable: true),
                Duration = table.Column<int>(nullable: true),
                MinChargingRate = table.Column<double>(nullable: true),
                PeriodsJson = table.Column<string>(nullable: false),
                Source = table.Column<int>(nullable: false),
                Status = table.Column<int>(nullable: false),
                LastError = table.Column<string>(maxLength: 512, nullable: true),
                ChargingLimitSource = table.Column<string>(maxLength: 16, nullable: true),
                ChargingStrategyID = table.Column<int>(nullable: true),
                LastSentAt = table.Column<DateTime>(nullable: true),
                CreatedByUserID = table.Column<int>(nullable: true),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChargingProfiles", p => p.ID);
                table.ForeignKey(
                    name: "FK_ChargingProfiles_ChargePoints_ChargePointID",
                    column: p => p.ChargePointID,
                    principalTable: "ChargePoints",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_ChargingProfiles_ChargePointID_EvseId_Status", table: "ChargingProfiles", columns: new[] { "ChargePointID", "EvseId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_ChargingProfiles_ChargePointID_OcppProfileId", table: "ChargingProfiles", columns: new[] { "ChargePointID", "OcppProfileId" });
        migrationBuilder.CreateIndex(name: "IX_ChargingProfiles_Source_Status", table: "ChargingProfiles", columns: new[] { "Source", "Status" });

        migrationBuilder.CreateTable(
            name: "EvChargingNeeds",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                EvseId = table.Column<int>(nullable: false),
                RequestedEnergyTransfer = table.Column<string>(maxLength: 32, nullable: true),
                DepartureTime = table.Column<DateTime>(nullable: true),
                MaxScheduleTuples = table.Column<int>(nullable: true),
                EnergyAmount = table.Column<double>(nullable: true),
                EvMinCurrent = table.Column<double>(nullable: true),
                EvMaxCurrent = table.Column<double>(nullable: true),
                EvMaxVoltage = table.Column<double>(nullable: true),
                EvMaxPower = table.Column<double>(nullable: true),
                StateOfCharge = table.Column<int>(nullable: true),
                EvEnergyCapacity = table.Column<double>(nullable: true),
                FullSoC = table.Column<int>(nullable: true),
                BulkSoC = table.Column<int>(nullable: true),
                EvScheduleTimeBase = table.Column<DateTime>(nullable: true),
                EvScheduleJson = table.Column<string>(nullable: true),
                ReceivedAt = table.Column<DateTime>(nullable: false),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EvChargingNeeds", n => n.ID);
                table.ForeignKey(
                    name: "FK_EvChargingNeeds_ChargePoints_ChargePointID",
                    column: n => n.ChargePointID,
                    principalTable: "ChargePoints",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_EvChargingNeeds_ChargePointID_EvseId_ReceivedAt", table: "EvChargingNeeds", columns: new[] { "ChargePointID", "EvseId", "ReceivedAt" });

        migrationBuilder.CreateTable(
            name: "StationLoadLimits",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargingStationID = table.Column<int>(nullable: false),
                Enabled = table.Column<bool>(nullable: false, defaultValue: false),
                MaxCurrentA = table.Column<double>(nullable: true),
                MaxPowerKW = table.Column<double>(nullable: true),
                Phases = table.Column<int>(nullable: false, defaultValue: 3),
                Voltage = table.Column<double>(nullable: false, defaultValue: 230d),
                MinPerSessionA = table.Column<double>(nullable: false, defaultValue: 6d),
                Strategy = table.Column<int>(nullable: false, defaultValue: 0),
                SafetyMarginPercent = table.Column<double>(nullable: false, defaultValue: 0d),
                LastRebalancedAt = table.Column<DateTime>(nullable: true),
                UpdatedByUserID = table.Column<int>(nullable: true),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StationLoadLimits", l => l.ID);
                table.ForeignKey(
                    name: "FK_StationLoadLimits_ChargingStations_ChargingStationID",
                    column: l => l.ChargingStationID,
                    principalTable: "ChargingStations",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_StationLoadLimits_ChargingStationID", table: "StationLoadLimits", column: "ChargingStationID", unique: true);

        migrationBuilder.CreateTable(
            name: "StationLoadAllocations",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargingStationID = table.Column<int>(nullable: false),
                TransactionID = table.Column<int>(nullable: false),
                ChargePointID = table.Column<int>(nullable: false),
                EvseId = table.Column<int>(nullable: false),
                ConnectorId = table.Column<int>(nullable: true),
                AllocatedA = table.Column<double>(nullable: false),
                AllocatedKW = table.Column<double>(nullable: false),
                StationLimitA = table.Column<double>(nullable: false),
                Queued = table.Column<bool>(nullable: false),
                ChargingProfileID = table.Column<int>(nullable: true),
                SendStatus = table.Column<string>(maxLength: 32, nullable: false),
                Reason = table.Column<string>(maxLength: 512, nullable: true),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_StationLoadAllocations", a => a.ID));

        migrationBuilder.CreateIndex(name: "IX_StationLoadAllocations_ChargingStationID_CreatedAt", table: "StationLoadAllocations", columns: new[] { "ChargingStationID", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_StationLoadAllocations_TransactionID_CreatedAt", table: "StationLoadAllocations", columns: new[] { "TransactionID", "CreatedAt" });

        migrationBuilder.CreateTable(
            name: "ChargingStrategies",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                Purpose = table.Column<int>(nullable: false),
                Kind = table.Column<int>(nullable: false),
                RecurrencyKind = table.Column<int>(nullable: true),
                ChargingRateUnit = table.Column<int>(nullable: false),
                StackLevel = table.Column<int>(nullable: false, defaultValue: 0),
                PeriodsJson = table.Column<string>(nullable: false),
                IsPredefined = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedByUserID = table.Column<int>(nullable: true),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ChargingStrategies", s => s.ID));

        // Purpose 1 = TxDefaultProfile; Kind 0 = Absolute, 1 = Recurring; RecurrencyKind 0 = Daily; ChargingRateUnit 0 = A.
        // startSeconds are business-local seconds since midnight (Daily) or since the strategy is applied (Absolute).
        // Plain SQL (no model in a hand-written migration to type the InsertData columns); valid on SQL Server and MySQL.
        migrationBuilder.Sql(@"
INSERT INTO ChargingStrategies (Name, Description, Purpose, Kind, RecurrencyKind, ChargingRateUnit, StackLevel, PeriodsJson, IsPredefined, IsDeleted, CreatedAt, UpdatedAt) VALUES
('Off-peak night boost', 'Full power at night (22:00-06:00), limited to 10 A during the day (06:00-22:00).', 1, 1, 0, 0, 0,
 '[{""startSeconds"":0,""limit"":32},{""startSeconds"":21600,""limit"":10},{""startSeconds"":79200,""limit"":32}]', 1, 0, '2026-10-04 13:00:00', '2026-10-04 13:00:00'),
('Solar midday', 'Full power while solar production peaks (10:00-16:00), limited to 10 A otherwise.', 1, 1, 0, 0, 0,
 '[{""startSeconds"":0,""limit"":10},{""startSeconds"":36000,""limit"":32},{""startSeconds"":57600,""limit"":10}]', 1, 0, '2026-10-04 13:00:00', '2026-10-04 13:00:00'),
('Fair share 16A', 'Constant 16 A per charger.', 1, 0, NULL, 0, 0,
 '[{""startSeconds"":0,""limit"":16}]', 1, 0, '2026-10-04 13:00:00', '2026-10-04 13:00:00');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ChargingStrategies");
        migrationBuilder.DropTable(name: "StationLoadAllocations");
        migrationBuilder.DropTable(name: "StationLoadLimits");
        migrationBuilder.DropTable(name: "EvChargingNeeds");
        migrationBuilder.DropTable(name: "ChargingProfiles");
    }
}
