using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using VoltaXApi.Settings;

namespace VoltaXApi.Services
{
    /// <summary>Section "MailQueue": how many messages wait at most, and how delivery is retried.</summary>
    public sealed class MailQueueOptions
    {
        public const string SectionName = "MailQueue";

        /// <summary>Messages waiting at most; past it new ones are refused (and logged) until the queue drains.</summary>
        public int Capacity { get; set; } = 1000;
        /// <summary>Delivery attempts per message, the first included.</summary>
        public int MaxAttempts { get; set; } = 4;
        /// <summary>Wait before the second attempt; it doubles for each next one.</summary>
        public double RetryDelaySeconds { get; set; } = 2;
        /// <summary>Time given to the waiting messages when the API stops.</summary>
        public double DrainOnStopSeconds { get; set; } = 10;
    }

    /// <summary>
    /// Outgoing mail waiting for <see cref="MailDeliveryService"/>. <see cref="Enqueue"/> returns at once, so no
    /// HTTP request or OCPP handler waits for an SMTP server. The queue is in memory: messages still waiting when
    /// the process dies are lost (they are logged as such on a normal stop).
    /// </summary>
    public interface IMailQueue
    {
        /// <summary>False when the queue is full or closed; the message is then dropped and logged.</summary>
        bool Enqueue(MimeMessage message);
    }

    public sealed class MailQueue : IMailQueue
    {
        private readonly Channel<MimeMessage> _channel;
        private readonly ILogger<MailQueue> _logger;

        public MailQueue(IOptions<MailQueueOptions> options, ILogger<MailQueue> logger)
        {
            _logger = logger;
            _channel = Channel.CreateBounded<MimeMessage>(new BoundedChannelOptions(Math.Max(1, options.Value.Capacity))
            {
                FullMode = BoundedChannelFullMode.Wait, // TryWrite then fails instead of waiting
                SingleReader = true
            });
        }

        public ChannelReader<MimeMessage> Reader => _channel.Reader;

        public int Count => _channel.Reader.Count;

        public bool Enqueue(MimeMessage message)
        {
            if (string.IsNullOrEmpty(message.MessageId))
                message.MessageId = MimeUtils.GenerateMessageId();
            if (_channel.Writer.TryWrite(message))
                return true;
            _logger.LogError("Mail {MessageId} was dropped: the delivery queue is full or stopped", message.MessageId);
            return false;
        }

        /// <summary>Refuses new messages; the ones already queued can still be read.</summary>
        public void Close() => _channel.Writer.TryComplete();
    }

    /// <summary>Sends one message to the mail server.</summary>
    public interface IMailTransport
    {
        Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
    }

    /// <summary>SMTP delivery with the "MailSettings" account (port 465: TLS from the start; otherwise STARTTLS).</summary>
    public sealed class SmtpMailTransport(IOptions<MailSettings> settings) : IMailTransport
    {
        public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            var mail = settings.Value;
            // Fail in 30 s instead of MailKit's 2 min when the host or port is unreachable.
            using var smtp = new SmtpClient { Timeout = 30_000 };
            // Port 465 (Hostinger's default) speaks TLS from the first byte; 587 / 2525 upgrade with STARTTLS.
            var security = mail.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            try
            {
                await smtp.ConnectAsync(mail.Host, mail.Port, security, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException($"Could not connect to SMTP server {mail.Host}:{mail.Port}", ex);
            }
            var username = string.IsNullOrWhiteSpace(mail.Username) ? mail.Mail : mail.Username;
            await smtp.AuthenticateAsync(username, mail.Password, cancellationToken);
            await smtp.SendAsync(message, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }
    }

    /// <summary>
    /// Delivers the queued mail one message at a time. A failed delivery is retried with a doubling delay
    /// (MailQueue:MaxAttempts, MailQueue:RetryDelaySeconds), except when the server refuses the message for good
    /// (5xx reply, bad address); a message that cannot be delivered is logged and dropped. Recipients are never
    /// logged, only the message id and the recipient count.
    /// </summary>
    public sealed class MailDeliveryService : BackgroundService
    {
        private readonly MailQueue _queue;
        private readonly IMailTransport _transport;
        private readonly MailQueueOptions _options;
        private readonly TimeProvider _time;
        private readonly ILogger<MailDeliveryService> _logger;

        public MailDeliveryService(MailQueue queue, IMailTransport transport, IOptions<MailQueueOptions> options, TimeProvider time,
            ILogger<MailDeliveryService> logger)
        {
            _queue = queue;
            _transport = transport;
            _options = options.Value;
            _time = time;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _queue.Reader.WaitToReadAsync(stoppingToken))
                    while (_queue.Reader.TryRead(out var message))
                        await DeliverAsync(message, Math.Max(1, _options.MaxAttempts), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            await DrainAsync();
        }

        /// <summary>On stop: no new messages, one attempt each for those still waiting, within DrainOnStopSeconds.</summary>
        private async Task DrainAsync()
        {
            _queue.Close();
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(0, _options.DrainOnStopSeconds)), _time);
            while (!budget.IsCancellationRequested && _queue.Reader.TryRead(out var message))
                await DeliverAsync(message, 1, budget.Token);
            if (_queue.Count > 0)
                _logger.LogWarning("{Count} queued mail(s) were not sent before the API stopped", _queue.Count);
        }

        private async Task DeliverAsync(MimeMessage message, int maxAttempts, CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await _transport.SendAsync(message, cancellationToken);
                    if (attempt > 1)
                        _logger.LogInformation("Mail {MessageId} sent on attempt {Attempt}", message.MessageId, attempt);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Mail {MessageId} was not sent: the API is stopping", message.MessageId);
                    return;
                }
                catch (Exception ex) when (IsPermanent(ex) || attempt >= maxAttempts)
                {
                    _logger.LogError(ex, "Mail {MessageId} to {RecipientCount} recipient(s) could not be sent after {Attempt} attempt(s); it is dropped",
                        message.MessageId, message.To.Count + message.Cc.Count + message.Bcc.Count, attempt);
                    return;
                }
                catch (Exception ex)
                {
                    var delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(ex, "Mail {MessageId} attempt {Attempt} of {MaxAttempts} failed; retrying in {Delay}",
                        message.MessageId, attempt, maxAttempts, delay);
                    try
                    {
                        await Task.Delay(delay, _time, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Mail {MessageId} was not sent: the API is stopping", message.MessageId);
                        return;
                    }
                }
            }
        }

        /// <summary>The server refused the message itself (5xx reply): sending it again cannot work.</summary>
        private static bool IsPermanent(Exception ex) =>
            ex is SmtpCommandException { StatusCode: >= (SmtpStatusCode)500 } or ParseException;
    }
}
