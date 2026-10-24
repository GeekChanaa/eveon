using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace VoltaX.Tests.Infrastructure;

public static class RepoPaths
{
    /// <summary>The VoltaXApi project folder, found by walking up from the test binaries.</summary>
    public static string Api { get; } = FindApi();

    private static string FindApi()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "VoltaXApi", "VoltaXApi.csproj")))
            dir = dir.Parent;
        return dir == null
            ? throw new DirectoryNotFoundException("VoltaXApi not found above " + AppContext.BaseDirectory)
            : Path.Combine(dir.FullName, "VoltaXApi");
    }
}

/// <summary>A hosting environment rooted in the API project (wwwroot assets, Assets/ fonts).</summary>
public sealed class ApiHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.Combine(RepoPaths.Api, "wwwroot");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "VoltaXApi";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = RepoPaths.Api;
    public string EnvironmentName { get; set; } = "Testing";
}
