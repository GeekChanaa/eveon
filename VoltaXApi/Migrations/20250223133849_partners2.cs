using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class partners2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingStations_Partner_PartnerID",
                table: "ChargingStations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Partner_PartnerID",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Partner",
                table: "Partner");

            migrationBuilder.RenameTable(
                name: "Partner",
                newName: "Partners");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Partners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Partners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Partners",
                table: "Partners",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingStations_Partners_PartnerID",
                table: "ChargingStations",
                column: "PartnerID",
                principalTable: "Partners",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Partners_PartnerID",
                table: "Users",
                column: "PartnerID",
                principalTable: "Partners",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingStations_Partners_PartnerID",
                table: "ChargingStations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Partners_PartnerID",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Partners",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Partners");

            migrationBuilder.RenameTable(
                name: "Partners",
                newName: "Partner");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Partner",
                table: "Partner",
                column: "ID");

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
    }
}
