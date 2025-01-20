using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class addinguseridtosystemreportcommetn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "SystemReportComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportComments_UserID",
                table: "SystemReportComments",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemReportComments_Users_UserID",
                table: "SystemReportComments",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemReportComments_Users_UserID",
                table: "SystemReportComments");

            migrationBuilder.DropIndex(
                name: "IX_SystemReportComments_UserID",
                table: "SystemReportComments");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "SystemReportComments");
        }
    }
}
