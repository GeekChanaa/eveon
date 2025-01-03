using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class additions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemReports",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    CardID = table.Column<int>(type: "int", nullable: true),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    ChargePointID = table.Column<int>(type: "int", nullable: true),
                    ResolvedByID = table.Column<int>(type: "int", nullable: true),
                    AssignedID = table.Column<int>(type: "int", nullable: true),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEmail = table.Column<bool>(type: "bit", nullable: false),
                    IsNotification = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Criticality = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReports_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SystemReports_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SystemReports_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSessions_CardID",
                table: "ChargingSessions",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_ChargePointID",
                table: "SystemReports",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_ConnectorID",
                table: "SystemReports",
                column: "ConnectorID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_UserID",
                table: "SystemReports",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingSessions_Cards_CardID",
                table: "ChargingSessions",
                column: "CardID",
                principalTable: "Cards",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingSessions_Cards_CardID",
                table: "ChargingSessions");

            migrationBuilder.DropTable(
                name: "SystemReports");

            migrationBuilder.DropIndex(
                name: "IX_ChargingSessions_CardID",
                table: "ChargingSessions");
        }
    }
}
