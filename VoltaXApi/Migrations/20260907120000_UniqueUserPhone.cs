using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <summary>
    /// Makes Users.Phone a real identifier: every row stored in the same
    /// "+212XXXXXXXXX" shape, and no two accounts sharing a number.
    ///
    /// Written by hand for the same reason as the previous migration: the snapshot was
    /// generated against MySQL while the application currently runs on SQL Server, so
    /// scaffolding would produce a full provider switch diff. Everything below applies
    /// on both providers.
    ///
    /// Existing rows are repaired before the index goes on, in three steps:
    ///   1. every number is rewritten with the country code,
    ///   2. numbers too long for the new column are archived and cleared,
    ///   3. duplicates are archived and cleared, keeping the verified owner (and, when
    ///      neither is verified, the account that registered first).
    /// Anything cleared is copied to UserPhoneConflicts first, so nothing is lost and
    /// Down() can put it back.
    /// </summary>
    [DbContext(typeof(VoltaXApiDbContext))]
    [Migration("20260907120000_UniqueUserPhone")]
    public partial class UniqueUserPhone : Migration
    {
        private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

        private bool IsSqlServer(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == SqlServerProvider;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            bool sqlServer = IsSqlServer(migrationBuilder);

            // Concatenation is the one piece of syntax the two providers disagree on.
            string Concat(string left, string right)
                => sqlServer ? $"({left} + {right})" : $"CONCAT({left}, {right})";

            CreateConflictTable(migrationBuilder, sqlServer);

            // ---- 1. one shape for every stored number -------------------------------

            // Separators first, so the prefix tests below see plain digits.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET Phone = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    Phone, ' ', ''), '-', ''), '.', ''), '(', ''), ')', '')
                WHERE Phone IS NOT NULL");

            migrationBuilder.Sql("UPDATE Users SET Phone = NULL WHERE Phone = ''");

            // Each statement leaves the number starting with '+', so a row rewritten here
            // can no longer match the statements that follow.
            migrationBuilder.Sql($@"
                UPDATE Users SET Phone = {Concat("'+212'", "SUBSTRING(Phone, 6, 20)")}
                WHERE Phone LIKE '00212%'");

            migrationBuilder.Sql($@"
                UPDATE Users SET Phone = {Concat("'+212'", "SUBSTRING(Phone, 4, 20)")}
                WHERE Phone LIKE '212%'");

            migrationBuilder.Sql($@"
                UPDATE Users SET Phone = {Concat("'+212'", "SUBSTRING(Phone, 2, 20)")}
                WHERE Phone LIKE '0%'");

            // Whatever is left is a bare national number ("610610614").
            migrationBuilder.Sql($@"
                UPDATE Users SET Phone = {Concat("'+212'", "Phone")}
                WHERE Phone IS NOT NULL AND Phone NOT LIKE '+%'");

            // ---- 2. numbers that cannot fit the new column --------------------------

            ArchiveAndClear(
                migrationBuilder,
                sqlServer,
                reason: "TooLong",
                whereClause: "Phone IS NOT NULL AND LENGTH_FN(Phone) > 20");

            // ---- 3. duplicates ------------------------------------------------------

            // The verified owner keeps the number; failing that, the oldest account does.
            ArchiveAndClear(
                migrationBuilder,
                sqlServer,
                reason: "Duplicate",
                whereClause: @"ID IN (
                    SELECT ID FROM (
                        SELECT ID, ROW_NUMBER() OVER (
                            PARTITION BY Phone
                            ORDER BY IsPhoneNumberVerified DESC, ID ASC) AS RowNo
                        FROM Users
                        WHERE Phone IS NOT NULL
                    ) Ranked
                    WHERE Ranked.RowNo > 1)");

            // ---- 4. the constraint itself -------------------------------------------

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            // SQL Server treats NULLs as duplicates in a unique index, so it needs a
            // filter. MySQL / MariaDB allow several NULLs already and reject filters.
            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: sqlServer ? "[Phone] IS NOT NULL" : null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            bool sqlServer = IsSqlServer(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_Users_Phone",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 20,
                oldNullable: true);

            // Hand the archived numbers back to the accounts they were taken from. Only
            // rows still without a number are touched, so a number set in the meantime
            // wins over the archive.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET Phone = (
                    SELECT MAX(c.Phone) FROM UserPhoneConflicts c WHERE c.UserID = Users.ID)
                WHERE Phone IS NULL
                  AND EXISTS (
                    SELECT 1 FROM UserPhoneConflicts c WHERE c.UserID = Users.ID)");

            migrationBuilder.Sql("DROP TABLE UserPhoneConflicts");
        }

        /// <summary>
        /// Holding pen for the numbers this migration has to take off an account. Kept as
        /// a plain table (it is not part of the EF model) so support can look up what
        /// happened to a user's number after the fact.
        /// </summary>
        private static void CreateConflictTable(MigrationBuilder migrationBuilder, bool sqlServer)
        {
            migrationBuilder.Sql(sqlServer
                ? @"CREATE TABLE UserPhoneConflicts (
                        UserID     INT           NOT NULL,
                        Phone      NVARCHAR(64)  NOT NULL,
                        Reason     NVARCHAR(32)  NOT NULL,
                        ArchivedAt DATETIME2     NOT NULL)"
                : @"CREATE TABLE UserPhoneConflicts (
                        UserID     INT          NOT NULL,
                        Phone      VARCHAR(64)  NOT NULL,
                        Reason     VARCHAR(32)  NOT NULL,
                        ArchivedAt DATETIME(6)  NOT NULL)");
        }

        /// <summary>
        /// Copies the numbers matched by <paramref name="whereClause"/> into
        /// UserPhoneConflicts, then clears them. The placeholder LENGTH_FN stands for the
        /// provider's string length function.
        /// </summary>
        private static void ArchiveAndClear(
            MigrationBuilder migrationBuilder,
            bool sqlServer,
            string reason,
            string whereClause)
        {
            string where = whereClause.Replace("LENGTH_FN", sqlServer ? "LEN" : "CHAR_LENGTH");
            string now = sqlServer ? "SYSUTCDATETIME()" : "UTC_TIMESTAMP(6)";

            migrationBuilder.Sql($@"
                INSERT INTO UserPhoneConflicts (UserID, Phone, Reason, ArchivedAt)
                SELECT ID, Phone, '{reason}', {now}
                FROM Users
                WHERE {where}");

            migrationBuilder.Sql($@"
                UPDATE Users SET Phone = NULL
                WHERE {where}");
        }
    }
}
