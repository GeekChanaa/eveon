using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Migrations;

namespace VoltaXApi.Data;

public static class RefreshTokenSchema
{
    public const string MigrationId = "20260912100000_AddRefreshTokens";

    // This repair intentionally applies only the additive refresh-token migration.
    // The historical migrations and snapshot use MySQL, while this database uses SQL Server.
    public static async Task Initialize(VoltaXApiDbContext db)
    {
        if (!db.Database.IsSqlServer())
            throw new InvalidOperationException("The refresh-token schema repair requires SQL Server.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = 'VoltaX.RefreshTokenSchema',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
            IF @result < 0 THROW 51000, 'Could not lock the refresh-token schema.', 1;
            """);

        var exists = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]')")
            .SingleAsync();
        if (exists == 0)
        {
            var migration = new AddRefreshTokens { ActiveProvider = db.Database.ProviderName! };
            var generator = db.GetService<IMigrationsSqlGenerator>();
            foreach (var command in generator.Generate(migration.UpOperations))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);

            var history = db.GetService<IHistoryRepository>();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            var applied = await history.GetAppliedMigrationsAsync();
            if (!applied.Any(m => m.MigrationId == MigrationId))
                await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(MigrationId, "9.0.0")));
        }
        await transaction.CommitAsync();
        await Verify(db);
    }

    public static async Task Verify(VoltaXApiDbContext db)
    {
        try
        {
            // Select every mapped column, even on an empty table, to validate the schema.
            await db.RefreshTokens.AsNoTracking().OrderBy(t => t.ID).Take(1).ToListAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 208 or 207)
        {
            throw new InvalidOperationException(
                "RefreshTokens is missing or incompatible. Run the API with --initialize-refresh-tokens " +
                "against this database before starting it. See docs/refresh-tokens.md.", ex);
        }
    }
}
