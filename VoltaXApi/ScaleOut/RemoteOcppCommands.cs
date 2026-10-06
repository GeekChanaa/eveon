using System.Collections.Concurrent;
using System.Text.Json;
using VoltaXApi.OCPP.Exceptions;

namespace VoltaXApi.ScaleOut;

/// <summary>
/// The charger is owned by another instance but the scale-out transport (Redis) cannot be reached.
/// A <see cref="WebSocketNotFoundException"/> so existing "not connected" handling keeps working; maps to 503 where handled.
/// </summary>
public sealed class ScaleOutUnavailableException : WebSocketNotFoundException
{
    public ScaleOutUnavailableException(string message, Exception? inner = null) : base(message + (inner == null ? "" : $" ({inner.Message})")) { }
}

/// <summary>A CSMS CALL forwarded to the instance that owns the charger.</summary>
public sealed record OcppCommandEnvelope(string RequestId, string ChargePointId, string Action, string? PayloadJson, long TimeoutMs, string ReplyChannel);

/// <summary>Outcome of a forwarded CALL. Status: ok | callerror | timeout | notconnected.</summary>
public sealed record OcppCommandReply(string RequestId, string Status, string? PayloadJson = null, string? ErrorCode = null, string? ErrorDescription = null)
{
    public const string Ok = "ok";
    public const string CallError = "callerror";
    public const string Timeout = "timeout";
    public const string NotConnected = "notconnected";
}

/// <summary>Caller side of cross-instance command routing: publishes the envelope and waits for the owner's reply.</summary>
public sealed class RemoteOcppCommandClient
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Network/scheduling allowance on top of the command timeout the owner already enforces.
    private static readonly TimeSpan ReplyGrace = TimeSpan.FromSeconds(2);

    private readonly ConcurrentDictionary<string, TaskCompletionSource<OcppCommandReply>> _pending = new();
    private readonly IScaleOutBus _bus;
    private readonly IChargerConnectionRegistry _registry;
    private readonly ScaleOutSettings _settings;
    private readonly ILogger<RemoteOcppCommandClient> _logger;

    public RemoteOcppCommandClient(IScaleOutBus bus, IChargerConnectionRegistry registry, ScaleOutSettings settings, ILogger<RemoteOcppCommandClient> logger)
    {
        _bus = bus;
        _registry = registry;
        _settings = settings;
        _logger = logger;
    }

    public int PendingCount => _pending.Count;

    /// <summary>The instance owning the charger when it is not this one; null when nobody owns it.</summary>
    public ChargerOwnership? FindRemoteOwner(string chargePointId)
    {
        try
        {
            var owner = _registry.Get(chargePointId);
            return owner != null && owner.InstanceId != _settings.InstanceId ? owner : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Charger registry unavailable: cannot look up the owner of {ChargePointId}", chargePointId);
            return null;
        }
    }

    /// <summary>Same contract as a local CALL: returns the CALLRESULT payload or throws the same exceptions.</summary>
    public async Task<string> SendAsync(string chargePointId, string action, string? payloadJson, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ChargerOwnership? owner;
        try
        {
            owner = await _registry.GetAsync(chargePointId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ScaleOutUnavailableException($"Charge point {chargePointId} cannot be reached: the instance registry is unavailable.", ex);
        }
        if (owner == null || owner.InstanceId == _settings.InstanceId)
            throw new WebSocketNotFoundException($"Charge point {chargePointId} is not connected.");

        var requestId = Guid.NewGuid().ToString("N");
        var reply = new TaskCompletionSource<OcppCommandReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = reply;
        try
        {
            var envelope = new OcppCommandEnvelope(requestId, chargePointId, action, payloadJson, (long)timeout.TotalMilliseconds,
                ScaleOutSettings.ReplyChannel(_settings.InstanceId));
            long receivers;
            try
            {
                receivers = await _bus.PublishAsync(ScaleOutSettings.CommandChannel(owner.InstanceId), JsonSerializer.Serialize(envelope, Json));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new ScaleOutUnavailableException($"Charge point {chargePointId} cannot be reached: the scale-out bus is unavailable.", ex);
            }
            if (receivers == 0)
                throw new WebSocketNotFoundException($"Charge point {chargePointId} is not connected (its instance {owner.InstanceId} does not answer).");

            _logger.LogInformation("Forwarded {Action} for {ChargePointId} to instance {InstanceId} (request {RequestId})", action, chargePointId, owner.InstanceId, requestId);
            OcppCommandReply answer;
            try
            {
                answer = await reply.Task.WaitAsync(timeout + ReplyGrace, cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Instance {InstanceId} did not answer {Action} for {ChargePointId} within {Timeout} s", owner.InstanceId, action, chargePointId, timeout.TotalSeconds);
                throw new TimeoutException($"Charge point {chargePointId} did not answer {action} within {timeout.TotalSeconds:0} s.");
            }
            return ToResult(chargePointId, action, answer);
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    /// <summary>Handles a message from this instance's reply channel.</summary>
    public void HandleReply(string message)
    {
        var reply = JsonSerializer.Deserialize<OcppCommandReply>(message, Json);
        if (reply == null || !_pending.TryRemove(reply.RequestId, out var waiter))
        {
            _logger.LogWarning("Scale-out reply {RequestId} matches no pending command (late or unknown)", reply?.RequestId);
            return;
        }
        waiter.TrySetResult(reply);
    }

    private static string ToResult(string chargePointId, string action, OcppCommandReply reply) => reply.Status switch
    {
        OcppCommandReply.Ok => reply.PayloadJson ?? "{}",
        OcppCommandReply.CallError => throw new OcppCallErrorException(reply.ErrorCode, reply.ErrorDescription, reply.PayloadJson),
        OcppCommandReply.Timeout => throw new TimeoutException(reply.ErrorDescription ?? $"Charge point {chargePointId} did not answer {action} in time."),
        OcppCommandReply.NotConnected => throw new WebSocketNotFoundException(reply.ErrorDescription ?? $"Charge point {chargePointId} is not connected."),
        _ => throw new OcppCallErrorException("InternalError", $"Unexpected scale-out reply status '{reply.Status}' for {action}.")
    };

    /// <summary>Owner side: turns the outcome of the local CALL into a reply.</summary>
    public static OcppCommandReply ToReply(string requestId, string? payloadJson, Exception? error) => error switch
    {
        null => new OcppCommandReply(requestId, OcppCommandReply.Ok, payloadJson),
        OcppCallErrorException callError => new OcppCommandReply(requestId, OcppCommandReply.CallError, callError.ErrorDetails, callError.ErrorCode, callError.ErrorDescription),
        TimeoutException timeout => new OcppCommandReply(requestId, OcppCommandReply.Timeout, ErrorDescription: timeout.Message),
        WebSocketNotFoundException notConnected => new OcppCommandReply(requestId, OcppCommandReply.NotConnected, ErrorDescription: notConnected.Message),
        OperationCanceledException => new OcppCommandReply(requestId, OcppCommandReply.NotConnected, ErrorDescription: "The owning instance is shutting down."),
        var other => new OcppCommandReply(requestId, OcppCommandReply.CallError, ErrorCode: "InternalError", ErrorDescription: other.Message)
    };
}
