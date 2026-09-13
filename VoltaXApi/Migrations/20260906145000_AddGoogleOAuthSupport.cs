using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <summary>
    /// Adds Google / OAuth sign in support on the Users table.
    ///
    /// Written by hand on purpose: scaffolding it produced a full provider switch diff,
    /// because the previous migration was generated against MySQL while the application
    /// now runs on SQL Server. This one only touches the columns the feature needs and
    /// applies cleanly on both providers.
    /// </summary>
    [DbContext(typeof(VoltaXApiDbContext))]
    [Migration("20260906145000_AddGoogleOAuthSupport")]
    public partial class AddGoogleOAuthSupport : Migration
    {
        private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Accounts created through Google never get a local password.
            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordHash",
                table: "Users",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldNullable: false);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordSalt",
                table: "Users",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldNullable: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Users",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Users",
                maxLength: 32,
                nullable: false,
                defaultValue: "Local");

            migrationBuilder.AddColumn<string>(
                name: "ExternalPictureUrl",
                table: "Users",
                maxLength: 512,
                nullable: true);

            // SQL Server treats NULLs as duplicates in a unique index, so it needs a filter.
            // MySQL / MariaDB allow several NULLs already and reject filtered indexes.
            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true,
                filter: migrationBuilder.ActiveProvider == SqlServerProvider
                    ? "[GoogleId] IS NOT NULL"
                    : null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_GoogleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalPictureUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Users");

            // Rows without a password (Google only accounts) must go before the column
            // can be made required again.
            migrationBuilder.Sql("DELETE FROM Users WHERE PasswordHash IS NULL");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordSalt",
                table: "Users",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordHash",
                table: "Users",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldNullable: true);
        }
    }
}
