using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace VoltaX.E2E;

/// <summary>
/// The real API (the VoltaXApi.dll built with this runner) as a child process on a free local port, configured only
/// through environment variables: SQLite database, throwaway signing key, no mail server, no payments, no OCPI, plain
/// ws:// charger connections with Basic auth (security profile 1).
/// </summary>
public sealed class ApiProcess : IAsyncDisposable
{
    private readonly Process _process;

    private ApiProcess(Process process, int port, string logFile)
    {
        _process = process;
        Port = port;
        LogFile = logFile;
    }

    public int Port { get; }
    public string LogFile { get; }
    public string BaseUrl => $"http://127.0.0.1:{Port}";
    public string OcppUrl => $"ws://127.0.0.1:{Port}/OCPP";
    public Process Process => _process;

    public static IDictionary<string, string?> Environment(string databaseFile) => new Dictionary<string, string?>
    {
        ["ASPNETCORE_ENVIRONMENT"] = "E2E",
        ["DOTNET_ENVIRONMENT"] = "E2E",
        ["Database__Provider"] = "Sqlite",
        ["ConnectionStrings__DefaultConnection"] = $"Data Source={databaseFile};Default Timeout=60",
        ["Database__InitializeRefreshTokens"] = "false",
        ["AppSettings__Token"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(48)),
        ["Authentication__Google__ClientSecret"] = "e2e-not-a-secret",
        ["AllowedHosts"] = "127.0.0.1;localhost",
        ["Cors__AllowedOrigins__0"] = "http://localhost",
        ["FirstSetup__Enabled"] = "false",
        ["Security__HttpsRedirection"] = "false",
        ["Ocpp__AllowInsecureProfile1"] = "true",
        ["Payments__Provider"] = "",
        ["Ocpi__Enabled"] = "false",
        // Nothing listens there: mails fail at once and are logged, nobody receives anything.
        ["MailSettings__Host"] = "127.0.0.1",
        ["MailSettings__Port"] = "2",
        ["MailSettings__Mail"] = "e2e@example.invalid",
        ["MailSettings__Password"] = "none",
        ["AwsSns__AccessKey"] = "",
        ["AwsSns__SecretKey"] = "",
        ["RateLimiting__AuthStrictPerMinute"] = "100000",
        ["RateLimiting__PublicPerMinute"] = "100000",
        ["RateLimiting__GlobalPerMinute"] = "1000000",
        ["GlobalConfigurations__vat"] = Tariff.VatRate.ToString(CultureInfo.InvariantCulture),
        ["GlobalConfigurations__gracePeriod"] = Tariff.GracePeriodSeconds.ToString(CultureInfo.InvariantCulture),
        ["GlobalConfigurations__defaultLowBalanceThresholdBuffer"] = "0",
        ["GlobalConfigurations__defaultMinimumRequiredBalance"] = "0",
        ["Serilog__MinimumLevel__Default"] = "Information",
    };

    public static async Task<ApiProcess> StartAsync(Workspace workspace, string databaseFile, CancellationToken ct)
    {
        var port = Run.FreeTcpPort();
        var workDir = Directory.CreateDirectory(Path.Combine(workspace.OutDir, "api")).FullName;
        var logFile = Path.Combine(workspace.OutDir, "api.log");
        // A copy of the build output, so the run never locks VoltaXApi/bin (rebuilds and tests go on meanwhile).
        var binaries = Path.Combine(workspace.OutDir, "api-bin");
        CopyDirectory(Path.GetDirectoryName(workspace.ApiDll)!, binaries);
        // The project folder, as with `dotnet run`: appsettings.json, wwwroot and the mail templates live there.
        var contentRoot = Path.Combine(workspace.Root, "VoltaXApi");
        var process = Run.Start("dotnet",
            new[] { Path.Combine(binaries, Path.GetFileName(workspace.ApiDll)), "--urls", $"http://127.0.0.1:{port}", "--contentRoot", contentRoot },
            workDir, logFile, Environment(databaseFile));
        var api = new ApiProcess(process, port, logFile) { _binaries = binaries };

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var until = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < until)
        {
            ct.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                var code = process.ExitCode;
                await api.DisposeAsync();
                throw new SetupException($"The API exited at start up (code {code}):\n{Run.Tail(logFile)}");
            }
            try
            {
                if ((await http.GetAsync($"{api.BaseUrl}/health/ready", ct)).IsSuccessStatusCode)
                    return api;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { /* slow start */ }
            await Task.Delay(500, ct);
        }
        await api.DisposeAsync();
        throw new SetupException($"The API did not become ready in 2 minutes:\n{Run.Tail(logFile)}");
    }

    private string? _binaries;

    /// <summary>
    /// The ERR entries of the API log, counted by message (first line, without its time). Not all are faults: EF Core
    /// also logs the unique-key collision that the invoice numbering retries.
    /// </summary>
    public List<(string Message, int Count)> LoggedErrors() =>
        Run.Tail(LogFile, int.MaxValue).Split('\n')
            .Where(l => l.Length > 15 && l[0] == '[' && l.AsSpan(10, 4).SequenceEqual("ERR]"))
            .Select(l => l[15..].TrimEnd('\r'))
            .GroupBy(m => m.Length > 160 ? m[..160] : m)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(g => g.Item2)
            .ToList();

    private static void CopyDirectory(string from, string to)
    {
        foreach (var directory in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, directory)));
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)), overwrite: true);
    }

    public async ValueTask DisposeAsync()
    {
        Run.Kill(_process);
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).ContinueWith(_ => { });
        _process.Dispose();
        if (_binaries != null)
            try { Directory.Delete(_binaries, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
