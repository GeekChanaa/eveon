using VoltaX.E2E;

// End-to-end matrix (default) or load test of the EVEON simulator against a local API on a throwaway database.
// Exit code: 0 passed, 1 a check failed, 2 the run could not be set up.
var options = RunnerOptions.Parse(args);
if (options.Help)
{
    Console.WriteLine(RunnerOptions.Usage);
    return 0;
}

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancel.Cancel();
};

try
{
    var workspace = Workspace.Locate(options);
    return options.Command == "load"
        ? await LoadTest.RunAsync(workspace, options, cancel.Token)
        : await EndToEnd.RunAsync(workspace, options, cancel.Token);
}
catch (SetupException ex)
{
    Console.Error.WriteLine($"[e2e] {ex.Message}");
    return 2;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("[e2e] cancelled");
    return 2;
}
