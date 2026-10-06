using StackExchange.Redis;

namespace VoltaXApi.ScaleOut;

public static class ScaleOutRegistration
{
    /// <summary>
    /// Redis mode (ScaleOut:Redis:ConnectionString set): SignalR backplane, Redis IDistributedCache, Redis registry and bus.
    /// Otherwise everything stays in process, which is the single-instance behaviour.
    /// </summary>
    public static void AddScaleOut(IServiceCollection services, IConfiguration configuration)
    {
        var settings = ScaleOutSettings.FromConfiguration(configuration);
        services.AddSingleton(settings);

        var health = services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

        if (settings.UsesRedis)
        {
            var redis = new RedisConnection(settings.RedisConnectionString!);
            services.AddSingleton(redis);
            services.AddSignalR().AddStackExchangeRedis(options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal(ScaleOutSettings.ChannelPrefix);
                options.ConnectionFactory = async _ => await redis.GetAsync();
            });
            services.AddStackExchangeRedisCache(options =>
            {
                options.InstanceName = ScaleOutSettings.ChannelPrefix + ":cache:";
                options.ConnectionMultiplexerFactory = () => redis.GetAsync();
            });
            services.AddSingleton<IScaleOutBus, RedisScaleOutBus>();
            services.AddSingleton<IChargerConnectionRegistry, RedisChargerConnectionRegistry>();
            services.AddSingleton<IClusterJobLease, RedisClusterJobLease>();
            health.AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" });
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<IScaleOutBus, InMemoryScaleOutBus>();
            services.AddSingleton<IChargerConnectionRegistry, InMemoryChargerConnectionRegistry>();
            services.AddSingleton<IClusterJobLease, InProcessClusterJobLease>();
        }

        services.AddSingleton<ISecurityStateStore, DistributedCacheSecurityStateStore>();
        services.AddSingleton<RemoteOcppCommandClient>();
        services.AddSingleton<ChargerOwnershipTracker>();
        services.AddHostedService<ScaleOutCoordinator>();
    }
}
