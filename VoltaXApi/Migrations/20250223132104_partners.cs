using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class partners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingStations_Users_PartnerID",
                table: "ChargingStations");

            migrationBuilder.AddColumn<int>(
                name: "PartnerID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Partner",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxIdentificationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partner", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PartnerID",
                table: "Users",
                column: "PartnerID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingStations_Partner_PartnerID",
                table: "ChargingStations",
                column: "PartnerID",
                principalTable: "Partner",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Partner_PartnerID",
                table: "Users",
                column: "PartnerID",
                principalTable: "Partner",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingStations_Partner_PartnerID",
                table: "ChargingStations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Partner_PartnerID",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Partner");

            migrationBuilder.DropIndex(
                name: "IX_Users_PartnerID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PartnerID",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingStations_Users_PartnerID",
                table: "ChargingStations",
                column: "PartnerID",
                principalTable: "Users",
                principalColumn: "ID");
        }
    }
}
