using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using VoltaxApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services;
using VoltaXApi.Settings;

namespace VoltaX.Tests.Api;

/// <summary>Mail is queued by MailService and delivered by MailDeliveryService with retries.</summary>
public class MailQueueTests
{
    /// <summary>Records deliveries; <see cref="Gate"/> holds every send until released, <see cref="Failures"/> fail first.</summary>
    private sealed class FakeTransport : IMailTransport
    {
        public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Queue<Exception> Failures { get; } = new();
        public List<(string? MessageId, string Subject)> Attempts { get; } = new();
        public List<string> Delivered { get; } = new();
        public TaskCompletionSource<bool> AllDelivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Expected { get; set; } = 1;

        public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            await Gate.Task.WaitAsync(cancellationToken);
            lock (Attempts) Attempts.Add((message.MessageId, message.Subject));
            if (Failures.TryDequeue(out var failure))
                throw failure;
            lock (Delivered)
            {
                Delivered.Add(message.Subject);
                if (Delivered.Count >= Expected)
                    AllDelivered.TrySetResult(true);
            }
        }
    }

    private sealed class Environment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Testing";
    }

    private static (MailQueue Queue, MailDeliveryService Delivery) Pipeline(FakeTransport transport, int capacity = 100, int maxAttempts = 4)
    {
        var options = Options.Create(new MailQueueOptions { Capacity = capacity, MaxAttempts = maxAttempts, RetryDelaySeconds = 0.01, DrainOnStopSeconds = 1 });
        var queue = new MailQueue(options, NullLogger<MailQueue>.Instance);
        return (queue, new MailDeliveryService(queue, transport, options, TimeProvider.System, NullLogger<MailDeliveryService>.Instance));
    }

    private static MailService Service(IMailQueue queue) => new(
        Options.Create(new MailSettings { Mail = "noreply@eveon.test", Host = "smtp.invalid", Port = 587 }),
        queue,
        new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
        Options.Create(new SupportEmails { Admin = "admin@eveon.test", Support = "support@eveon.test" }),
        new Environment(),
        null!);

    private static MimeMessage Message(string subject) => new() { Subject = subject, To = { MailboxAddress.Parse("driver@eveon.test") } };

    [Fact]
    public async Task Sending_a_mail_returns_while_the_server_is_still_busy()
    {
        var transport = new FakeTransport();
        var (queue, delivery) = Pipeline(transport);
        await delivery.StartAsync(CancellationToken.None);

        var send = Service(queue).SendWelcomeEmail(new MailRequest { ToEmails = new List<string> { "driver@eveon.test" }, Subject = "Welcome" }, "Ev");

        Assert.True(send.IsCompletedSuccessfully);
        Assert.Empty(transport.Delivered);
        transport.Gate.SetResult();
        Assert.True(await transport.AllDelivered.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal("Welcome", Assert.Single(transport.Delivered));
        await delivery.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task A_failed_delivery_is_retried_until_it_succeeds()
    {
        var transport = new FakeTransport { Expected = 2 };
        transport.Failures.Enqueue(new IOException("connection reset"));
        transport.Failures.Enqueue(new InvalidOperationException("Could not connect to SMTP server"));
        transport.Gate.SetResult();
        var (queue, delivery) = Pipeline(transport);
        await delivery.StartAsync(CancellationToken.None);

        Assert.True(queue.Enqueue(Message("first")));
        Assert.True(queue.Enqueue(Message("second")));

        Assert.True(await transport.AllDelivered.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(new[] { "first", "second" }, transport.Delivered);
        Assert.Equal(new[] { "first", "first", "first", "second" }, transport.Attempts.Select(a => a.Subject));
        Assert.All(transport.Attempts, a => Assert.False(string.IsNullOrEmpty(a.MessageId)));
        await delivery.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task A_message_the_server_refuses_or_that_keeps_failing_is_dropped_and_the_next_one_sent()
    {
        var transport = new FakeTransport { Expected = 1 };
        transport.Failures.Enqueue(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "550 no such user"));
        transport.Failures.Enqueue(new IOException("down"));
        transport.Failures.Enqueue(new IOException("down"));
        transport.Gate.SetResult();
        var (queue, delivery) = Pipeline(transport, maxAttempts: 2);
        await delivery.StartAsync(CancellationToken.None);

        queue.Enqueue(Message("refused"));
        queue.Enqueue(Message("unreachable"));
        queue.Enqueue(Message("fine"));

        Assert.True(await transport.AllDelivered.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal("fine", Assert.Single(transport.Delivered));
        Assert.Equal(new[] { "refused", "unreachable", "unreachable", "fine" }, transport.Attempts.Select(a => a.Subject));
        await delivery.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void A_full_queue_refuses_new_messages()
    {
        var (queue, _) = Pipeline(new FakeTransport(), capacity: 2);

        Assert.True(queue.Enqueue(Message("1")));
        Assert.True(queue.Enqueue(Message("2")));
        Assert.False(queue.Enqueue(Message("3")));
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public async Task Waiting_messages_are_sent_when_the_api_stops()
    {
        var transport = new FakeTransport { Expected = 2 };
        var (queue, delivery) = Pipeline(transport);
        await delivery.StartAsync(CancellationToken.None);
        queue.Enqueue(Message("a"));
        queue.Enqueue(Message("b"));

        var stop = delivery.StopAsync(CancellationToken.None);
        transport.Gate.SetResult();
        await stop;

        Assert.Equal(new[] { "a", "b" }, transport.Delivered);
        Assert.False(queue.Enqueue(Message("late")));
    }
}
