using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace VoltaXApi.Configurations;

public static class SecurityConfiguration
{
    public const string CorsPolicy = "CorsPolicy";
    public const string AuthStrictRateLimit = "auth-strict";
    public const string PublicRateLimit = "public";

    // Settings that must come from user-secrets / environment variables. Startup fails fast
    // when one is missing instead of running with an empty key or connection string.
    private static readonly string[] RequiredSettings =
    {
        "AppSettings:Token",
        "ConnectionStrings:DefaultConnection",
    };

    public static void AddSecurity(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ValidateRequiredSettings(configuration, environment);
        AddCors(services, configuration, environment);
        AddRateLimiting(services, configuration);
        AddForwardedHeaders(services, configuration);

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
        });
    }

    public static void UseSecurity(WebApplication app)
    {
        // Must run first so scheme, client IP (rate limiting, audit, logs) and host are the
        // ones the client used, not the reverse proxy's.
        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            // Health probes (Railway, load balancers) call over plain HTTP from inside the platform
            // and need a 200, not a redirect.
            if (app.Configuration.GetValue("Security:HttpsRedirection", true))
                app.UseWhen(context => !context.Request.Path.StartsWithSegments("/health"),
                    branch => branch.UseHttpsRedirection());
        }

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // The API serves JSON, files and PDFs, never pages that need scripts.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            await next();
        });
    }

    public static void UseRateLimiting(WebApplication app)
    {
        app.UseRateLimiter();
    }

    private static void ValidateRequiredSettings(IConfiguration configuration, IHostEnvironment environment)
    {
        var missing = RequiredSettings.Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToList();
        if (missing.Count > 0)
        {
            var hint = environment.IsDevelopment()
                ? "Set them with `dotnet user-secrets set <key> <value>` in VoltaXApi."
                : "Set them as environment variables (use __ instead of :, e.g. AppSettings__Token).";
            throw new InvalidOperationException($"Missing required configuration: {string.Join(", ", missing)}. {hint}");
        }

        if (configuration["AppSettings:Token"]!.Length < 64)
            throw new InvalidOperationException("AppSettings:Token must be at least 64 characters long.");

        if (!environment.IsDevelopment())
        {
            var hosts = configuration["AllowedHosts"];
            if (string.IsNullOrWhiteSpace(hosts) || hosts.Trim() == "*")
                throw new InvalidOperationException("AllowedHosts must list the API host names in production (e.g. AllowedHosts=api.eveon.ma).");
        }
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (origins.Length == 0 && !environment.IsDevelopment())
            throw new InvalidOperationException("Cors:AllowedOrigins must list the dashboard origins in production (e.g. Cors__AllowedOrigins__0=https://app.eveon.ma).");

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, builder => builder
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition"));
        });
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var strictPerMinute = configuration.GetValue("RateLimiting:AuthStrictPerMinute", 5);
        var publicPerMinute = configuration.GetValue("RateLimiting:PublicPerMinute", 60);
        var globalPerMinute = configuration.GetValue("RateLimiting:GlobalPerMinute", 600);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                return ValueTask.CompletedTask;
            };

            // Login, OTP, password reset, 2FA: per client IP and per endpoint, so one
            // brute-force target does not lock other flows for the same user.
            options.AddPolicy(AuthStrictRateLimit, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    $"{ClientIp(context)}|{context.Request.Path.Value?.ToLowerInvariant()}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = strictPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(PublicRateLimit, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = publicPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            // Safety net for every request: per signed-in user, else per IP. Charger
            // websockets (/ocpp) and SignalR hubs are long lived and excluded.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path;
                if (path.StartsWithSegments("/ocpp") || context.WebSockets.IsWebSocketRequest)
                    return RateLimitPartition.GetNoLimiter("websocket");

                var user = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var key = user != null ? $"user:{user}" : $"ip:{ClientIp(context)}";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = globalPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                });
            });
        });
    }

    private static void AddForwardedHeaders(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = 1;

            // Only trust X-Forwarded-* from the reverse proxy. Configure its address(es) with
            // ReverseProxy:KnownProxies; without it only loopback is trusted (the default).
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
            {
                if (IPAddress.TryParse(proxy, out var address))
                    options.KnownProxies.Add(address);
            }

            // Managed platforms (Railway) put an edge proxy with no fixed address in front of the
            // container, and it is the only way in. Trust it; ForwardLimit = 1 still means only the
            // entry it appended is used, so a client cannot spoof its IP or scheme.
            if (configuration.GetValue("ReverseProxy:TrustAllProxies", false))
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }

            var allowedHosts = configuration["AllowedHosts"];
            if (!string.IsNullOrWhiteSpace(allowedHosts) && allowedHosts.Trim() != "*")
                options.AllowedHosts = allowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        });
    }

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
