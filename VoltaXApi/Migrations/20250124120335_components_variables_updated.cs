using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class components_variables_updated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComponentID",
                table: "OcppVariableComponents");

            migrationBuilder.DropColumn(
                name: "VariableID",
                table: "OcppVariableComponents");

            migrationBuilder.AddColumn<string>(
                name: "Component",
                table: "OcppVariableComponents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DataType",
                table: "OcppVariableComponents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Isntance",
                table: "OcppVariableComponents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "OcppVariableComponents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Variable",
                table: "OcppVariableComponents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "required",
                table: "OcppVariableComponents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Component",
                table: "OcppVariableComponents");

            migrationBuilder.DropColumn(
                name: "DataType",
                table: "OcppVariableComponents");

            migrationBuilder.DropColumn(
                name: "Isntance",
                table: "OcppVariableComponents");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "OcppVariableComponents");

            migrationBuilder.DropColumn(
                name: "Variable",
                table: "OcppVariableComponents");

            migrationBuilder.DropColumn(
                name: "required",
                table: "OcppVariableComponents");

            migrationBuilder.AddColumn<int>(
                name: "ComponentID",
                table: "OcppVariableComponents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VariableID",
                table: "OcppVariableComponents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
