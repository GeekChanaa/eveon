using System.Text.Json;
using VoltaX.Tests.Infrastructure;
using VoltaX.Tests.Ocpp.Matrix;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp;

/// <summary>OCPP 1.6J transaction specifics on the matrix station: meter value units and samples, transactionData, errorCode.</summary>
public class Ocpp16TransactionTests
{
    private static Task<JsonElement> Call(OcppTestHost host, string action, object payload) => host.Call(Cp, action, payload, OcppTestHost.Ocpp16);

    private static Task<JsonElement> Meter(OcppTestHost host, int transactionId, DateTime at, params object[] samples) =>
        Call(host, "MeterValues", new { connectorId = 1, transactionId, meterValue = new[] { new { timestamp = at, sampledValue = samples } } });

    [Fact]
    public async Task Meter_values_in_kWh_without_unit_or_measurand_and_signed_samples_are_read_like_the_charger_meant()
    {
        var (host, charger, cardId) = await Open(OcppTestHost.Ocpp16);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        var id = ScenarioCharger16.TransactionId(uid);

        // 6 kWh in kWh; the L1 reading and the current do not replace the whole-meter register.
        await Meter(host, id, T0.AddMinutes(10),
            new { value = "6", measurand = "Energy.Active.Import.Register", unit = "kWh", context = "Sample.Periodic" },
            new { value = "2100", measurand = "Energy.Active.Import.Register", unit = "Wh", phase = "L1" },
            new { value = "32", measurand = "Current.Import", unit = "A", phase = "L1" });
        Assert.Equal(6.0, (await Tx(host)).MeterStop!.Value, 6);

        // No measurand means Energy.Active.Import.Register and no unit means Wh; signed data is skipped.
        await Meter(host, id, T0.AddMinutes(20),
            new { value = "9000" },
            new { value = "AP8Bc2lnbmVk", format = "SignedData", measurand = "Energy.Active.Import.Register", unit = "Wh" });
        Assert.Equal(9.0, (await Tx(host)).MeterStop!.Value, 6);

        // transactionData in StopTransaction only feeds the live view: the bill is on meterStop.
        await Call(host, "StopTransaction", new
        {
            transactionId = id,
            idTag = "TAG-1",
            meterStop = 13000,
            timestamp = T0.AddMinutes(30),
            reason = "Local",
            transactionData = new[] { new { timestamp = T0.AddMinutes(30), sampledValue = new[] { new { value = "99999", unit = "Wh" } } } }
        });

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
    }

    [Fact]
    public async Task A_status_with_an_error_code_while_charging_is_not_a_fault()
    {
        var (host, charger, cardId) = await Open(OcppTestHost.Ocpp16);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        // Charging goes on with a warning (e.g. high temperature derating).
        await Call(host, "StatusNotification", new { connectorId = 1, errorCode = "HighTemperature", status = "Charging", timestamp = T0.AddMinutes(5) });
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");

        Assert.Empty(host.SystemReports.CallsTo(nameof(VoltaXApi.Services.ISystemReportService.HandleReport)));
        Assert.DoesNotContain("ChargerFaultDuringSession", await ScenarioAssert.Notifications(host));
        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
    }

    [Fact]
    public async Task Connector_zero_status_and_meter_values_never_touch_a_session()
    {
        var (host, charger, cardId) = await Open(OcppTestHost.Ocpp16);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        await Call(host, "StatusNotification", new { connectorId = 0, errorCode = "NoError", status = "Available", timestamp = T0.AddMinutes(1) });
        await Call(host, "MeterValues", new
        {
            connectorId = 0,
            meterValue = new[] { new { timestamp = T0.AddMinutes(2), sampledValue = new[] { new { value = "500000", unit = "Wh" } } } }
        });
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
    }
}
