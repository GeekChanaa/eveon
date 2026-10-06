using Microsoft.Extensions.Diagnostics.HealthChecks;
using VoltaXApi.Data;

namespace VoltaXApi.ScaleOut;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
        return await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database cannot be reached.");
    }
}

/// <summary>Degraded, not unhealthy, while Redis is down: the instance still serves its own chargers.</summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly RedisConnection _redis;

    public RedisHealthCheck(RedisConnection redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await (await _redis.GetAsync()).GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis ping {latency.TotalMilliseconds:0} ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Redis is unreachable: cross-instance commands and SignalR fan-out are unavailable.", ex);
        }
    }
}
