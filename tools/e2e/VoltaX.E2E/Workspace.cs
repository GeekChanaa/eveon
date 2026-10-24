using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace VoltaX.E2E;

/// <summary>Where things are: the VoltaX checkout, the built API, the simulator and the run's work folder.</summary>
public sealed class Workspace
{
    private Workspace(string root, string simDir, string outDir, string apiDll)
    {
        Root = root;
        SimDir = simDir;
        OutDir = outDir;
        ApiDll = apiDll;
    }

    public string Root { get; }
    public string SimDir { get; }
    public string OutDir { get; }
    public string ApiDll { get; }
    public string SimServerJs => Path.Combine(SimDir, "dist", "sim-server", "sim-server", "server.js");

    public static Workspace Locate(RunnerOptions options)
    {
        var root = FindUp(AppContext.BaseDirectory, d => File.Exists(Path.Combine(d, "VoltaX.sln")) && Directory.Exists(Path.Combine(d, "VoltaXApi")))
            ?? throw new SetupException("VoltaX checkout not found above " + AppContext.BaseDirectory);

        var simDir = options.SimDir ?? Environment.GetEnvironmentVariable("EVEON_SIM_DIR");
        if (string.IsNullOrEmpty(simDir))
        {
            // An EVEON_SIM checkout next to the VoltaX one (or next to one of its parents, for a git worktree).
            var parent = FindUp(root, d => Directory.Exists(Path.Combine(d, "..", "EVEON_SIM", "sim-server")));
            simDir = parent == null ? null : Path.Combine(parent, "..", "EVEON_SIM");
        }
        if (simDir == null || !Directory.Exists(Path.Combine(simDir, "sim-server")))
            throw new SetupException("EVEON_SIM not found: pass --sim <dir> or set EVEON_SIM_DIR (a checkout of the simulator repository).");

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        // Built with this runner (project reference).
        var apiDll = Path.Combine(root, "VoltaXApi", "bin", configuration, "net9.0", "VoltaXApi.dll");
        if (!File.Exists(apiDll))
            throw new SetupException($"{apiDll} not found: build with `dotnet build -c {configuration} tools/e2e/VoltaX.E2E`.");

        var outDir = Path.GetFullPath(options.OutDir ?? Path.Combine(root, "tools", "e2e", "out",
            $"{options.Command}-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}"));
        Directory.CreateDirectory(outDir);
        return new Workspace(Path.GetFullPath(root), Path.GetFullPath(simDir), outDir, apiDll);
    }

    private static string? FindUp(string start, Func<string, bool> match) => FindUp(start, d => match(d) ? d : null);

    private static string? FindUp(string start, Func<string, string?> match)
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (match(dir.FullName) is { } found)
                return found;
        return null;
    }

    /// <summary>`npm ci` when node_modules is missing, then `npm run sim:build`.</summary>
    public async Task BuildSimulatorAsync(CancellationToken ct)
    {
        if (!Directory.Exists(Path.Combine(SimDir, "node_modules")))
            await Run.NpmAsync(SimDir, "ci", Path.Combine(OutDir, "sim-npm-ci.log"), ct);
        await Run.NpmAsync(SimDir, "run sim:build", Path.Combine(OutDir, "sim-build.log"), ct);
        if (!File.Exists(SimServerJs))
            throw new SetupException($"{SimServerJs} missing after the simulator build (see sim-build.log).");
    }
}

/// <summary>Child processes with their output in a log file, killed with their children when the run ends.</summary>
public static class Run
{
    public static Process Start(string fileName, IEnumerable<string> arguments, string workingDirectory, string logFile,
        IDictionary<string, string?>? environment = null)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);
        if (environment != null)
            foreach (var (key, value) in environment)
                psi.Environment[key] = value;

        var log = new StreamWriter(logFile, append: false) { AutoFlush = true };
        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        object gate = new();
        void Write(string? line)
        {
            if (line == null) return;
            lock (gate) log.WriteLine(line);
        }
        process.OutputDataReceived += (_, e) => Write(e.Data);
        process.ErrorDataReceived += (_, e) => Write(e.Data);
        process.Exited += (_, _) => { lock (gate) log.Flush(); };
        if (!process.Start())
            throw new SetupException($"Could not start {fileName}");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    public static async Task<int> WaitAsync(Process process, TimeSpan timeout, CancellationToken ct)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            if (ct.IsCancellationRequested) throw;
            return -1;
        }
        // Lets the asynchronous output readers reach the end of the streams.
        process.WaitForExit();
        return process.ExitCode;
    }

    public static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { /* already gone */ }
    }

    public static async Task NpmAsync(string directory, string arguments, string logFile, CancellationToken ct)
    {
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var process = windows
            ? Start("cmd.exe", new[] { "/c", "npm " + arguments }, directory, logFile)
            : Start("npm", arguments.Split(' '), directory, logFile);
        var code = await WaitAsync(process, TimeSpan.FromMinutes(10), ct);
        if (code != 0)
            throw new SetupException($"`npm {arguments}` in {directory} failed (exit {code}), see {logFile}");
    }

    public static int FreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public static string Tail(string file, int lines = 40)
    {
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var all = reader.ReadToEnd().Split('\n');
            return string.Join('\n', all.TakeLast(lines));
        }
        catch (IOException)
        {
            return "";
        }
    }
}
