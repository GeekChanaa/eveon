using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Billing;
using VoltaXApi.Models;

namespace VoltaX.Tests.PartnerPortal;

/// <summary>Partners read their own monthly statements; every total is the sum of the statement's sessions.</summary>
public class PartnerStatementApiTests
{
    [Fact]
    public async Task A_statements_totals_are_the_sum_of_its_sessions()
    {
        await using var network = await SettlementNetwork.Create();
        var settlement = await network.Generate(network.PartnerA, network.Previous);
        var client = network.Api.Client(network.PartnerUserA);

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/partner-portal/statements/{settlement!.ID}");

        var s = detail.GetProperty("settlement");
        var lines = detail.GetProperty("lines").EnumerateArray().ToList();
        decimal Sum(string field) => lines.Sum(l => l.GetProperty(field).GetDecimal());
        Assert.Equal(lines.Count, s.GetProperty("sessionCount").GetInt32());
        Assert.Equal(Sum("kwh"), s.GetProperty("energyKwh").GetDecimal());
        Assert.Equal(Sum("gross"), s.GetProperty("grossAmount").GetDecimal());
        Assert.Equal(Sum("netExclVat"), s.GetProperty("netExclVat").GetDecimal());
        Assert.Equal(Sum("vat"), s.GetProperty("revenueVat").GetDecimal());
        Assert.Equal(Sum("commissionExclVat"), s.GetProperty("commissionExclVat").GetDecimal());
        Assert.Equal(Sum("commissionVat"), s.GetProperty("commissionVat").GetDecimal());
        Assert.Equal(Sum("netPayout"), s.GetProperty("netPayout").GetDecimal());

        // ... and the lines are the sessions as billed to the drivers.
        var sessionIds = lines.Select(l => l.GetProperty("chargingSessionId").GetInt32()).ToList();
        var sessions = await network.Api.Db(db => db.ChargingSessions.Where(x => sessionIds.Contains(x.ID)).ToListAsync());
        Assert.Equal(sessions.Sum(x => x.TotalAmount), s.GetProperty("grossAmount").GetDecimal());
        Assert.Equal(sessions.Sum(x => Math.Round((decimal)x.ChargedKwhs!.Value, 3)), s.GetProperty("energyKwh").GetDecimal());
        Assert.Equal(new[] { network.S1, network.S2 }.OrderBy(i => i), sessionIds.OrderBy(i => i));
    }

    [Fact]
    public async Task Partners_only_see_their_own_statements()
    {
        await using var network = await SettlementNetwork.Create();
        var mine = await network.Generate(network.PartnerA, network.Previous);
        var theirs = await network.Generate(network.PartnerB, network.Previous);
        var client = network.Api.Client(network.PartnerUserA);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/partner-portal/statements");

        Assert.Equal(1, list.GetProperty("total").GetInt32());
        Assert.Equal(mine!.ID, list.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/partner-portal/statements/{theirs!.ID}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/partner-portal/statements/{theirs.ID}/export?format=csv")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await network.Api.Client(network.Customer).GetAsync("/api/partner-portal/statements")).StatusCode);
    }

    [Fact]
    public async Task Statements_export_as_csv_with_a_total_row_and_as_pdf()
    {
        await using var network = await SettlementNetwork.Create();
        var settlement = await network.Generate(network.PartnerA, network.Previous);
        var client = network.Api.Client(network.PartnerUserA);

        var csv = await client.GetAsync($"/api/partner-portal/statements/{settlement!.ID}/export?format=csv");
        var pdf = await client.GetAsync($"/api/partner-portal/statements/{settlement.ID}/export?format=pdf&language=ar");

        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        var rows = Encoding.UTF8.GetString(await csv.Content.ReadAsByteArrayAsync()).TrimStart('﻿').Trim().Split('\n').Select(r => r.TrimEnd('\r').Split(';')).ToList();
        var body = rows.Skip(1).Where(r => r[0] != "TOTAL").ToList();
        var total = rows.Single(r => r[0] == "TOTAL");
        for (var column = 6; column <= 12; column++)
            Assert.Equal(body.Sum(r => decimal.Parse(r[column], CultureInfo.InvariantCulture)), decimal.Parse(total[column], CultureInfo.InvariantCulture));
        Assert.Equal("43.24", total[12]);
        Assert.Equal("%PDF", Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/partner-portal/statements/{settlement.ID}/export?format=xls")).StatusCode);
    }
}
