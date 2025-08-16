using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class add_idle_price_charging_session : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PricePerIdleMinute",
                table: "ChargingSessions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PricePerMinute",
                table: "ChargingSessions",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerIdleMinute",
                table: "ChargingSessions");

            migrationBuilder.DropColumn(
                name: "PricePerMinute",
                table: "ChargingSessions");
        }
    }
}
