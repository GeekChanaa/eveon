using Newtonsoft.Json;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaX.Tests.Ocpp;

/// <summary>
/// OCPP 2.0.1 device data a charger reports: events (NotifyEvent), monitors (NotifyMonitoringReport), customer
/// information and display messages, including their multi-part (tbc / seqNo) assembly.
/// </summary>
public class DeviceDataMappingTests
{
    private static readonly DateTime Received = new(2026, 10, 3, 8, 1, 0, DateTimeKind.Utc);

    private static List<VoltaXApi.Models.Ocpp201.ChargerEvent> TwoPartEvents()
    {
        var part0 = JsonConvert.DeserializeObject<NotifyEventRequest>("""
            { "generatedAt": "2026-10-03T10:00:00+02:00", "tbc": true, "seqNo": 0,
              "eventData": [
                { "eventId": 1, "timestamp": "2026-10-03T09:59:00+02:00", "trigger": "Alerting", "actualValue": "71",
                  "eventNotificationType": "CustomMonitor", "component": { "name": "EVSE", "evse": { "id": 1, "connectorId": 2 } },
                  "variable": { "name": "Temperature" }, "variableMonitoringId": 7, "techCode": "T-01" },
                { "eventId": 2, "timestamp": "2026-10-03T08:00:00Z", "trigger": "Delta", "actualValue": "true",
                  "eventNotificationType": "HardWiredNotification", "component": { "name": "ChargingStation" }, "variable": { "name": "Problem" } } ] }
            """)!;
        var part1 = JsonConvert.DeserializeObject<NotifyEventRequest>("""
            { "generatedAt": "2026-10-03T08:00:05Z", "tbc": false, "seqNo": 1,
              "eventData": [
                { "eventId": 3, "timestamp": "2026-10-03T08:00:04Z", "trigger": "Delta", "actualValue": "false", "cleared": true,
                  "eventNotificationType": "HardWiredNotification", "component": { "name": "ChargingStation" }, "variable": { "name": "Problem" } },
                { "eventId": 4, "timestamp": "2026-10-03T08:00:04Z", "trigger": "Periodic", "actualValue": "230.1",
                  "eventNotificationType": "PreconfiguredMonitor", "component": { "name": "EVSE", "evse": { "id": 1 } },
                  "variable": { "name": "Voltage" }, "variableMonitoringId": 8 } ] }
            """)!;
        var severities = new Dictionary<int, int> { [7] = 2, [8] = 8 };
        return ChargerEventMapper.Map("CP-1", part0, Received, severities).Concat(ChargerEventMapper.Map("CP-1", part1, Received, severities)).ToList();
    }

    [Fact]
    public void Every_event_of_every_NotifyEvent_part_is_mapped()
    {
        var events = TwoPartEvents();

        Assert.Equal(new[] { 1, 2, 3, 4 }, events.Select(e => e.EventId));
        var first = events[0];
        Assert.Equal(new DateTime(2026, 10, 3, 7, 59, 0, DateTimeKind.Utc), first.Timestamp); // offset converted to UTC
        Assert.Equal(DateTimeKind.Utc, first.Timestamp.Kind);
        Assert.Equal((1, 2), (first.EvseId, first.ConnectorId));
        Assert.Equal(("EVSE", "Temperature"), (first.ComponentName, first.VariableName));
        Assert.Equal(("Alerting", "CustomMonitor", "T-01"), (first.Trigger, first.EventNotificationType, first.TechCode));
    }

    [Fact]
    public void Event_severity_comes_from_the_reporting_monitor_and_decides_the_alarm()
    {
        var events = TwoPartEvents();

        Assert.Equal(2, events[0].Severity);
        Assert.Null(events[1].Severity);
        Assert.Equal(8, events[3].Severity);
        Assert.True(ChargerEventMapper.IsAlarm(events[0]));  // monitor severity 2 (<= 3)
        Assert.True(ChargerEventMapper.IsAlarm(events[1]));  // hard-wired Problem = true
        Assert.False(ChargerEventMapper.IsAlarm(events[2])); // cleared Problem
        Assert.False(ChargerEventMapper.IsAlarm(events[3])); // severity 8 periodic voltage
    }

    [Fact]
    public void Oversized_event_fields_are_truncated_to_their_column()
    {
        var request = new NotifyEventRequest
        {
            GeneratedAt = Received,
            EventData = new() { new EventDataType { TechInfo = new string('x', 900), Component = new ComponentType { Name = "C" }, Variable = new VariableType { Name = "V" } } }
        };

        Assert.Equal(500, ChargerEventMapper.Map("CP-1", request, Received)[0].TechInfo!.Length);
    }

    private static NotifyMonitoringReportRequest MonitorPart(int requestId, int seq, bool tbc, params (int Id, string Variable, int Severity)[] monitors) => new()
    {
        RequestId = requestId, SeqNo = seq, Tbc = tbc, GeneratedAt = Received,
        Monitor = monitors.Select(m => new MonitoringDataType
        {
            Component = new ComponentType { Name = "EVSE", Evse = new EVSEType { Id = 1 } },
            Variable = new VariableType { Name = m.Variable },
            VariableMonitoring = new() { new VariableMonitoringType { Id = m.Id, Severity = m.Severity, Type = MonitorEnumType.UpperThreshold, Value = 60 } }
        }).ToList()
    };

    [Fact]
    public void A_monitoring_report_completes_once_every_part_arrived_in_any_order()
    {
        using var assembler = new MonitoringReportAssembler();

        Assert.Null(assembler.Add("CP-1", MonitorPart(5, 1, true, (2, "Power", 4))));
        Assert.Null(assembler.Add("CP-1", MonitorPart(5, 2, false, (3, "Current", 5))));           // seq 0 still missing
        Assert.Single(assembler.Add("CP-2", MonitorPart(5, 0, false, (9, "Power", 1)))!);         // other charger, same request id
        var complete = assembler.Add("CP-1", MonitorPart(5, 0, true, (1, "Temperature", 2), (2, "Power", 4)));
        Assert.Equal(4, complete!.Count);

        var rows = ChargerEventMapper.MapMonitors("CP-1", 5, complete, Received);
        Assert.Equal(new[] { 1, 2, 3 }, rows.Select(r => r.MonitoringId).OrderBy(i => i)); // deduplicated by monitoring id
        Assert.True(rows.Single(r => r.MonitoringId == 1) is { Severity: 2, Type: "UpperThreshold", Value: 60, EvseId: 1, VariableName: "Temperature", RequestId: 5 });
    }

    [Fact]
    public void A_completed_monitoring_report_starts_over_and_an_empty_single_part_completes()
    {
        using var assembler = new MonitoringReportAssembler();
        assembler.Add("CP-1", MonitorPart(5, 0, false, (1, "Temperature", 2)));

        Assert.Single(assembler.Add("CP-1", MonitorPart(5, 0, false, (1, "Temperature", 2)))!);
        Assert.Empty(assembler.Add("CP-1", MonitorPart(6, 0, false))!);
        Assert.Throws<ArgumentException>(() => assembler.Add("CP-1", MonitorPart(9, -1, false)));
    }

    [Fact]
    public void A_full_monitoring_report_is_flagged_per_charger_and_request()
    {
        using var assembler = new MonitoringReportAssembler();
        assembler.MarkFullReport("CP-1", 7);

        Assert.True(assembler.IsFullReport("CP-1", 7));
        Assert.False(assembler.IsFullReport("CP-1", 8));
        Assert.False(assembler.IsFullReport("CP-2", 7));
    }

    [Fact]
    public async Task Stale_monitoring_report_parts_expire()
    {
        using var assembler = new MonitoringReportAssembler(TimeSpan.FromMilliseconds(1));
        assembler.Add("CP-1", MonitorPart(1, 0, true, (1, "Power", 4)));
        await Task.Delay(20);

        Assert.Null(assembler.Add("CP-1", MonitorPart(1, 1, false, (2, "Power", 4)))); // no partial report assembled
    }

    [Fact]
    public void Customer_information_parts_are_joined_in_seqNo_order()
    {
        var last = CustomerInformationAssembler.Append(null, 1, "world", tbc: false);
        Assert.False(last.Complete);
        Assert.Equal(1, last.PartsReceived);

        var both = CustomerInformationAssembler.Append(last.PartsJson, 0, "hello ", tbc: true);
        Assert.True(both.Complete);
        Assert.Equal("hello world", both.Data);
        Assert.Equal(2, both.PartsReceived);

        Assert.True(CustomerInformationAssembler.Append(null, 0, "only", tbc: false) is { Complete: true, Data: "only" });
    }

    [Fact]
    public void A_missing_customer_information_part_keeps_it_incomplete_until_it_arrives()
    {
        var gap = CustomerInformationAssembler.Append(CustomerInformationAssembler.Append(null, 0, "a", true).PartsJson, 2, "c", false);
        Assert.False(gap.Complete);
        Assert.True(CustomerInformationAssembler.Append(gap.PartsJson, 1, "b", true) is { Complete: true, Data: "abc" });
    }

    [Fact]
    public void Display_messages_are_stored_as_a_snapshot_with_UTC_dates()
    {
        var display = JsonConvert.DeserializeObject<NotifyDisplayMessagesRequestType>("""
            { "requestId": 12, "tbc": false, "messageInfo": [
              { "id": 3, "priority": "InFront", "state": "Charging", "startDateTime": "2026-10-03T10:00:00+02:00", "transactionId": "tx-1",
                "message": { "format": "UTF8", "language": "fr", "content": "Bonjour" }, "display": { "name": "Display", "evse": { "id": 2 } } } ] }
            """)!;

        var snapshot = Assert.Single(ChargerEventMapper.MapDisplayMessages("CP-1", display.RequestId!.Value, display.MessageInfo, Received));

        Assert.True(snapshot is { RequestId: 12, MessageId: 3, Priority: "InFront", State: "Charging", Content: "Bonjour", Format: "UTF8", Language: "fr",
            DisplayComponentName: "Display", DisplayEvseId: 2, TransactionId: "tx-1" });
        Assert.Equal(new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc), snapshot.StartDateTime);
    }
}
