using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class converting_all_double : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "States",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "States",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Countries",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Countries",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PricePerMinute",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "PricePerKWh",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "PricePerIdleMinute",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Power",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "MaxPower",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "FlatFee",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "CostPerKwh",
                table: "Connectors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Cities",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Cities",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "States",
                type: "double(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "States",
                type: "double(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Countries",
                type: "double(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Countries",
                type: "double(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PricePerMinute",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "PricePerKWh",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "PricePerIdleMinute",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Power",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "MaxPower",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "FlatFee",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CostPerKwh",
                table: "Connectors",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Cities",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Cities",
                type: "double(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
