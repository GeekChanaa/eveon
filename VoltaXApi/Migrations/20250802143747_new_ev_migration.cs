using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class new_ev_migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ElectricVehicleModels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ElectricVehicleModels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ElectricVehicleModels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ElectricVehicleModels");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ElectricVehicleModels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ElectricVehicleModels");
        }
    }
}
