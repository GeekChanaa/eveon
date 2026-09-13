using Serilog;
using Serilog.Events;
using VoltaXApi.Middlewares;

namespace VoltaXApi.Configurations;

/// <summary>
/// Logging setup for the whole application.
///
/// Serilog sits behind the standard <see cref="ILogger{TCategoryName}"/>, so nothing in
/// the code base needs to know it is there: controllers, services and handlers keep
/// injecting <c>ILogger&lt;T&gt;</c>.
///
/// Where the log ends up is decided by the "Serilog" section of appsettings, not by this
/// file. Today that is a rolling text file plus the console. Adding another destination
/// later (Seq, Application Insights, Elastic, Datadog...) is a package reference and one
/// more entry under "Serilog:WriteTo" — no code change here.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>Name every log line is stamped with, so a shared sink can tell the app apart.</summary>
    private const string ApplicationName = "VoltaXApi";

    public static void ConfigureLogging(WebApplicationBuilder builder)
    {
        var loggerConfiguration = new LoggerConfiguration()
            // Sinks, levels and overrides come from configuration. Anything set in code
            // below is a default the configuration can still override.
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);

        Log.Logger = loggerConfiguration.CreateLogger();

        // A sink that cannot start (unwritable path, bad connection string) fails
        // silently otherwise, which is the worst possible failure for a logger.
        if (builder.Environment.IsDevelopment())
            Serilog.Debugging.SelfLog.Enable(Console.Error);

        // Replaces the default providers: every ILogger<T> in the app now writes through
        // Serilog, and Serilog is disposed (and flushed) with the host.
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog(Log.Logger, dispose: true);
    }

    /// <summary>
    /// One summary line per HTTP request instead of the three the framework emits, with
    /// the status code, the duration and who made the call.
    /// </summary>
    public static void ConfigureRequestLogging(WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = GetRequestLevel;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());

                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserID", RequestContextLoggingMiddleware.ResolveUserId(httpContext));
                    diagnosticContext.Set("UserRole", httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
                }
            };
        });
    }

    /// <summary>
    /// Keeps the noise down: polling endpoints stay at Debug, failures move up. A 4xx is
    /// the caller's problem (Warning), a 5xx is ours (Error).
    /// </summary>
    private static LogEventLevel GetRequestLevel(HttpContext httpContext, double elapsedMs, Exception? exception)
    {
        if (exception != null)
            return LogEventLevel.Error;

        int statusCode = httpContext.Response.StatusCode;

        if (statusCode >= 500)
            return LogEventLevel.Error;

        if (statusCode >= 400)
            return LogEventLevel.Warning;

        string path = httpContext.Request.Path.Value ?? string.Empty;

        bool isNoisy = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/StaticFiles", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);

        return isNoisy ? LogEventLevel.Debug : LogEventLevel.Information;
    }
}
