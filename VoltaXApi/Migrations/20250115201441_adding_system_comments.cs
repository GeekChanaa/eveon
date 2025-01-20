using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class adding_system_comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemReportComments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemReportID = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReportComments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReportComments_SystemReports_SystemReportID",
                        column: x => x.SystemReportID,
                        principalTable: "SystemReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemReportCommentImage",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageID = table.Column<int>(type: "int", nullable: false),
                    SystemReportCommentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReportCommentImage", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReportCommentImage_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemReportCommentImage_SystemReportComments_SystemReportCommentID",
                        column: x => x.SystemReportCommentID,
                        principalTable: "SystemReportComments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportCommentImage_ImageID",
                table: "SystemReportCommentImage",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportCommentImage_SystemReportCommentID",
                table: "SystemReportCommentImage",
                column: "SystemReportCommentID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportComments_SystemReportID",
                table: "SystemReportComments",
                column: "SystemReportID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemReportCommentImage");

            migrationBuilder.DropTable(
                name: "SystemReportComments");
        }
    }
}
