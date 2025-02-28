using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class changedpartnerimagetoentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageID",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_ImageID",
                table: "Partners",
                column: "ImageID");

            migrationBuilder.AddForeignKey(
                name: "FK_Partners_Images_ImageID",
                table: "Partners",
                column: "ImageID",
                principalTable: "Images",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partners_Images_ImageID",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Partners_ImageID",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ImageID",
                table: "Partners");
        }
    }
}
