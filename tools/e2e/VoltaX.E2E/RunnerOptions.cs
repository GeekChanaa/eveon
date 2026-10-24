using System.Globalization;

namespace VoltaX.E2E;

/// <summary>The run cannot start (missing simulator, API that does not come up, ...): exit code 2.</summary>
public sealed class SetupException(string message) : Exception(message);

public sealed class RunnerOptions
{
    public const string Usage = """
        VoltaX end-to-end runner: EVEON_SIM against a local API on a throwaway SQLite database.

          dotnet run -c Release --project tools/e2e/VoltaX.E2E -- [e2e] [options]
          dotnet run -c Release --project tools/e2e/VoltaX.E2E -- load [options]

        Common options
          --sim <dir>          EVEON_SIM checkout (default: $EVEON_SIM_DIR, else an EVEON_SIM folder next to the
                               VoltaX checkout or one of its parents)
          --out <dir>          work folder for the database, logs and reports (default: tools/e2e/out/<run>)
          --no-sim-build       do not run `npm ci` / `npm run sim:build` in the simulator
          --version <v>        only OCPP 1.6 or 2.0.1 (default: both)

        e2e
          --only <text>        only the presets whose name contains <text> (case-insensitive)
          --parallel <n>       simulator runs at the same time (default 8)

        load
          --chargers <n>       chargers (default 200), half 1.6 and half 2.0.1 unless --version is given
          --minutes <n>        measured duration (default 5)
          --interval <s>       MeterValueSampleInterval of the chargers in simulated seconds (default 60)
          --stagger <ms>       delay between two connections (default 50)
        """;

    public string Command { get; private set; } = "e2e";
    public bool Help { get; private set; }
    public string? SimDir { get; private set; }
    public string? OutDir { get; private set; }
    public bool BuildSim { get; private set; } = true;
    public string? OnlyVersion { get; private set; }
    public string? Only { get; private set; }
    public int Parallel { get; private set; } = 8;
    public int Chargers { get; private set; } = 200;
    public double Minutes { get; private set; } = 5;
    public int MeterIntervalSeconds { get; private set; } = 60;
    public int StaggerMs { get; private set; } = 50;

    public IEnumerable<string> Versions =>
        OnlyVersion == null ? new[] { "1.6", "2.0.1" } : new[] { OnlyVersion };

    public static RunnerOptions Parse(string[] args)
    {
        var options = new RunnerOptions();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new SetupException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "e2e" or "load" when i == 0: options.Command = args[i]; break;
                case "-h" or "--help": options.Help = true; break;
                case "--sim": options.SimDir = Value(); break;
                case "--out": options.OutDir = Value(); break;
                case "--no-sim-build": options.BuildSim = false; break;
                case "--version":
                    options.OnlyVersion = Value() switch
                    {
                        "1.6" or "16" => "1.6",
                        "2.0.1" or "201" => "2.0.1",
                        var v => throw new SetupException($"--version {v}: use 1.6 or 2.0.1")
                    };
                    break;
                case "--only": options.Only = Value(); break;
                case "--parallel": options.Parallel = Math.Max(1, int.Parse(Value(), CultureInfo.InvariantCulture)); break;
                case "--chargers": options.Chargers = Math.Max(1, int.Parse(Value(), CultureInfo.InvariantCulture)); break;
                case "--minutes": options.Minutes = double.Parse(Value(), CultureInfo.InvariantCulture); break;
                case "--interval": options.MeterIntervalSeconds = Math.Max(1, int.Parse(Value(), CultureInfo.InvariantCulture)); break;
                case "--stagger": options.StaggerMs = Math.Max(0, int.Parse(Value(), CultureInfo.InvariantCulture)); break;
                default: throw new SetupException($"Unknown argument {args[i]}\n\n{Usage}");
            }
        }
        return options;
    }
}
