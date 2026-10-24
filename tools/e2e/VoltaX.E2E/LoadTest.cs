namespace VoltaX.E2E;

public static class LoadTest
{
    public static Task<int> RunAsync(Workspace workspace, RunnerOptions options, CancellationToken ct) =>
        throw new SetupException("load: not implemented yet");
}
