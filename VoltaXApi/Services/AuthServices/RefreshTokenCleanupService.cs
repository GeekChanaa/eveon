using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Settings;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Prunes refresh tokens that can no longer be spent. Without it the table only ever
    /// grows: every sign in and every refresh adds a row.
    ///
    /// Rows are kept for <see cref="AuthTokenSettings.CleanupRetentionDays"/> after they
    /// expire or get revoked, which leaves a window to investigate a reuse alert.
    /// </summary>
    public class RefreshTokenCleanupService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

        private readonly IServiceProvider _serviceProvider;
        private readonly AuthTokenSettings _settings;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(
            IServiceProvider serviceProvider,
            IOptions<AuthTokenSettings> settings,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // The repository is scoped, so it cannot be injected into a singleton.
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

                    int removed = await repository.DeleteExpired(_settings.CleanupRetention);

                    if (removed > 0)
                        _logger.LogInformation("Refresh token cleanup removed {Count} stale tokens", removed);
                }
                catch (Exception ex)
                {
                    // A failed sweep must never take the host down; the next pass retries.
                    _logger.LogError(ex, "Refresh token cleanup failed");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}
