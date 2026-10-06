using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class CardExpirationWarningService : BackgroundService
    {
        private readonly ILogger<CardExpirationWarningService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBusinessClock _clock;
        private readonly IClusterJobLease _lease;
        private readonly CardExpirationSettings _settings;

        public CardExpirationWarningService(
            ILogger<CardExpirationWarningService> logger,
            IOptions<CardExpirationSettings> options,
            IServiceScopeFactory scopeFactory,
            IBusinessClock clock,
            IClusterJobLease lease)
        {
            _lease = lease;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clock = clock;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Card Expiration Warning Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // CheckTime is a time of day in the business time zone.
                var nextRunLocal = _clock.Today.Add(_settings.CheckTime);
                if (_clock.LocalNow > nextRunLocal)
                    nextRunLocal = nextRunLocal.AddDays(1);

                var nextRunUtc = _clock.StartOfDayUtc(nextRunLocal.Date).Add(nextRunLocal.TimeOfDay);
                var delay = nextRunUtc - _clock.UtcNow;
                _logger.LogInformation("Next card expiration check scheduled at {NextRunUtc:o} (UTC)", nextRunUtc);

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (await _lease.TryAcquireAsync("card-expiration-warnings", TimeSpan.FromHours(23), stoppingToken))
                    {
                        await ProcessCardExpirations(stoppingToken);
                        _logger.LogInformation("Card expiration check completed successfully.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error occurred while checking card expirations.");
                }

                // Add a small delay to prevent tight loop if there's an issue with time calculation
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessCardExpirations(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
            var cardExpirationNotificationRepository = scope.ServiceProvider.GetRequiredService<ICardExpirationNotificationRepository>();

            _logger.LogInformation("Starting card expiration check...");

            var today = _clock.Today;
            foreach (var interval in _settings.WarningIntervals.Where(i => i.Enabled))
            {
                // Cards whose whole-day distance to expiration, counted from the start of the business day
                // (TimeSpan.Days truncates toward zero), equals the interval.
                var (fromUtc, toUtc) = ExpirationWindow(today, interval.DaysBeforeExpiration);
                var cards = await db.Cards.AsNoTracking()
                    .Include(c => c.User)
                    .Where(c => c.ExpirationDate > fromUtc && c.ExpirationDate < toUtc)
                    .ToListAsync(cancellationToken);

                foreach (var card in cards)
                {
                    if (string.IsNullOrWhiteSpace(card.User?.Email))
                        continue;

                    if (await cardExpirationNotificationRepository.HasNotificationBeenSentAsync(card.ID, interval.Name))
                    {
                        _logger.LogInformation("Skipping {Interval} notification for card {CardId}: already sent", interval.Name, card.ID);
                        continue;
                    }

                    _logger.LogInformation("Sending {Interval} expiration warning for card {CardId}", interval.Name, card.ID);
                    var mailRequest = new MailRequest
                    {
                        Name = "VoltaX Card Expiration Card",
                        ToEmails = new List<string> { card.User.Email },
                        Subject = "VoltaX Card Expiration"
                    };
                    await mailService.SendWarningEmail(mailRequest, card.User.FullName);
                    await cardExpirationNotificationRepository.CreateCardExpirationNotification(card.ID, interval.Name);
                }
            }
        }

        /// <summary>
        /// Exclusive UTC bounds of the expiration instants for which (expiration - start of today).Days == days.
        /// </summary>
        private (DateTime FromUtc, DateTime ToUtc) ExpirationWindow(DateTime today, int days)
        {
            var startOfToday = _clock.StartOfDayUtc(today);
            if (days > 0)
                return (startOfToday.AddDays(days).AddTicks(-1), startOfToday.AddDays(days + 1));
            if (days < 0)
                return (startOfToday.AddDays(days - 1), startOfToday.AddDays(days).AddTicks(1));
            return (startOfToday.AddDays(-1), startOfToday.AddDays(1));
        }
    }
}
