using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class ocpp_configurations_migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OCPPConfigurationEVSEs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EVSEId = table.Column<int>(type: "int", nullable: false),
                    ConnectorId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationEVSEs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationVariableCharacteristics",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: true),
                    MinLimit = table.Column<double>(type: "float", nullable: true),
                    MaxLimit = table.Column<double>(type: "float", nullable: true),
                    ValuesList = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportsMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationVariableCharacteristics", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationVariables",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationVariables", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationComponents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OCPPConfigurationEVSEID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationComponents", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationComponents_OCPPConfigurationEVSEs_OCPPConfigurationEVSEID",
                        column: x => x.OCPPConfigurationEVSEID,
                        principalTable: "OCPPConfigurationEVSEs",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointID = table.Column<int>(type: "int", nullable: false),
                    OCPPConfigurationComponentID = table.Column<int>(type: "int", nullable: false),
                    OCPPConfigurationVariableID = table.Column<int>(type: "int", nullable: false),
                    OCPPConfigurationVariableCharacteristicID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_OCPPConfigurationComponents_OCPPConfigurationComponentID",
                        column: x => x.OCPPConfigurationComponentID,
                        principalTable: "OCPPConfigurationComponents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_OCPPConfigurationVariableCharacteristics_OCPPConfigurationVariableCharacteristicID",
                        column: x => x.OCPPConfigurationVariableCharacteristicID,
                        principalTable: "OCPPConfigurationVariableCharacteristics",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_OCPPConfigurationVariables_OCPPConfigurationVariableID",
                        column: x => x.OCPPConfigurationVariableID,
                        principalTable: "OCPPConfigurationVariables",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationVariableAttributes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mutability = table.Column<int>(type: "int", nullable: false),
                    Persistent = table.Column<bool>(type: "bit", nullable: false),
                    Constant = table.Column<bool>(type: "bit", nullable: false),
                    OCPPConfigurationItemID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationVariableAttributes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationVariableAttributes_OCPPConfigurationItems_OCPPConfigurationItemID",
                        column: x => x.OCPPConfigurationItemID,
                        principalTable: "OCPPConfigurationItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationComponents_OCPPConfigurationEVSEID",
                table: "OCPPConfigurationComponents",
                column: "OCPPConfigurationEVSEID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_ChargePointID",
                table: "OCPPConfigurationItems",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_OCPPConfigurationComponentID",
                table: "OCPPConfigurationItems",
                column: "OCPPConfigurationComponentID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_OCPPConfigurationVariableCharacteristicID",
                table: "OCPPConfigurationItems",
                column: "OCPPConfigurationVariableCharacteristicID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_OCPPConfigurationVariableID",
                table: "OCPPConfigurationItems",
                column: "OCPPConfigurationVariableID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationVariableAttributes_OCPPConfigurationItemID",
                table: "OCPPConfigurationVariableAttributes",
                column: "OCPPConfigurationItemID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OCPPConfigurationVariableAttributes");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationItems");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationComponents");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationVariableCharacteristics");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationVariables");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationEVSEs");
        }
    }
}
