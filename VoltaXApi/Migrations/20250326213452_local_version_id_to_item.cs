using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class local_version_id_to_item : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OCPPLocalListItems_OCPPLocalListVersions_OCPPLocalListVersionID",
                table: "OCPPLocalListItems");

            migrationBuilder.AlterColumn<int>(
                name: "OCPPLocalListVersionID",
                table: "OCPPLocalListItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OCPPLocalListItems_OCPPLocalListVersions_OCPPLocalListVersionID",
                table: "OCPPLocalListItems",
                column: "OCPPLocalListVersionID",
                principalTable: "OCPPLocalListVersions",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OCPPLocalListItems_OCPPLocalListVersions_OCPPLocalListVersionID",
                table: "OCPPLocalListItems");

            migrationBuilder.AlterColumn<int>(
                name: "OCPPLocalListVersionID",
                table: "OCPPLocalListItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_OCPPLocalListItems_OCPPLocalListVersions_OCPPLocalListVersionID",
                table: "OCPPLocalListItems",
                column: "OCPPLocalListVersionID",
                principalTable: "OCPPLocalListVersions",
                principalColumn: "ID");
        }
    }
}
