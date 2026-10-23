using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Public information partners edit from their portal: a description for drivers and the weekly opening hours
/// (JSON, null = unknown / always open). Both nullable, so existing stations are unchanged. Written by hand:
/// column types are left to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261024100000_StationPublicInfo")]
public partial class StationPublicInfo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Description", table: "ChargingStations", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "OpeningHours", table: "ChargingStations", maxLength: 2000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Description", table: "ChargingStations");
        migrationBuilder.DropColumn(name: "OpeningHours", table: "ChargingStations");
    }
}
