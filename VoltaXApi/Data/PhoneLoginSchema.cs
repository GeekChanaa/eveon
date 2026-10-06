using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data;
// Additive bootstrap: no migration creates this table, so it is created here (SQL Server or MySQL / MariaDB).
public static class PhoneLoginSchema
{
    public static Task Initialize(VoltaXApiDbContext db) => db.Database.IsSqlServer()
        ? db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.PhoneLoginChallenges', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PhoneLoginChallenges (
                    Phone nvarchar(16) NOT NULL PRIMARY KEY,
                    ChallengeId nvarchar(32) NOT NULL,
                    Digest nvarchar(64) NOT NULL,
                    Salt nvarchar(64) NOT NULL,
                    IpAddress nvarchar(64) NOT NULL,
                    ExpiresAt datetime2 NOT NULL,
                    ResendAt datetime2 NOT NULL,
                    WindowStart datetime2 NOT NULL,
                    Sends int NOT NULL,
                    Attempts int NOT NULL,
                    Consumed bit NOT NULL
                );
                CREATE INDEX IX_PhoneLoginChallenges_IpAddress ON dbo.PhoneLoginChallenges(IpAddress, WindowStart);
            END
            """)
        : db.Database.ExecuteSqlRawAsync("""
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
