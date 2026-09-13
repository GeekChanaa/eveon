using System.Security.Claims;
using Serilog.Context;

namespace VoltaXApi.Middlewares;

/// <summary>
/// Attaches a correlation id — and the caller, once authentication has run — to every
/// log line produced while handling a request.
///
/// Without this, a report like "it failed around 14:20" means reading interleaved lines
/// from every concurrent request. With it, one grep on the correlation id returns the
/// whole story of a single call, across services and background work started from it.
///
/// The id is taken from the incoming <c>X-Correlation-ID</c> header when the caller sends
/// one (so a chain of calls shares it) and echoed back on the response either way.
/// </summary>
public class RequestContextLoggingMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public RequestContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        string correlationId = GetCorrelationId(context);

        // Echoed before the response starts: adding a header later throws.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserID", ResolveUserId(context)))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// The signed in user's id, or null for an anonymous call. Public because the request
    /// logging summary reports the same value.
    /// </summary>
    public static string? ResolveUserId(HttpContext context)
        => context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private static string GetCorrelationId(HttpContext context)
    {
        string? incoming = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        // A caller supplied value is trusted only as a label; cap it so it cannot be used
        // to blow up log lines.
        if (!string.IsNullOrWhiteSpace(incoming))
            return incoming.Length <= 64 ? incoming : incoming.Substring(0, 64);

        return context.TraceIdentifier;
    }
}

public static class RequestContextLoggingMiddlewareExtensions
{
    /// <summary>
    /// Register before the request logging and the endpoints, but after authentication,
    /// so the user claims are already available.
    /// </summary>
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestContextLoggingMiddleware>();
}
