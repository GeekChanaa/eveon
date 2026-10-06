using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Messages;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
ReportDataType ConnectorReport(int evse, int connector) => new()
{
    Component = new() { Name = "Connector", Evse = new() { Id = evse, ConnectorId = connector } },
    Variable = new() { Name = "AvailabilityState" },
    VariableAttribute = new() { new() { Type = AttributeEnumType.Actual, Value = "Available" } }
};
NotifyReportRequest Part(int id, int sequence, bool more, params ReportDataType[] rows) => new()
{
    RequestId = id, SeqNo = sequence, Tbc = more, ReportData = rows.ToList()
};
using var buffer = new ConnectorReportBuffer();
var report = buffer.Add("CP-A", Part(8502492, 0, false, ConnectorReport(1, 1), ConnectorReport(2, 1)));
Check(report?.Count == 2, "example report produces exactly two connector addresses");
buffer.Complete("CP-A", 8502492);
Check(buffer.Add("CP-A", Part(7, 0, true, ConnectorReport(1, 1))) == null, "multipart report does not replace connectors early");
Check(buffer.Add("CP-B", Part(7, 1, false, ConnectorReport(9, 1))) == null, "reports from different charge points stay isolated");
Check(buffer.Add("CP-A", Part(8, 1, false, ConnectorReport(9, 1))) == null, "different request IDs stay isolated");
report = buffer.Add("CP-A", Part(7, 1, false, ConnectorReport(2, 1)));
Check(report?.Count == 2, "completed multipart report includes earlier connectors");
Check(buffer.Add("CP-C", Part(9, 2, false, ConnectorReport(3, 1))) == null, "missing report chunks do not trigger replacement");
Check(buffer.Add("CP-C", Part(9, 0, true, ConnectorReport(1, 1))) == null, "sequence gap remains pending");
Check(buffer.Add("CP-C", Part(9, 1, true, ConnectorReport(2, 1)))?.Count == 3, "out-of-order chunks assemble when all arrive");
Check(buffer.Add("CP-D", Part(10, 0, false, ConnectorReport(1, 1), ConnectorReport(1, 1)))?.Count == 1, "duplicate addresses collapse");
Check(buffer.Add("CP-E", Part(11, 0, false))?.Count == 0, "configuration-only report has no connector inventory");
try { buffer.Add("CP-A", Part(12, 0, false, ConnectorReport(0, 1))); throw new Exception("Invalid address accepted"); }
catch (ArgumentException) { Check(true, "invalid connector addresses are rejected before reconciliation"); }

// Use a private in-memory database; no application database is touched.
var options = new DbContextOptionsBuilder<VoltaXApiDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
await using var db = new VoltaXApiDbContext(options);
var retained = new Connector { ChargePointID = 1, EvseID = 1, ConnectorID = 1, PricePerKWh = 7.5 };
var removed = new Connector { ChargePointID = 1, EvseID = 3, ConnectorID = 1 };
var other = new Connector { ChargePointID = 2, EvseID = 8, ConnectorID = 1 };
db.Connectors.AddRange(retained, removed, other);
await db.SaveChangesAsync();
db.ConnectorStatuses.AddRange(
    new ConnectorStatus { ConnectorID = retained.ID, LastStatus = ConnectorStatusEnumType.Available },
    new ConnectorStatus { ConnectorID = removed.ID, LastStatus = ConnectorStatusEnumType.Available },
    new ConnectorStatus { ConnectorID = other.ID, LastStatus = ConnectorStatusEnumType.Available });
db.ChargingSessions.Add(new ChargingSession { ConnectorID = removed.ID });
await db.SaveChangesAsync();
var config = new GlobalConfigurations { DefaultPricePerKwh = 2, DefaultFlatFee = 3 };
var repository = new ConnectorRepository(db, config, null!);
await repository.ReconcileChargePointConnectors(1, new[] { (1, 1), (2, 1) });
var active = await db.Connectors.Where(c => c.ChargePointID == 1).ToListAsync();
Check(active.Count == 2 && active.All(c => c.ConnectorID == 1 && (c.EvseID == 1 || c.EvseID == 2)), "only reported connectors remain active");
Check(active.Single(c => c.EvseID == 1).ID == retained.ID && retained.PricePerKWh == 7.5, "matching connector keeps identity and pricing");
Check(active.Single(c => c.EvseID == 2).FlatFee == 3, "new connectors receive default pricing");
Check(await db.Connectors.AnyAsync(c => c.ID == other.ID), "other charge points remain unchanged");
Check(!await db.ConnectorStatuses.AnyAsync(s => s.ConnectorID == removed.ID), "removed connector statuses disappear from counts");
Check(await db.Connectors.IgnoreQueryFilters().AnyAsync(c => c.ID == removed.ID && c.IsDeleted), "removed connector record is retained through soft deletion");
Check(await db.ChargingSessions.AnyAsync(s => s.ConnectorID == removed.ID), "historical charging session record is retained");
await repository.ReconcileChargePointConnectors(1, new[] { (1, 1), (2, 1) });
Check(await db.Connectors.IgnoreQueryFilters().CountAsync() == 4, "replaying a report creates no duplicates");
await repository.ReconcileChargePointConnectors(1, new[] { (3, 1) });
Check((await db.Connectors.SingleAsync(c => c.ChargePointID == 1)).ID == removed.ID, "returning connector reuses its existing row");
Check(await db.ConnectorStatuses.AnyAsync(s => s.ConnectorID == removed.ID), "returning connector status is reactivated");
Console.WriteLine($"Passed {checks} connector report checks.");
