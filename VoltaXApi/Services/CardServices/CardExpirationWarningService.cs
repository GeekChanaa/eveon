using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class CardExpirationWarningService : BackgroundService
    {
        private readonly ILogger<CardExpirationWarningService> _logger;
        private readonly  IServiceScopeFactory _scopeFactory;
        private readonly CardExpirationSettings _settings;

        public CardExpirationWarningService(
            ILogger<CardExpirationWarningService> logger,
            IOptions<CardExpirationSettings> options,
             IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Card Expiration Warning Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate the time until next check
                var now = DateTime.Now;
                var nextRunTime = now.Date.Add(_settings.CheckTime);

                if (now > nextRunTime)
                {
                    nextRunTime = nextRunTime.AddDays(1);
                }

                var delay = nextRunTime - now;
                _logger.LogInformation($"Next card expiration check scheduled at: {nextRunTime}");

                // Delay until next check time
                await Task.Delay(delay, stoppingToken);
                
                // Run the check
                try
                {
                    await ProcessCardExpirations();
                    _logger.LogInformation("Card expiration check completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking card expirations.");
                }

                // Add a small delay to prevent tight loop if there's an issue with time calculation
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessCardExpirations()
        {
            // Create a new scope for each execution
            using (var scope = _scopeFactory.CreateScope())
            {
                // Get services from the scope
                var cardRepository = scope.ServiceProvider.GetRequiredService<ICardRepository>();
                var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
                var cardExpirationNotificationRepository = scope.ServiceProvider.GetRequiredService<ICardExpirationNotificationRepository>();
                
                _logger.LogInformation("Starting card expiration check...");

                // Get cards that need notification
                var cards = await cardRepository.GetAllCards();
                var today = DateTime.Today;

                foreach (var card in cards)
                {
                    // Calculate days until expiration
                    var daysUntilExpiration = (card.ExpirationDate - today).Days;

                    // Check against each warning interval
                    foreach (var interval in _settings.WarningIntervals.Where(i => i.Enabled))
                    {
                        if (daysUntilExpiration == interval.DaysBeforeExpiration)
                        {
                            // Check if notification has already been sent for this interval
                            bool alreadySent = await cardExpirationNotificationRepository.HasNotificationBeenSentAsync(card.ID, interval.Name);
                            
                            if (!alreadySent)
                            {
                                _logger.LogInformation($"Sending {interval.Name} expiration warning for card ending with {card.LastFourDigits}");
                                

                                // Send the email
                                MailRequest mailRequest = new()
                                {
                                    Name = "VoltaX Card Expiration Card",
                                    ToEmails = new List<string> { card.User.Email},
                                    Subject = "VoltaX Card Expiration"
                                };
                                await mailService.SendWarningEmail(mailRequest, card.User.FullName);
                                // Record that the notification was sent
                                await cardExpirationNotificationRepository.CreateCardExpirationNotification(card.ID, interval.Name);
                            }
                            else
                            {
                                _logger.LogInformation($"Skipping {interval.Name} notification for card {card.LastFourDigits} as it was already sent");
                            }
                        }
                    }
                }
            }
        }
    }
}