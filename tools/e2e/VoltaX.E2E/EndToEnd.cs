using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Ocpp16;

namespace VoltaX.E2E;

/// <summary>One preset on one protocol version, on its own charge point.</summary>
public sealed class ScenarioRun(ScenarioSpec spec, string version)
{
    public ScenarioSpec Spec { get; } = spec;
    public string Version { get; } = version;
    public string ChargePointId => Spec.ChargePointFor(Version);
    public string Name => $"{Spec.Preset} [{Version}]";
    public int ExitCode { get; set; } = -1;
    public TimeSpan Duration { get; set; }
    public string? DriverError { get; set; }
    public SimReport? Report { get; set; }
    public string LogFile { get; set; } = "";
    public List<(bool Ok, string Text)> Checks { get; } = new();
    public bool Passed => Checks.Count > 0 && Checks.All(c => c.Ok);

    public void Check(bool ok, string text) => Checks.Add((ok, text));
}

/// <summary>
/// The end-to-end matrix: every preset of <see cref="Scenarios"/> on OCPP 1.6 and 2.0.1, each on its own charge point,
/// a few at a time, then the API is asked (as each driver) what it recorded: closed sessions whose energy, duration,
/// stop reason, amounts and VAT match what the charger reported and the tariff, one wallet debit per session, and card
/// balances equal to the opening balance minus the sessions.
/// </summary>
public static class EndToEnd
{
    public static async Task<int> RunAsync(Workspace workspace, RunnerOptions options, CancellationToken ct)
    {
        var specs = Scenarios.All.Where(s => options.Only == null || s.Preset.Contains(options.Only, StringComparison.OrdinalIgnoreCase)).ToList();
        if (specs.Count == 0)
            throw new SetupException($"No preset matches --only {options.Only}: {string.Join(", ", Scenarios.All.Select(s => s.Preset))}");
        var runs = specs.SelectMany(s => options.Versions.Select(v => new ScenarioRun(s, v))).ToList();

        var chargerPassword = "e2e-cp-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        var driverPassword = "E2e-driver-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        var drivers = runs.Where(r => r.Spec.IdTag != null)
            .Select(r => new SeedDriver(r.Spec.UserFor(r.Version), driverPassword, r.Spec.CardFor(r.Version), r.Spec.Balance))
            .DistinctBy(d => d.Email)
            .ToList();

        Console.WriteLine($"[e2e] work folder {workspace.OutDir}");
        Console.WriteLine($"[e2e] simulator {workspace.SimDir}");
        if (options.BuildSim)
        {
            Console.WriteLine("[e2e] building the simulator (npm run sim:build)");
            await workspace.BuildSimulatorAsync(ct);
        }

        var database = Path.Combine(workspace.OutDir, "voltax-e2e.db");
        await Seeder.CreateAsync(database, runs.Select(r => new SeedChargePoint(r.ChargePointId, chargerPassword)), drivers, ct);
        Console.WriteLine($"[e2e] seeded {runs.Count} charge points and {drivers.Count} drivers in {database}");

        await using var api = await ApiProcess.StartAsync(workspace, database, ct);
        Console.WriteLine($"[e2e] API on {api.BaseUrl} (log {api.LogFile}); {runs.Count} runs, {options.Parallel} at a time");

        var clock = Stopwatch.StartNew();
        using var gate = new SemaphoreSlim(options.Parallel);
        await Task.WhenAll(runs.Select(async run =>
        {
            await gate.WaitAsync(ct);
            try
            {
                await ExecuteAsync(workspace, api, run, chargerPassword, driverPassword, ct);
            }
            finally
            {
                gate.Release();
            }
        }));
        Console.WriteLine($"[e2e] all simulator runs finished in {clock.Elapsed.TotalSeconds:0}s; checking what the API recorded");

        await VerifyAsync(api, database, runs, driverPassword, ct);
        return Summarize(workspace, runs, clock.Elapsed, api.LoggedErrors());
    }

    private static async Task ExecuteAsync(Workspace workspace, ApiProcess api, ScenarioRun run, string chargerPassword, string driverPassword,
        CancellationToken ct)
    {
        var report = Path.Combine(workspace.OutDir, $"{run.ChargePointId}.report.json");
        run.LogFile = Path.Combine(workspace.OutDir, $"{run.ChargePointId}.log");
        var started = Stopwatch.StartNew();
        var process = SimProcess.StartScenario(workspace, api.OcppUrl, run.Spec.Preset, run.ChargePointId, run.Version, chargerPassword, report, run.LogFile);
        var driver = run.Spec.RemoteStartStop ? RemoteDriveAsync(api, run, driverPassword, ct) : Task.FromResult<string?>(null);
        run.ExitCode = await Run.WaitAsync(process, TimeSpan.FromMinutes(10), ct);
        run.DriverError = await driver;
        run.Duration = started.Elapsed;
        if (File.Exists(report))
        {
            try { run.Report = SimReport.Read(report); }
            catch (Exception ex) { run.DriverError ??= $"unreadable report: {ex.Message}"; }
        }
        Console.WriteLine($"[e2e] {run.Name,-46} simulator exit {run.ExitCode} in {run.Duration.TotalSeconds:0}s");
    }

    /// <summary>
    /// What a driver does in the mobile app for "Remote start and stop": start once the car is plugged in (the API
    /// refuses before), let it charge, stop the transaction of their current session.
    /// </summary>
    private static async Task<string?> RemoteDriveAsync(ApiProcess api, ScenarioRun run, string password, CancellationToken ct)
    {
        try
        {
            using var client = await VoltaXClient.SignInAsync(api.BaseUrl, run.Spec.UserFor(run.Version), password, ct);
            var last = "";
            var startedAt = DateTime.UtcNow;
            var accepted = false;
            for (var attempt = 1; !accepted && DateTime.UtcNow - startedAt < TimeSpan.FromMinutes(2); attempt++)
            {
                var (status, body) = await client.RemoteStartAsync(run.ChargePointId, 1000 + attempt, ct);
                accepted = status == HttpStatusCode.OK && body.Contains("Accepted", StringComparison.OrdinalIgnoreCase);
                last = $"{(int)status} {body}";
                if (!accepted)
                    await Task.Delay(1000, ct);
            }
            if (!accepted)
                return $"remote start never accepted: {last}";

            string? uid = null;
            for (var i = 0; uid == null && i < 60; i++)
            {
                uid = await client.CurrentTransactionUidAsync(ct);
                if (uid == null)
                    await Task.Delay(1000, ct);
            }
            if (uid == null)
                return "no current session after the remote start";

            await Task.Delay(TimeSpan.FromSeconds(20), ct); // a few simulated minutes of charging
            var (stopStatus, stopBody) = await client.RemoteStopAsync(run.ChargePointId, uid, ct);
            return stopStatus == HttpStatusCode.OK && stopBody.Contains("Accepted", StringComparison.OrdinalIgnoreCase)
                ? null
                : $"remote stop of {uid} not accepted: {(int)stopStatus} {stopBody}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"remote driver failed: {ex.Message}";
        }
    }

    private static async Task VerifyAsync(ApiProcess api, string database, List<ScenarioRun> runs, string driverPassword, CancellationToken ct)
    {
        foreach (var run in runs)
        {
            run.Check(run.ExitCode == 0, $"simulator scenario passed its own expectations (exit {run.ExitCode}, log {Path.GetFileName(run.LogFile)})");
            if (run.Report == null)
                run.Check(false, "simulator report written");
            if (run.DriverError != null)
                run.Check(false, run.DriverError);
        }

        foreach (var group in runs.Where(r => r.Spec.IdTag != null).GroupBy(r => r.Spec.UserFor(r.Version)))
        {
            using var client = await VoltaXClient.SignInAsync(api.BaseUrl, group.Key, driverPassword, ct);
            var sessions = await client.SessionsAsync(ct);
            var wallet = await client.WalletAsync(ct);
            var cards = await client.CardsAsync(ct);
            foreach (var run in group)
                await VerifyRunAsync(client, run, sessions, wallet, ct);

            // The card pays every session of its owner: opening balance minus the session totals, and the ledger agrees.
            var first = group.First();
            var card = cards.SingleOrDefault();
            if (card == null)
            {
                first.Check(false, $"{group.Key} has exactly one card (has {cards.Count})");
                continue;
            }
            var expected = first.Spec.Balance - sessions.Sum(s => s.Total);
            var ledger = wallet.Sum(m => m.Amount);
            foreach (var run in group)
            {
                run.Check(card.Balance == expected, $"card balance {card.Balance:0.00} = opening {first.Spec.Balance:0.00} - sessions {sessions.Sum(s => s.Total):0.00}");
                run.Check(ledger == card.Balance, $"wallet ledger sum {ledger:0.00} = card balance {card.Balance:0.00}");
            }
        }

        // Runs without a driver (refused unknown card): nothing at all may have been recorded on their charge point.
        await using var db = Seeder.Open(database);
        foreach (var run in runs.Where(r => r.Spec.IdTag == null))
        {
            var count = await db.ChargingSessions.CountAsync(s => s.Connector!.ChargePoint!.ChargePointId == run.ChargePointId, ct);
            run.Check(count == 0, $"no session recorded on {run.ChargePointId} (found {count})");
            var accepted = run.Report?.Transactions.Count(t => t.StartStatus == "Accepted") ?? 0;
            run.Check(accepted == 0, $"the charger got no accepted transaction (got {accepted})");
        }
    }

    private static async Task VerifyRunAsync(VoltaXClient client, ScenarioRun run, List<MySessionDto> userSessions,
        List<MyWalletMovementDto> wallet, CancellationToken ct)
    {
        if (run.Report == null)
            return;
        var accepted = run.Report.Transactions.Where(t => t.StartStatus == "Accepted").ToList();
        var sessions = userSessions.Where(s => s.ChargePointName == run.ChargePointId).ToList();
        switch (run.Spec.Sessions)
        {
            case SessionExpectation.One:
                run.Check(accepted.Count == 1, $"the charger reported one accepted transaction (reported {accepted.Count})");
                run.Check(sessions.Count == 1, $"one session recorded (found {sessions.Count})");
                break;
            case SessionExpectation.PerTransaction:
                run.Check(accepted.Count >= 1, $"the charger reported accepted transactions ({accepted.Count})");
                run.Check(sessions.Count == accepted.Count, $"one session per reported transaction ({sessions.Count} for {accepted.Count})");
                break;
        }

        foreach (var tx in accepted)
        {
            var session = sessions.FirstOrDefault(s => tx.StartedAt != null && Math.Abs((s.StartDate - tx.StartedAt.Value).TotalSeconds) < 2);
            var label = $"transaction {tx.TransactionId}";
            if (session == null)
            {
                run.Check(false, $"{label}: a session starting at {tx.StartedAt:o} ({string.Join(", ", sessions.Select(s => s.StartDate.ToString("o", CultureInfo.InvariantCulture)))})");
                continue;
            }
            await VerifySessionAsync(client, run, tx, session, wallet, $"{label} / session {session.Id}", ct);
        }
    }

    private static async Task VerifySessionAsync(VoltaXClient client, ScenarioRun run, ReportedTransaction tx, MySessionDto session,
        List<MyWalletMovementDto> wallet, string label, CancellationToken ct)
    {
        run.Check(!session.Active && session.EndDate != null, $"{label}: closed (active={session.Active}, end={session.EndDate:o})");

        if (run.Spec.EnergyFromReport && tx.EnergyWh != null)
            run.Check(Math.Abs(session.ChargedKwhs - tx.EnergyWh.Value / 1000) <= 0.001,
                $"{label}: {session.ChargedKwhs:0.###} kWh = charger meterStop - meterStart ({tx.EnergyWh} Wh)");
        if (tx.StartedAt != null && tx.StoppedAt != null)
        {
            var minutes = (tx.StoppedAt.Value - tx.StartedAt.Value).TotalMinutes;
            run.Check(Math.Abs(session.ChargedMinutes - minutes) <= 0.05, $"{label}: {session.ChargedMinutes:0.##} min = charger stop - start ({minutes:0.##} min)");
        }

        var expectedReason = ExpectedReason(run.Version, tx.StopReason);
        if (expectedReason != null)
            run.Check(session.StoppedReason == expectedReason, $"{label}: stop reason {session.StoppedReason} (charger sent {tx.StopReason})");
        if (run.Spec.Reasons is { Length: > 0 } reasons)
            run.Check(reasons.Contains(session.StoppedReason), $"{label}: stop reason {session.StoppedReason} is one of {string.Join("/", reasons)}");

        var kwh = (decimal)Math.Max(0, session.ChargedKwhs);
        var energy = Tariff.Round(kwh * Tariff.PricePerKWh);
        var time = Tariff.Round((decimal)Math.Max(0, session.ChargedMinutes) * Tariff.PricePerMinute);
        var flat = kwh > 0 ? Tariff.FlatFee : 0m;
        var charge = energy + time + flat;
        run.Check(session.EnergyAmount == energy && session.TimeAmount == time && session.FlatFeeAmount == flat,
            $"{label}: amounts {session.EnergyAmount:0.00} + {session.TimeAmount:0.00} + {session.FlatFeeAmount:0.00} = tariff {energy:0.00} + {time:0.00} + {flat:0.00}");
        if (!run.Spec.IdleAllowed)
            run.Check(session.IdleAmount == 0, $"{label}: no idle fee after a session the charger ended ({session.IdleAmount:0.00})");
        run.Check(session.Total == charge + session.IdleAmount, $"{label}: total {session.Total:0.00} = {charge:0.00} + idle {session.IdleAmount:0.00}");

        var detail = await client.SessionAsync(session.Id, ct);
        if (detail == null)
        {
            run.Check(false, $"{label}: detail readable");
        }
        else
        {
            var excl = Tariff.Round(detail.Session.Total / (1 + Tariff.VatRate));
            run.Check(detail.VatRate == Tariff.VatRate && detail.TotalExclVat == excl && detail.TotalExclVat + detail.Vat == detail.Session.Total,
                $"{label}: VAT {detail.VatRate:P0}: {detail.TotalExclVat:0.00} + {detail.Vat:0.00} = {detail.Session.Total:0.00}");
        }

        var debits = wallet.Where(m => m.ChargingSessionId == session.Id && m.Type == WalletTransactionType.SessionCharge).ToList();
        run.Check(charge > 0 ? debits.Count == 1 && debits[0].Amount == -charge : debits.Count == 0,
            $"{label}: one wallet debit of {-charge:0.00} ({string.Join(", ", debits.Select(d => d.Amount.ToString("0.00", CultureInfo.InvariantCulture)))})");
        var idle = wallet.Where(m => m.ChargingSessionId == session.Id && m.Type == WalletTransactionType.IdleFee).ToList();
        run.Check(session.IdleAmount > 0 ? idle.Count == 1 && idle[0].Amount == -session.IdleAmount : idle.Count == 0,
            $"{label}: idle fee movement matches {session.IdleAmount:0.00}");
    }

    /// <summary>The stop reason a session stores for what the charger sent (1.6 reasons are mapped to the 2.0.1 vocabulary).</summary>
    private static ReasonEnumType? ExpectedReason(string version, string? sent)
    {
        if (string.IsNullOrEmpty(sent))
            return version == "1.6" ? ReasonEnumType.Local : null;
        if (version == "1.6")
            return Ocpp16Mapping.ToStoppedReason(sent);
        return Enum.TryParse<ReasonEnumType>(sent, out var reason) ? reason : null;
    }

    private static int Summarize(Workspace workspace, List<ScenarioRun> runs, TimeSpan elapsed, List<(string Message, int Count)> apiErrors)
    {
        var text = new StringBuilder();
        text.AppendLine($"# VoltaX end-to-end run ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC, {elapsed.TotalSeconds:0}s)");
        text.AppendLine();
        text.AppendLine("| Scenario | OCPP | Result | Sessions | Energy (kWh) | Billed | Simulator |");
        text.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var run in runs)
        {
            var accepted = run.Report?.Transactions.Where(t => t.StartStatus == "Accepted").ToList() ?? new();
            text.AppendLine($"| {run.Spec.Preset} | {run.Version} | {(run.Passed ? "PASS" : "FAIL")} | {accepted.Count} | " +
                $"{accepted.Sum(t => (t.EnergyWh ?? 0) / 1000):0.###} | {run.Checks.Count(c => c.Ok)}/{run.Checks.Count} checks | " +
                $"exit {run.ExitCode}, {run.Duration.TotalSeconds:0}s |");
        }
        foreach (var run in runs.Where(r => !r.Passed))
        {
            text.AppendLine();
            text.AppendLine($"## {run.Name}: failed checks");
            foreach (var (_, check) in run.Checks.Where(c => !c.Ok))
                text.AppendLine($"- {check}");
            foreach (var assertion in run.Report?.Assertions.Where(a => a.StartsWith("FAIL", StringComparison.Ordinal)) ?? Enumerable.Empty<string>())
                text.AppendLine($"- simulator: {assertion}");
            text.AppendLine($"- log: {run.LogFile}");
        }
        // Not a check (some are handled, such as an invoice number collision that is retried), but worth a look.
        if (apiErrors.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"## API errors logged ({apiErrors.Sum(e => e.Count)}, by message)");
            foreach (var (message, count) in apiErrors.Take(20))
                text.AppendLine($"- {count} x {message}");
        }
        var summary = text.ToString();
        File.WriteAllText(Path.Combine(workspace.OutDir, "summary.md"), summary);
        Console.WriteLine();
        Console.WriteLine(summary);
        var failed = runs.Count(r => !r.Passed);
        Console.WriteLine(failed == 0
            ? $"[e2e] PASSED: {runs.Count} runs, {runs.Sum(r => r.Checks.Count)} checks"
            : $"[e2e] FAILED: {failed} of {runs.Count} runs (details above and in {workspace.OutDir})");
        return failed == 0 ? 0 : 1;
    }
}
