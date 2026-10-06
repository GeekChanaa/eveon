using System.Text.Json;
using VoltaXApi.Authorization;
using VoltaXApi.OCPP.Core;

namespace VoltaXApi.ScaleOut;

/// <summary>
/// Subscribes this instance to its scale-out channels (forwarded commands, replies, close-stale, hub revocations)
/// and refreshes the ownership of the local chargers. Subscriptions are retried until the bus is reachable.
/// </summary>
public sealed class ScaleOutCoordinator : BackgroundService
{
    private static readonly TimeSpan SubscribeRetry = TimeSpan.FromSeconds(5);

    private readonly IScaleOutBus _bus;
    private readonly ScaleOutSettings _settings;
    private readonly OCPPMessageProcessor _processor;
    private readonly RemoteOcppCommandClient _remote;
    private readonly ChargerOwnershipTracker _ownership;
    private readonly HubConnections _hubConnections;
    private readonly ILogger<ScaleOutCoordinator> _logger;
    private readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ScaleOutCoordinator(IScaleOutBus bus, ScaleOutSettings settings, OCPPMessageProcessor processor, RemoteOcppCommandClient remote,
        ChargerOwnershipTracker ownership, HubConnections hubConnections, ILogger<ScaleOutCoordinator> logger)
    {
        _bus = bus;
        _settings = settings;
        _processor = processor;
        _remote = remote;
        _ownership = ownership;
        _hubConnections = hubConnections;
        _logger = logger;
    }

    /// <summary>Completes once every channel is subscribed.</summary>
    public Task Subscribed => _subscribed.Task;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scale-out instance {InstanceId} ({Mode})", _settings.InstanceId, _settings.UsesRedis ? "Redis" : "in-memory");
        await SubscribeAllAsync(stoppingToken);

        using var timer = new PeriodicTimer(_settings.Heartbeat);
        while (await WaitTick(timer, stoppingToken))
            await _ownership.HeartbeatAsync();
    }

    private async Task SubscribeAllAsync(CancellationToken stoppingToken)
    {
        var channels = new (string Channel, Func<string, Task> Handler)[]
        {
            (ScaleOutSettings.CommandChannel(_settings.InstanceId), HandleCommandAsync),
            (ScaleOutSettings.ReplyChannel(_settings.InstanceId), m => { _remote.HandleReply(m); return Task.CompletedTask; }),
            (ScaleOutSettings.CloseStaleChannel(_settings.InstanceId), m => { _ownership.HandleCloseStale(m); return Task.CompletedTask; }),
            (ScaleOutSettings.HubRevokeChannel, m => { _hubConnections.HandleRemoteRevocation(m); return Task.CompletedTask; }),
        };

        foreach (var (channel, handler) in channels)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _bus.SubscribeAsync(channel, handler);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not subscribe to {Channel}; retrying in {Delay} s", channel, SubscribeRetry.TotalSeconds);
                    try { await Task.Delay(SubscribeRetry, stoppingToken); }
                    catch (OperationCanceledException) { return; }
                }
            }
        }
        _subscribed.TrySetResult();
    }

    /// <summary>Runs a command forwarded by another instance on the local connection and publishes the outcome.</summary>
    public async Task HandleCommandAsync(string message)
    {
        var envelope = JsonSerializer.Deserialize<OcppCommandEnvelope>(message, RemoteOcppCommandClient.Json);
        if (envelope == null) return;

        string? payload = null;
        Exception? error = null;
        try
        {
            payload = await _processor.SendLocalCallAsync(envelope.ChargePointId, envelope.Action, envelope.PayloadJson,
                TimeSpan.FromMilliseconds(Math.Max(1, envelope.TimeoutMs)), CancellationToken.None);
        }
        catch (Exception ex)
        {
            error = ex;
        }

        var reply = RemoteOcppCommandClient.ToReply(envelope.RequestId, payload, error);
        _logger.LogInformation("Forwarded {Action} for {ChargePointId} (request {RequestId}) finished: {Status}",
            envelope.Action, envelope.ChargePointId, envelope.RequestId, reply.Status);
        await _bus.PublishAsync(envelope.ReplyChannel, JsonSerializer.Serialize(reply, RemoteOcppCommandClient.Json));
    }

    private static async Task<bool> WaitTick(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try { return await timer.WaitForNextTickAsync(stoppingToken); }
        catch (OperationCanceledException) { return false; }
    }
}
