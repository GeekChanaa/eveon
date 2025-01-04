using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class additiontwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPerKwh",
                table: "Connectors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_AssignedID",
                table: "SystemReports",
                column: "AssignedID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_CardID",
                table: "SystemReports",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_ResolvedByID",
                table: "SystemReports",
                column: "ResolvedByID");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemReports_Cards_CardID",
                table: "SystemReports",
                column: "CardID",
                principalTable: "Cards",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemReports_Users_AssignedID",
                table: "SystemReports",
                column: "AssignedID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemReports_Users_ResolvedByID",
                table: "SystemReports",
                column: "ResolvedByID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemReports_Cards_CardID",
                table: "SystemReports");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemReports_Users_AssignedID",
                table: "SystemReports");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemReports_Users_ResolvedByID",
                table: "SystemReports");

            migrationBuilder.DropIndex(
                name: "IX_SystemReports_AssignedID",
                table: "SystemReports");

            migrationBuilder.DropIndex(
                name: "IX_SystemReports_CardID",
                table: "SystemReports");

            migrationBuilder.DropIndex(
                name: "IX_SystemReports_ResolvedByID",
                table: "SystemReports");

            migrationBuilder.DropColumn(
                name: "CostPerKwh",
                table: "Connectors");
        }
    }
}
