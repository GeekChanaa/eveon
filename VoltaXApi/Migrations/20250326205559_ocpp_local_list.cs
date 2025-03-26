using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class ocpp_local_list : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OCPPLocalListVersions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ChargePointID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPLocalListVersions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPLocalListVersions_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OCPPLocalListItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenType = table.Column<int>(type: "int", nullable: false),
                    TokenStatus = table.Column<int>(type: "int", nullable: false),
                    OCPPLocalListVersionID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPLocalListItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPLocalListItems_OCPPLocalListVersions_OCPPLocalListVersionID",
                        column: x => x.OCPPLocalListVersionID,
                        principalTable: "OCPPLocalListVersions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OCPPLocalListItems_OCPPLocalListVersionID",
                table: "OCPPLocalListItems",
                column: "OCPPLocalListVersionID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPLocalListVersions_ChargePointID",
                table: "OCPPLocalListVersions",
                column: "ChargePointID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OCPPLocalListItems");

            migrationBuilder.DropTable(
                name: "OCPPLocalListVersions");
        }
    }
}
