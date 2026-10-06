using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data;
// Additive SQL Server bootstrap; older migration snapshots target MySQL.
public static class PhoneLoginSchema
{
    public static Task Initialize(VoltaXApiDbContext db) => db.Database.ExecuteSqlRawAsync("""
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
        """);
}
