using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <summary>
    /// Adds the RefreshTokens table, which turns a login into a rotating session instead
    /// of a single long lived JWT.
    ///
    /// Hand written for the same reason as the two migrations before it: the snapshot was
    /// generated against MySQL while the application runs on SQL Server, so scaffolding
    /// would produce a full provider switch diff. Everything below applies on both.
    /// </summary>
    [DbContext(typeof(VoltaXApiDbContext))]
    [Migration("20260912100000_AddRefreshTokens")]
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TokenHash = table.Column<string>(maxLength: 128, nullable: false),
                    UserID = table.Column<int>(nullable: false),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    RevokedAt = table.Column<DateTime>(nullable: true),
                    ReplacedByTokenHash = table.Column<string>(maxLength: 128, nullable: true),
                    RevokedReason = table.Column<string>(maxLength: 64, nullable: true),
                    CreatedByIp = table.Column<string>(maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            // The hash is the lookup key of every refresh call, and two rows can never
            // share one.
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            // Revoking a whole account scans by user.
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserID",
                table: "RefreshTokens",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RefreshTokens");
        }

    }
}
