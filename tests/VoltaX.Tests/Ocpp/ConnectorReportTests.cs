using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Messages;

namespace VoltaX.Tests.Ocpp;

/// <summary>
/// The connector inventory a 2.0.1 charger reports (NotifyReport, possibly in several parts) and how it replaces the
/// stored connectors of that charge point.
/// </summary>
public class ConnectorReportTests
{
    private static ReportDataType Connector(int evse, int connector) => new()
    {
        Component = new() { Name = "Connector", Evse = new() { Id = evse, ConnectorId = connector } },
        Variable = new() { Name = "AvailabilityState" },
        VariableAttribute = new() { new() { Type = AttributeEnumType.Actual, Value = "Available" } }
    };

    private static NotifyReportRequest Part(int requestId, int sequence, bool more, params ReportDataType[] rows) => new()
    {
        RequestId = requestId, SeqNo = sequence, Tbc = more, ReportData = rows.ToList()
    };

    [Fact]
    public void A_single_part_report_gives_its_connector_addresses()
    {
        using var buffer = new ConnectorReportBuffer();
        Assert.Equal(2, buffer.Add("CP-A", Part(8502492, 0, false, Connector(1, 1), Connector(2, 1)))?.Count);
    }

    [Fact]
    public void A_multipart_report_completes_with_its_last_part_only()
    {
        using var buffer = new ConnectorReportBuffer();

        Assert.Null(buffer.Add("CP-A", Part(7, 0, true, Connector(1, 1))));
        Assert.Null(buffer.Add("CP-B", Part(7, 1, false, Connector(9, 1))));  // other charge point, same request id
        Assert.Null(buffer.Add("CP-A", Part(8, 1, false, Connector(9, 1))));  // same charge point, other request id
        Assert.Equal(2, buffer.Add("CP-A", Part(7, 1, false, Connector(2, 1)))?.Count);
    }

    [Fact]
    public void Missing_parts_keep_the_report_pending_until_they_arrive_in_any_order()
    {
        using var buffer = new ConnectorReportBuffer();

        Assert.Null(buffer.Add("CP-C", Part(9, 2, false, Connector(3, 1))));
        Assert.Null(buffer.Add("CP-C", Part(9, 0, true, Connector(1, 1))));
        Assert.Equal(3, buffer.Add("CP-C", Part(9, 1, true, Connector(2, 1)))?.Count);
    }

    [Fact]
    public void Duplicate_addresses_collapse_and_a_configuration_only_report_has_no_inventory()
    {
        using var buffer = new ConnectorReportBuffer();

        Assert.Equal(1, buffer.Add("CP-D", Part(10, 0, false, Connector(1, 1), Connector(1, 1)))?.Count);
        Assert.Equal(0, buffer.Add("CP-E", Part(11, 0, false))?.Count);
    }

    [Fact]
    public void An_invalid_connector_address_is_rejected_before_reconciliation()
    {
        using var buffer = new ConnectorReportBuffer();
        Assert.Throws<ArgumentException>(() => buffer.Add("CP-A", Part(12, 0, false, Connector(0, 1))));
    }

    [Fact]
    public async Task Reconciliation_keeps_reported_connectors_retires_the_others_and_revives_returning_ones()
    {
        await using var host = new OcppTestHost(new Dictionary<string, string?>
        {
            ["GlobalConfigurations:DefaultFlatFee"] = "3"
        }, relational: true);
        int pointId = 0, otherPointId = 0, retainedId = 0, removedId = 0, otherId = 0;
        await host.Seed(async db =>
        {
            var station = await Seeds.Station(db);
            pointId = (await Seeds.ChargePoint(db, "CP-1", chargingStationId: station.ID)).ID;
            otherPointId = (await Seeds.ChargePoint(db, "CP-2", chargingStationId: station.ID)).ID;
            var retained = new Connector { ChargePointID = pointId, EvseID = 1, ConnectorID = 1, PricePerKWh = 7.5m };
            var removed = new Connector { ChargePointID = pointId, EvseID = 3, ConnectorID = 1 };
            var other = new Connector { ChargePointID = otherPointId, EvseID = 8, ConnectorID = 1 };
            var card = Seeds.Card("TAG-1", 0m);
            db.AddRange(retained, removed, other, card);
            await db.SaveChangesAsync();
            db.ConnectorStatuses.AddRange(
                new ConnectorStatus { ConnectorID = retained.ID, LastStatus = ConnectorStatusEnumType.Available },
                new ConnectorStatus { ConnectorID = removed.ID, LastStatus = ConnectorStatusEnumType.Available },
                new ConnectorStatus { ConnectorID = other.ID, LastStatus = ConnectorStatusEnumType.Available });
            db.ChargingSessions.Add(new ChargingSession { ConnectorID = removed.ID, CardID = card.ID });
            (retainedId, removedId, otherId) = (retained.ID, removed.ID, other.ID);
        });
        Task Reconcile(params (int, int)[] reported) => host.With<IConnectorRepository>(r => r.ReconcileChargePointConnectors(pointId, reported));

        await Reconcile((1, 1), (2, 1));

        var active = await host.Query(db => db.Connectors.Where(c => c.ChargePointID == pointId).ToListAsync());
        Assert.Equal(new[] { (1, 1), (2, 1) }, active.Select(c => (c.EvseID, c.ConnectorID ?? 0)).OrderBy(k => k));
        var kept = active.Single(c => c.EvseID == 1);
        Assert.Equal(retainedId, kept.ID);                       // same row: identity and pricing kept
        Assert.Equal(7.5m, kept.PricePerKWh);
        Assert.Equal(3m, active.Single(c => c.EvseID == 2).FlatFee); // new connectors get the default pricing
        Assert.True(await host.Query(db => db.Connectors.AnyAsync(c => c.ID == otherId)));
        Assert.False(await host.Query(db => db.ConnectorStatuses.AnyAsync(s => s.ConnectorID == removedId)));
        Assert.True(await host.Query(db => db.Connectors.IgnoreQueryFilters().AnyAsync(c => c.ID == removedId && c.IsDeleted)));
        Assert.True(await host.Query(db => db.ChargingSessions.AnyAsync(s => s.ConnectorID == removedId)));

        // Replaying the same report creates nothing (the address index is unique even for deleted rows).
        await Reconcile((1, 1), (2, 1));
        Assert.Equal(4, await host.Query(db => db.Connectors.IgnoreQueryFilters().CountAsync()));

        // A connector that comes back reuses its row and its status reappears.
        await Reconcile((3, 1));
        Assert.Equal(removedId, (await host.Query(db => db.Connectors.SingleAsync(c => c.ChargePointID == pointId))).ID);
        Assert.True(await host.Query(db => db.ConnectorStatuses.AnyAsync(s => s.ConnectorID == removedId)));
    }
}
