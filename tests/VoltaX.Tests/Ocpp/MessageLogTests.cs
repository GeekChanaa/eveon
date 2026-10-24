using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Models;

namespace VoltaX.Tests.Ocpp;

public class MessageLogTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private VoltaXApiDbContext NewContext() => new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task A_message_without_a_result_is_still_logged()
    {
        // The 2.0.1 Heartbeat handler logs no result: the row used to fail on the required Result column.
        await using (var db = NewContext())
        {
            var repository = new MessageLogRepository(db, null!, NullLogger<MessageLogRepository>.Instance);
            var request = new OCPPMessage("2", "hb-1", "Heartbeat", "{}");
            var response = new OCPPMessage("3", "hb-1", "Heartbeat", "{\"currentTime\":\"2026-10-24T12:00:00Z\"}");

            Assert.True(await repository.SaveLogMessage("CP-1", null, "Heartbeat", null!, null!, request, response));
        }

        await using var check = NewContext();
        var log = Assert.Single(await check.MessageLogs.ToListAsync());
        Assert.Equal("CP-1", log.ChargePointId);
        Assert.Equal("Heartbeat", log.Message);
        Assert.Equal("", log.Result);
    }
}
