using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// PhoneLoginChallenges (one pending SMS sign-in code per phone number) is in the model, but until now
/// no migration created it: the API created it at start up with raw SQL (Data/PhoneLoginSchema.cs, in
/// Development or with Database:InitializePhoneLogin=true). That initializer is gone; this migration
/// takes over. It is idempotent because databases where the initializer ran already have the table,
/// with exactly these columns and index.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261027000000_AddPhoneLoginChallenges")]
public partial class AddPhoneLoginChallenges : Migration
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
    private const string MySqlProvider = "Pomelo.EntityFrameworkCore.MySql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider == SqlServerProvider)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'PhoneLoginChallenges', N'U') IS NULL
                BEGIN
                    CREATE TABLE PhoneLoginChallenges (
                        Phone nvarchar(16) NOT NULL,
                        ChallengeId nvarchar(32) NOT NULL,
                        Digest nvarchar(64) NOT NULL,
                        Salt nvarchar(64) NOT NULL,
                        IpAddress nvarchar(64) NOT NULL,
                        ExpiresAt datetime2 NOT NULL,
                        ResendAt datetime2 NOT NULL,
                        WindowStart datetime2 NOT NULL,
                        Sends int NOT NULL,
                        Attempts int NOT NULL,
                        Consumed bit NOT NULL,
                        CONSTRAINT PK_PhoneLoginChallenges PRIMARY KEY (Phone)
                    );
                    CREATE INDEX IX_PhoneLoginChallenges_IpAddress ON PhoneLoginChallenges (IpAddress, WindowStart);
                END
                """);
        }
        else if (migrationBuilder.ActiveProvider == MySqlProvider)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS `PhoneLoginChallenges` (
                    `Phone` varchar(16) CHARACTER SET utf8mb4 NOT NULL,
                    `ChallengeId` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
                    `Digest` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
                    `Salt` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
                    `IpAddress` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
                    `ExpiresAt` datetime(6) NOT NULL,
                    `ResendAt` datetime(6) NOT NULL,
                    `WindowStart` datetime(6) NOT NULL,
                    `Sends` int NOT NULL,
                    `Attempts` int NOT NULL,
                    `Consumed` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_PhoneLoginChallenges` PRIMARY KEY (`Phone`),
                    INDEX `IX_PhoneLoginChallenges_IpAddress` (`IpAddress`, `WindowStart`)
                ) CHARACTER SET=utf8mb4;
                """);
        }
        else
        {
            // Any other provider (e.g. SQLite in tests) only ever starts from an empty database.
            migrationBuilder.CreateTable(
                name: "PhoneLoginChallenges",
                columns: table => new
                {
                    Phone = table.Column<string>(maxLength: 16, nullable: false),
                    ChallengeId = table.Column<string>(maxLength: 32, nullable: false),
                    Digest = table.Column<string>(maxLength: 64, nullable: false),
                    Salt = table.Column<string>(maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    ResendAt = table.Column<DateTime>(nullable: false),
                    WindowStart = table.Column<DateTime>(nullable: false),
                    Sends = table.Column<int>(nullable: false),
                    Attempts = table.Column<int>(nullable: false),
                    Consumed = table.Column<bool>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PhoneLoginChallenges", x => x.Phone));
            migrationBuilder.CreateIndex(
                name: "IX_PhoneLoginChallenges_IpAddress",
                table: "PhoneLoginChallenges",
                columns: new[] { "IpAddress", "WindowStart" });
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PhoneLoginChallenges");
    }
}
