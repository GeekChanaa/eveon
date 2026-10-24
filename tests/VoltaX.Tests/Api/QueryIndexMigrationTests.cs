using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;
using Xunit.Abstractions;

namespace VoltaX.Tests.Api;

/// <summary>
/// The QueryIndexes migration is written by hand for both providers: its script must generate for SQL Server and
/// MariaDB (no connection needed), with index names MariaDB accepts, and the model snapshot must match the model.
/// </summary>
public class QueryIndexMigrationTests(ITestOutputHelper output)
{
    private const string Previous = "20261023020956_OnlinePayments";
    private const string Migration = "20261026010000_QueryIndexes";

    private static VoltaXApiDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlServer("Server=none;Database=x").Options);

    private static VoltaXApiDbContext MariaDb() =>
        new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseMySql("Server=none;Database=x", new MariaDbServerVersion("10.6.15")).Options);

    public static TheoryData<string> Providers => new() { "SqlServer", "MariaDb" };

    [Theory]
    [MemberData(nameof(Providers))]
    public void The_script_creates_every_index_and_can_be_undone(string provider)
    {
        using var db = provider == "SqlServer" ? SqlServer() : MariaDb();
        var migrator = db.GetService<IMigrator>();

        var up = migrator.GenerateScript(Previous, Migration);
        var down = migrator.GenerateScript(Migration, Previous);
        output.WriteLine(up);

        foreach (var index in new[] { "IX_Cards_CardNumber", "IX_Transactions_Uid", "IX_Transactions_StartTime", "IX_Orders_RechargeDate",
                     "IX_MessageLogs_ChargePointId_LogTime", "IX_ChargingSessions_StartDate", "IX_ChargingSessions_UserID_EndDate",
                     "IX_ChargingSessions_ConnectorID_StartDate" })
        {
            Assert.Contains(index, up);
            Assert.Contains(index, down);
            Assert.True(index.Length <= 64, index);
        }
        Assert.True(up.IndexOf("IX_ChargingSessions_UserID_EndDate", StringComparison.Ordinal)
            < up.IndexOf("DROP INDEX", StringComparison.OrdinalIgnoreCase), "the foreign key indexes are dropped last");
    }

    [Fact]
    public void The_snapshot_matches_the_model()
    {
        using var db = SqlServer();
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
