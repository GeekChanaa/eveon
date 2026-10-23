using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;

namespace VoltaX.Tests.Ocpp;

public class HarnessSmokeTests
{
    [Fact]
    public async Task Boot_answers_by_provisioning_state()
    {
        await using var host = new OcppTestHost();
        await host.Seed(db => Seeds.ChargePoint(db, "CP1"));
        await host.Seed(db => { db.ChargePoints.Add(new VoltaXApi.Models.ChargePoint { ChargePointId = "CP-PENDING" }); return Task.CompletedTask; });

        var accepted = await new Charger16(host, "CP1").Boot();
        var pending = await new Charger16(host, "CP-PENDING").Boot();
        var unknown = await new Charger16(host, "NOPE").Boot();

        Assert.Equal("Accepted", accepted.GetProperty("status").GetString());
        Assert.Equal("Pending", pending.GetProperty("status").GetString());
        Assert.Equal("Rejected", unknown.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Authorize_checks_the_card()
    {
        await using var host = new OcppTestHost();
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, "CP1");
            db.Cards.AddRange(Seeds.Card("OK", 50), Seeds.Card("BLOCKED", 50, blocked: true));
        });
        var charger = new Charger16(host, "CP1");
        await charger.Boot();

        Assert.Equal("Accepted", await charger.Authorize("OK"));
        Assert.Equal("Blocked", await charger.Authorize("BLOCKED"));
        Assert.Equal("Invalid", await charger.Authorize("UNKNOWN"));
        Assert.True(await host.Query(db => db.MessageLogs.AnyAsync()));
    }
}
