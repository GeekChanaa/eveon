using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace VoltaXApi.Data;

/// <summary>
/// The schema changes through EF Core migrations only, on SQL Server and MySQL / MariaDB alike
/// (docs/database.md). Nothing creates tables at start up any more.
///  - <c>--migrate</c>: apply the pending migrations, check that every mapped table and column exists,
///    exit. This is the deploy step (deploy.sh runs it in a one-off container before the swap).
///  - <c>Database:MigrateOnStartup=true</c>: the same, before the API starts serving. For single
///    instance hosts without a pre-deploy hook.
///  - Otherwise a start with pending migrations fails when <c>Database:FailOnPendingMigrations</c> is
///    true (the default in Production) and logs a warning elsewhere, instead of failing later on the
///    first request that touches a missing column.
/// </summary>
public static class DatabaseMigrations
{
    public const string Flag = "--migrate";

    public static async Task MigrateAsync(VoltaXApiDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database schema up to date: no migration to apply");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        // Converting the columns of a large table can take longer than the 30 second default.
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Applied {Count} migration(s)", pending.Count);
    }

    /// <summary>
    /// Selects every mapped column of every table (no rows), so a database that drifted from the model
    /// fails here, with the list of what is missing, rather than on a request.
    /// </summary>
    public static async Task VerifySchemaAsync(VoltaXApiDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var sql = db.GetService<ISqlGenerationHelper>();
        var tables = db.GetService<IDesignTimeModel>().Model.GetRelationalModel().Tables
            .Where(t => !t.IsExcludedFromMigrations)
            .ToList();
        var problems = new List<string>();
        foreach (var table in tables)
        {
            var columns = string.Join(", ", table.Columns.Select(c => sql.DelimitIdentifier(c.Name)));
            var query = "SELECT " + columns + " FROM " + sql.DelimitIdentifier(table.Name, table.Schema) + " WHERE 1 = 0";
            try
            {
                await db.Database.ExecuteSqlRawAsync(query, cancellationToken);
            }
            catch (DbException ex)
            {
                problems.Add($"{table.Name}: {ex.Message}");
            }
        }

        if (problems.Count > 0)
            throw new InvalidOperationException(
                "The database schema does not match the model. Apply the migrations (--migrate, see docs/database.md):"
                + Environment.NewLine + string.Join(Environment.NewLine, problems));
        logger.LogInformation("Database schema matches the model ({Count} tables)", tables.Count);
    }

    public static async Task CheckNothingPendingAsync(VoltaXApiDbContext db, bool failOnPending, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
            return;
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
            return;

        const string message = "{Count} migration(s) not applied to this database: {Migrations}. "
            + "Run the API once with --migrate (or set Database:MigrateOnStartup=true), see docs/database.md";
        if (!failOnPending)
        {
            logger.LogWarning(message, pending.Count, string.Join(", ", pending));
            return;
        }
        logger.LogCritical(message, pending.Count, string.Join(", ", pending));
        throw new InvalidOperationException(
            $"{pending.Count} migration(s) not applied to this database. Run the API with --migrate first (docs/database.md).");
    }
}
