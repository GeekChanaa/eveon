using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class components_variables_updated4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "required",
                table: "OcppVariableComponents",
                newName: "Required");

            migrationBuilder.RenameColumn(
                name: "Isntance",
                table: "OcppVariableComponents",
                newName: "Instance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Required",
                table: "OcppVariableComponents",
                newName: "required");

            migrationBuilder.RenameColumn(
                name: "Instance",
                table: "OcppVariableComponents",
                newName: "Isntance");
        }
    }
}
