using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaX.E2E;

/// <summary>What the simulator fleets report together (EVEON_SIM GET /api/fleet/stats, added up).</summary>
public sealed record FleetStats(int Total, int Connected, int Accepted, int Charging, int Faulted, long Sent, long Received,
    long Errors, long Reconnects, long Queued, int AvgRttMs)
{
    public static FleetStats Sum(IReadOnlyList<FleetStats> all) => new(
        all.Sum(s => s.Total), all.Sum(s => s.Connected), all.Sum(s => s.Accepted), all.Sum(s => s.Charging),
        all.Sum(s => s.Faulted), all.Sum(s => s.Sent), all.Sum(s => s.Received), all.Sum(s => s.Errors),
        all.Sum(s => s.Reconnects), all.Sum(s => s.Queued),
        all.Sum(s => s.Total) == 0 ? 0 : (int)Math.Round(all.Sum(s => (double)s.AvgRttMs * s.Total) / all.Sum(s => s.Total)));
}

/// <summary>
/// One headless EVEON_SIM process running <c>--count</c> chargers of one OCPP version against the local API, driven
/// through its control API (local only, on a free port).
/// </summary>
public sealed class SimFleet : IAsyncDisposable
{
    private readonly Process _process;
    private readonly HttpClient _http;

    private SimFleet(Process process, HttpClient http, string version, IReadOnlyList<string> ids, string logFile)
    {
        _process = process;
        _http = http;
        Version = version;
        Ids = ids;
        LogFile = logFile;
    }

    public string Version { get; }
    public IReadOnlyList<string> Ids { get; }
    public string LogFile { get; }

    /// <summary>The identities the simulator gives its chargers for a pattern such as E2E-LOAD-16-{0000}.</summary>
    public static IReadOnlyList<string> IdsFor(string version, int count)
    {
        var width = Math.Max(4, count.ToString(CultureInfo.InvariantCulture).Length);
        return Enumerable.Range(1, count)
            .Select(i => $"E2E-LOAD-{ScenarioSpec.VersionTag(version)}-{i.ToString(new string('0', width), CultureInfo.InvariantCulture)}")
            .ToList();
    }

    public static async Task<SimFleet> StartAsync(Workspace workspace, ApiProcess api, string version, int count, string password,
        int staggerMs, CancellationToken ct)
    {
        var ids = IdsFor(version, count);
        var width = ids[0].Length - ids[0].LastIndexOf('-') - 1;
        var port = Run.FreeTcpPort();
        var logFile = Path.Combine(workspace.OutDir, $"sim-load-{ScenarioSpec.VersionTag(version)}.log");
        var process = Run.Start("node", new[]
        {
            workspace.SimServerJs,
            "--port", port.ToString(CultureInfo.InvariantCulture),
            "--csms", api.OcppUrl,
            "--version", version,
            "--password", password,
            "--connectors", "1",
            "--count", count.ToString(CultureInfo.InvariantCulture),
            "--pattern", $"E2E-LOAD-{ScenarioSpec.VersionTag(version)}-{{{new string('0', width)}}}",
            "--stagger", staggerMs.ToString(CultureInfo.InvariantCulture),
        }, workspace.SimDir, logFile);
        var fleet = new SimFleet(process, new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(30) },
            version, ids, logFile);

        var until = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < until)
        {
            ct.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                await fleet.DisposeAsync();
                throw new SetupException($"The {version} simulator exited at start up:\n{Run.Tail(logFile)}");
            }
            try
            {
                if ((await fleet._http.GetAsync("/api/health", ct)).IsSuccessStatusCode)
                    return fleet;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            await Task.Delay(500, ct);
        }
        await fleet.DisposeAsync();
        throw new SetupException($"The {version} simulator control API did not answer in 60 s:\n{Run.Tail(logFile)}");
    }

    public async Task<FleetStats> StatsAsync(CancellationToken ct)
    {
        using var body = JsonDocument.Parse(await _http.GetStringAsync("/api/fleet/stats", ct));
        var s = body.RootElement;
        int I(string name) => s.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
        long L(string name) => s.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
        return new FleetStats(I("total"), I("connected"), I("accepted"), I("charging"), I("faulted"), L("sent"), L("received"),
            L("errors"), L("reconnects"), L("queued"), I("avgRttMs"));
    }

    /// <summary>A simulator action on every charger of the fleet (sampleInterval, stop, ...).</summary>
    public async Task FleetActionAsync(string action, object args, CancellationToken ct) =>
        (await _http.PostAsJsonAsync($"/api/fleet/actions/{action}", args, ct)).EnsureSuccessStatusCode();

    /// <summary>A simulator action on one charger; its result as text ("Accepted" for a card the CSMS took).</summary>
    public async Task<string> ActionAsync(string chargerId, string action, object args, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/api/chargers/{chargerId}/actions/{action}", args, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return $"{(int)response.StatusCode} {text}";
        using var body = JsonDocument.Parse(text);
        return body.RootElement.TryGetProperty("result", out var result) ? result.ToString() : text;
    }

    public async ValueTask DisposeAsync()
    {
        Run.Kill(_process);
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).ContinueWith(_ => { });
        _process.Dispose();
        _http.Dispose();
    }
}

/// <summary>
/// Load run: <c>--chargers</c> simulated chargers (half 1.6, half 2.0.1) connect to the local API, each starts a
/// session with its own card, they charge and report meter values for <c>--minutes</c> while the runner samples the
/// fleet, the API process and the response time of a driver polling their running session; then every session is
/// stopped and the database must hold one closed, billed session per charger.
/// </summary>
public static class LoadTest
{
    private sealed record Sample(TimeSpan At, FleetStats Fleet, double CpuPercent, long WorkingSetMb, double ProbeP50Ms, double ProbeP95Ms,
        int ProbeErrors);

    public static async Task<int> RunAsync(Workspace workspace, RunnerOptions options, CancellationToken ct)
    {
        var versions = options.Versions.ToList();
        var perVersion = versions.Select((v, i) => (Version: v,
            Count: options.Chargers / versions.Count + (i < options.Chargers % versions.Count ? 1 : 0)))
            .Where(p => p.Count > 0)
            .ToList();
        var chargers = perVersion.SelectMany(p => SimFleet.IdsFor(p.Version, p.Count)).ToList();

        var chargerPassword = "e2e-cp-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        var driverPassword = "E2e-driver-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        // One driver and card per charger, with enough credit that no session runs out during the run.
        var drivers = chargers.Select(id => new SeedDriver($"{id}@example.com".ToLowerInvariant(), driverPassword, id, 100_000m)).ToList();
        var checks = new List<(bool Ok, string Text)>();
        void Check(bool ok, string text) => checks.Add((ok, text));

        Console.WriteLine($"[load] work folder {workspace.OutDir}");
        Console.WriteLine($"[load] simulator {workspace.SimDir}");
        if (options.BuildSim)
        {
            Console.WriteLine("[load] building the simulator (npm run sim:build)");
            await workspace.BuildSimulatorAsync(ct);
        }

        var database = Path.Combine(workspace.OutDir, "voltax-e2e.db");
        await Seeder.CreateAsync(database, chargers.Select(id => new SeedChargePoint(id, chargerPassword)), drivers, ct);
        Console.WriteLine($"[load] seeded {chargers.Count} charge points and drivers in {database}");

        await using var api = await ApiProcess.StartAsync(workspace, database, ct);
        Console.WriteLine($"[load] API on {api.BaseUrl} (log {api.LogFile})");

        var fleets = new List<SimFleet>();
        try
        {
            var clock = Stopwatch.StartNew();
            foreach (var (version, count) in perVersion)
                fleets.Add(await SimFleet.StartAsync(workspace, api, version, count, chargerPassword, options.StaggerMs, ct));
            Console.WriteLine($"[load] {string.Join(", ", perVersion.Select(p => $"{p.Count} x OCPP {p.Version}"))} connecting, {options.StaggerMs} ms apart");

            // 1. Every charger connects and is accepted.
            var connectLimit = TimeSpan.FromMilliseconds(Math.Max(perVersion.Max(p => p.Count) * options.StaggerMs, 0)) + TimeSpan.FromSeconds(90);
            var stats = await WaitForAsync(fleets, s => s.Accepted == chargers.Count, connectLimit, ct);
            var connectTime = clock.Elapsed;
            Check(stats.Accepted == chargers.Count, $"{stats.Accepted}/{chargers.Count} chargers booted and accepted ({connectTime.TotalSeconds:0.0}s)");
            Console.WriteLine($"[load] {stats.Accepted}/{chargers.Count} accepted after {connectTime.TotalSeconds:0.0}s");

            // 2. Every charger starts a session with its own card.
            foreach (var fleet in fleets)
                await fleet.FleetActionAsync("sampleInterval", new { seconds = options.MeterIntervalSeconds }, ct);
            var cardAnswers = new Dictionary<string, string>();
            using (var gate = new SemaphoreSlim(16))
            {
                await Task.WhenAll(fleets.SelectMany(f => f.Ids.Select((id, i) => (Fleet: f, Id: id, Index: i))).Select(async c =>
                {
                    await Task.Delay(c.Index * options.StaggerMs, ct);
                    await gate.WaitAsync(ct);
                    try
                    {
                        await c.Fleet.ActionAsync(c.Id, "plugIn", new { connector = 1 }, ct);
                        var answer = await c.Fleet.ActionAsync(c.Id, "card", new { idTag = c.Id, connector = 1 }, ct);
                        lock (cardAnswers) cardAnswers[c.Id] = answer.Trim('"');
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));
            }
            var refused = cardAnswers.Where(a => a.Value != "Accepted").ToList();
            Check(refused.Count == 0, $"every card accepted ({refused.Count} refused{(refused.Count > 0 ? ": " + string.Join(", ", refused.Take(5).Select(r => $"{r.Key} {r.Value}")) : "")})");
            stats = await WaitForAsync(fleets, s => s.Charging == chargers.Count, TimeSpan.FromSeconds(90), ct);
            Check(stats.Charging == chargers.Count, $"{stats.Charging}/{chargers.Count} connectors charging");
            Console.WriteLine($"[load] {stats.Charging}/{chargers.Count} charging; measuring for {options.Minutes:0.#} min");

            // 3. Measured window.
            var before = stats;
            var samples = await MeasureAsync(api, fleets, drivers[0], TimeSpan.FromMinutes(options.Minutes), ct);
            var after = samples.Count > 0 ? samples[^1].Fleet : before;
            var window = TimeSpan.FromMinutes(options.Minutes).TotalSeconds;
            Check(after.Reconnects == before.Reconnects, $"no charger reconnected during the window ({after.Reconnects - before.Reconnects})");
            Check(after.Errors == 0, $"no CALLERROR or timeout seen by the chargers ({after.Errors})");
            Check(after.Charging == chargers.Count, $"{after.Charging}/{chargers.Count} still charging at the end of the window");
            var probes = samples.Where(s => s.ProbeP95Ms > 0).ToList();
            var probeErrors = samples.Sum(s => s.ProbeErrors);
            Check(probeErrors == 0, $"polling the running session never failed ({probeErrors} errors)");

            // 4. Every session stops and is billed.
            foreach (var fleet in fleets)
                await fleet.FleetActionAsync("stop", new { connector = 1, reason = "Local" }, ct);
            stats = await WaitForAsync(fleets, s => s.Charging == 0 && s.Queued == 0, TimeSpan.FromSeconds(120), ct);
            Check(stats.Charging == 0 && stats.Queued == 0, $"every session stopped and every message answered ({stats.Charging} charging, {stats.Queued} queued)");
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // the last StopTransaction / TransactionEvent(Ended) bills after answering

            await using (var db = Seeder.Open(database))
            {
                var sessions = await db.ChargingSessions.AsNoTracking()
                    .Where(s => s.Connector!.ChargePoint!.ChargePointId.StartsWith("E2E-LOAD-"))
                    .Select(s => new { s.ID, s.EndDate, s.TotalAmount, s.ChargedKwhs, ChargePoint = s.Connector!.ChargePoint!.ChargePointId })
                    .ToListAsync(ct);
                var debits = await db.WalletTransactions.AsNoTracking()
                    .Where(w => w.Type == WalletTransactionType.SessionCharge && w.ChargingSessionID != null)
                    .GroupBy(w => w.ChargingSessionID!.Value)
                    .Select(g => new { Session = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Session, g => g.Count, ct);
                var messages = await db.MessageLogs.AsNoTracking().CountAsync(m => m.ChargePointId!.StartsWith("E2E-LOAD-"), ct);

                var perCharger = sessions.GroupBy(s => s.ChargePoint).ToDictionary(g => g.Key, g => g.Count());
                var wrong = chargers.Where(id => perCharger.GetValueOrDefault(id) != 1).ToList();
                Check(wrong.Count == 0, $"one session per charger ({sessions.Count} sessions for {chargers.Count} chargers{(wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong.Take(5)) : "")})");
                var open = sessions.Count(s => s.EndDate == null);
                Check(open == 0, $"every session closed ({open} open)");
                var unbilled = sessions.Count(s => s.TotalAmount == null || s.TotalAmount <= 0);
                Check(unbilled == 0, $"every session billed ({unbilled} without an amount)");
                var badDebits = sessions.Count(s => debits.GetValueOrDefault(s.ID) != 1);
                Check(badDebits == 0, $"one wallet debit per session ({badDebits} sessions with none or several)");
                Console.WriteLine($"[load] {sessions.Count} sessions, {sessions.Sum(s => s.ChargedKwhs ?? 0):0.#} kWh, {messages} OCPP messages logged");

                var errors = api.LoggedErrors();
                return Summarize(workspace, options, perVersion, connectTime, before, after, window, samples, probes, sessions.Count,
                    sessions.Sum(s => s.ChargedKwhs ?? 0), messages, errors, checks);
            }
        }
        finally
        {
            foreach (var fleet in fleets)
                await fleet.DisposeAsync();
        }
    }

    private static async Task<FleetStats> WaitForAsync(List<SimFleet> fleets, Func<FleetStats, bool> done, TimeSpan limit, CancellationToken ct)
    {
        var until = DateTime.UtcNow + limit;
        while (true)
        {
            var stats = FleetStats.Sum(await Task.WhenAll(fleets.Select(f => f.StatsAsync(ct))));
            if (done(stats) || DateTime.UtcNow > until)
                return stats;
            await Task.Delay(1000, ct);
        }
    }

    /// <summary>Every 5 s: fleet counters, API CPU and memory, response times of a driver polling api/my/sessions/active (once a second).</summary>
    private static async Task<List<Sample>> MeasureAsync(ApiProcess api, List<SimFleet> fleets, SeedDriver driver, TimeSpan duration,
        CancellationToken ct)
    {
        using var client = await VoltaXClient.SignInAsync(api.BaseUrl, driver.Email, driver.Password, ct);
        var samples = new List<Sample>();
        var clock = Stopwatch.StartNew();
        var cpuBefore = api.Process.TotalProcessorTime;
        var cpuClock = Stopwatch.StartNew();
        var latencies = new List<double>();
        var probeErrors = 0;
        while (clock.Elapsed < duration)
        {
            var probe = Stopwatch.StartNew();
            try
            {
                if (await client.CurrentTransactionUidAsync(ct) == null)
                    probeErrors++;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                probeErrors++;
            }
            latencies.Add(probe.Elapsed.TotalMilliseconds);

            if (latencies.Count >= 5 || clock.Elapsed >= duration)
            {
                api.Process.Refresh();
                var cpu = api.Process.TotalProcessorTime;
                var cpuPercent = (cpu - cpuBefore).TotalMilliseconds / cpuClock.Elapsed.TotalMilliseconds / Environment.ProcessorCount * 100;
                cpuBefore = cpu;
                cpuClock.Restart();
                var fleet = FleetStats.Sum(await Task.WhenAll(fleets.Select(f => f.StatsAsync(ct))));
                latencies.Sort();
                var sample = new Sample(clock.Elapsed, fleet, cpuPercent, api.Process.WorkingSet64 / (1024 * 1024),
                    Percentile(latencies, 0.5), Percentile(latencies, 0.95), probeErrors);
                samples.Add(sample);
                if (samples.Count % 6 == 0)
                    Console.WriteLine($"[load] {sample.At.TotalSeconds,5:0}s  charging {fleet.Charging}  sent {fleet.Sent}  rtt {fleet.AvgRttMs} ms  " +
                        $"api cpu {cpuPercent:0}%  {sample.WorkingSetMb} MB  poll p95 {sample.ProbeP95Ms:0} ms");
                latencies.Clear();
                probeErrors = 0;
            }
            var wait = TimeSpan.FromSeconds(1) - probe.Elapsed;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, ct);
        }
        return samples;
    }

    private static double Percentile(List<double> sorted, double p) =>
        sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(p * sorted.Count) - 1)];

    private static int Summarize(Workspace workspace, RunnerOptions options, List<(string Version, int Count)> perVersion, TimeSpan connectTime,
        FleetStats before, FleetStats after, double windowSeconds, List<Sample> samples, List<Sample> probes, int sessions, double kwh,
        int messages, List<(string Message, int Count)> errors, List<(bool Ok, string Text)> checks)
    {
        string F(double v, string format = "0") => v.ToString(format, CultureInfo.InvariantCulture);
        var text = new StringBuilder();
        text.AppendLine($"# VoltaX load run ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)");
        text.AppendLine();
        text.AppendLine($"{string.Join(" + ", perVersion.Select(p => $"{p.Count} x OCPP {p.Version}"))} chargers, MeterValueSampleInterval " +
            $"{options.MeterIntervalSeconds} s, {F(options.Minutes, "0.#")} min measured, {options.StaggerMs} ms between connections, SQLite.");
        text.AppendLine();
        text.AppendLine("| Measure | Value |");
        text.AppendLine("|---|---|");
        text.AppendLine($"| All chargers accepted after | {F(connectTime.TotalSeconds, "0.0")} s |");
        text.AppendLine($"| Messages sent by the chargers in the window | {after.Sent - before.Sent} ({F((after.Sent - before.Sent) / windowSeconds, "0.0")}/s) |");
        text.AppendLine($"| Average charger round trip (whole run) | {after.AvgRttMs} ms |");
        if (probes.Count > 0)
            text.AppendLine($"| Active-session poll (api/my/sessions/active) p50 / p95 / worst 5 s p95 | {F(probes.Average(s => s.ProbeP50Ms))} / {F(probes.Average(s => s.ProbeP95Ms))} / {F(probes.Max(s => s.ProbeP95Ms))} ms |");
        if (samples.Count > 0)
        {
            text.AppendLine($"| API CPU average / peak (all cores) | {F(samples.Average(s => s.CpuPercent))} / {F(samples.Max(s => s.CpuPercent))} % |");
            text.AppendLine($"| API working set at the end / peak | {samples[^1].WorkingSetMb} / {samples.Max(s => s.WorkingSetMb)} MB |");
        }
        text.AppendLine($"| Sessions / energy | {sessions} / {F(kwh, "0.#")} kWh |");
        text.AppendLine($"| OCPP messages logged by the API | {messages} |");
        text.AppendLine($"| API errors logged | {errors.Sum(e => e.Count)} |");
        text.AppendLine();
        text.AppendLine("## Checks");
        foreach (var (ok, check) in checks)
            text.AppendLine($"- {(ok ? "PASS" : "FAIL")} {check}");
        if (errors.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## API errors (by message)");
            foreach (var (message, count) in errors.Take(20))
                text.AppendLine($"- {count} x {message}");
        }

        var csv = new StringBuilder("seconds,charging,sent,errors,reconnects,queued,avgRttMs,apiCpuPercent,apiWorkingSetMb,pollP50Ms,pollP95Ms,pollErrors\n");
        foreach (var s in samples)
            csv.AppendLine(string.Join(',', F(s.At.TotalSeconds), s.Fleet.Charging, s.Fleet.Sent, s.Fleet.Errors, s.Fleet.Reconnects,
                s.Fleet.Queued, s.Fleet.AvgRttMs, F(s.CpuPercent, "0.0"), s.WorkingSetMb, F(s.ProbeP50Ms, "0.0"), F(s.ProbeP95Ms, "0.0"), s.ProbeErrors));
        File.WriteAllText(Path.Combine(workspace.OutDir, "load-samples.csv"), csv.ToString());

        var summary = text.ToString();
        File.WriteAllText(Path.Combine(workspace.OutDir, "summary.md"), summary);
        Console.WriteLine();
        Console.WriteLine(summary);
        var failed = checks.Count(c => !c.Ok);
        Console.WriteLine(failed == 0
            ? $"[load] PASSED: {checks.Count} checks (samples in load-samples.csv)"
            : $"[load] FAILED: {failed} of {checks.Count} checks (details above and in {workspace.OutDir})");
        return failed == 0 ? 0 : 1;
    }
}
