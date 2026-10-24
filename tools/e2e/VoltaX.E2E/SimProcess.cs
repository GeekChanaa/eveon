using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace VoltaX.E2E;

/// <summary>What one simulated charger reported about one transaction (EVEON_SIM sim-server --report).</summary>
public sealed record ReportedTransaction(
    string? TransactionId,
    string? IdTag,
    DateTime? StartedAt,
    DateTime? StoppedAt,
    double? MeterStartWh,
    double? MeterStopWh,
    double? EnergyWh,
    string? StartStatus,
    string? StopReason,
    string? StopTrigger);

public sealed record SimReport(bool? Passed, IReadOnlyList<ReportedTransaction> Transactions, IReadOnlyList<string> Assertions)
{
    public static SimReport Read(string file)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        var root = doc.RootElement;
        bool? passed = null;
        var assertions = new List<string>();
        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object)
        {
            if (result.TryGetProperty("passed", out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False)
                passed = p.GetBoolean();
            if (result.TryGetProperty("assertions", out var list) && list.ValueKind == JsonValueKind.Array)
                foreach (var a in list.EnumerateArray())
                    assertions.Add($"{(Bool(a, "passed") == true ? "PASS" : "FAIL")} {Str(a, "label")} — {Str(a, "detail")}");
        }

        var transactions = new List<ReportedTransaction>();
        if (root.TryGetProperty("chargers", out var chargers))
            foreach (var charger in chargers.EnumerateArray())
                foreach (var t in charger.GetProperty("transactions").EnumerateArray())
                    transactions.Add(new ReportedTransaction(
                        t.TryGetProperty("transactionId", out var id) && id.ValueKind != JsonValueKind.Null ? id.ToString() : null,
                        Str(t, "idTag"), Time(t, "startedAt"), Time(t, "stoppedAt"), Num(t, "meterStartWh"), Num(t, "meterStopWh"),
                        Num(t, "energyWh"), Str(t, "startStatus"), Str(t, "stopReason"), Str(t, "stopTrigger")));
        return new SimReport(passed, transactions, assertions);
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool? Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;

    private static double? Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static DateTime? Time(JsonElement e, string name) =>
        Str(e, name) is { } s && DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t) ? t : null;
}

/// <summary>One headless EVEON_SIM run (sim-server) of a preset on one charger against the local API.</summary>
public static class SimProcess
{
    public static Process StartScenario(Workspace workspace, string ocppUrl, string preset, string chargePointId, string version,
        string password, string reportFile, string logFile) =>
        Run.Start("node", new[]
        {
            workspace.SimServerJs,
            "--port", "0",
            "--scenario", preset,
            "--csms", ocppUrl,
            "--id", chargePointId,
            "--version", version,
            "--password", password,
            "--connectors", "1",
            "--drain", "120", // ends as soon as every queued message is answered
            "--report", reportFile,
            "--exit"
        }, workspace.SimDir, logFile);
}
