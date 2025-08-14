using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class charged_idle_minutes_chargingsession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ChargedMinutes",
                table: "ChargingSessions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IdleMinutes",
                table: "ChargingSessions",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargedMinutes",
                table: "ChargingSessions");

            migrationBuilder.DropColumn(
                name: "IdleMinutes",
                table: "ChargingSessions");
        }
    }
}
