using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCPP.Core.Server;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;
using static VoltaX.Tests.Infrastructure.ScriptedWebSocket;

namespace VoltaX.Tests.Ocpp;

/// <summary>
/// The OCPP transport of one API instance, without database or business handlers: the real message processor,
/// pending request registry and socket handler, with a test handler for Heartbeat (answers, throws or fails on
/// demand) and one for NotifyEvent that itself sends a command back to the charger.
/// </summary>
internal sealed class OcppTransport : IDisposable
{
    public sealed class ScopeMarker { public Guid Id { get; } = Guid.NewGuid(); }

    /// <summary>The DI scope each handled CALL ran in.</summary>
    public sealed class ScopeLog { public ConcurrentBag<Guid> Scopes { get; } = new(); }

    private sealed class HeartbeatHandler(ScopeMarker marker, ScopeLog log) : IOCPPRequestHandler
    {
        public Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            log.Scopes.Add(marker.Id);
            if (msgIn.JsonPayload!.Contains("throw")) throw new InvalidOperationException("secret database detail");
            if (msgIn.JsonPayload.Contains("fail")) return Task.FromResult(ErrorCodes.FormationViolation);
            msgOut.JsonPayload = "{\"currentTime\":\"now\"}";
            return Task.FromResult<string>(null!);
        }
    }

    private sealed class NestedCommandHandler(IOcppCommandSender sender) : IOCPPRequestHandler
    {
        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            await sender.SendRequestAsync<GetVariablesRequest, GetVariablesResponse>(chargePointStatus.Id, "GetVariables", new GetVariablesRequest());
            msgOut.JsonPayload = "{\"nested\":\"answered\"}";
            return null!;
        }
    }

    private readonly ServiceProvider _provider;

    public OcppTransport()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ocpp:CommandTimeoutSeconds"] = "2",
            ["Ocpp:MaxMessageSizeBytes"] = "8192"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<WebSocketManagerService>();
        services.AddSingleton<OcppPendingRequestRegistry>();
        services.AddSingleton<OcppInboundHandlerRegistry>();
        services.AddSingleton<OCPPRequestHandler>();
        services.AddSingleton<OCPPMessageProcessor>();
        services.AddSingleton<IOcppCommandSender>(sp => sp.GetRequiredService<OCPPMessageProcessor>());
        services.AddSingleton<IHubContext<ChargerHub>, NullHubContext<ChargerHub>>();
        services.AddSingleton<ChargePointStatusManagerService>();
        services.AddSingleton<ChargePointConnectivityNotifier>();
        services.AddSingleton<WebSocketHandler>();
        services.AddSingleton<WebSocketSubProtocolMatcher>();
        services.AddSingleton<ScopeLog>();
        services.AddScoped<ScopeMarker>();
        services.AddOcppInboundHandler<HeartbeatHandler>(OcppProtocols.Ocpp201, "Heartbeat");
        services.AddOcppInboundHandler<NestedCommandHandler>(OcppProtocols.Ocpp201, "NotifyEvent");
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public WebSocketManagerService Connections => _provider.GetRequiredService<WebSocketManagerService>();
    public OcppPendingRequestRegistry Pending => _provider.GetRequiredService<OcppPendingRequestRegistry>();
    public OCPPMessageProcessor Processor => _provider.GetRequiredService<OCPPMessageProcessor>();
    public OcppInboundHandlerRegistry Registry => _provider.GetRequiredService<OcppInboundHandlerRegistry>();
    public WebSocketSubProtocolMatcher Matcher => _provider.GetRequiredService<WebSocketSubProtocolMatcher>();
    public WebSocketHandler Handler => _provider.GetRequiredService<WebSocketHandler>();
    public ScopeLog Scopes => _provider.GetRequiredService<ScopeLog>();

    public static OcppConnection Connection(string id, ScriptedWebSocket socket) =>
        new(id, OcppProtocols.Ocpp201, socket, new ChargePointStatus { Id = id, Protocol = OcppProtocols.Ocpp201 });

    /// <summary>A connection registered as live, without a receive loop (frames are fed to the processor by hand).</summary>
    public OcppConnection Connect(string id, ScriptedWebSocket socket)
    {
        var connection = Connection(id, socket);
        Connections.AddConnection(connection);
        return connection;
    }

    public void Dispose() => _provider.Dispose();
}

/// <summary>Inbound CALLs and outgoing commands through the message processor, frame by frame.</summary>
public class OcppMessageProcessorTests : IDisposable
{
    private readonly OcppTransport _transport = new();
    private readonly ScriptedWebSocket _socket = new();
    private readonly OcppConnection _cp1;

    public OcppMessageProcessorTests() => _cp1 = _transport.Connect("CP1", _socket);

    public void Dispose() => _transport.Dispose();

    /// <summary>Feeds a frame like the receive loop does; returns the reply sent to the charger, if any.</summary>
    private async Task<System.Text.Json.JsonElement[]> Inbound(string text)
    {
        var before = _socket.Sent.Count;
        var queued = await _transport.Processor.ProcessIncomingAsync(_cp1, text, CancellationToken.None);
        if (queued != null)
            await _transport.Processor.HandleCallAsync(_cp1, queued, text, CancellationToken.None);
        return _socket.Sent.Count > before ? Elements(_socket.Sent[^1]) : Array.Empty<System.Text.Json.JsonElement>();
    }

    private Task Answer(string uniqueId, string payload, OcppConnection? from = null) =>
        _transport.Processor.ProcessIncomingAsync(from ?? _cp1, $"[3,\"{uniqueId}\",{payload}]", CancellationToken.None);

    [Fact]
    public void Only_versions_with_handlers_are_offered()
    {
        Assert.Equal(new[] { OcppProtocols.Ocpp201 }, _transport.Registry.SupportedProtocols);
        Assert.Null(_transport.Matcher.GetMatchingSubProtocol(new[] { "ocpp1.6" }));
        Assert.Equal("ocpp2.0.1", _transport.Matcher.GetMatchingSubProtocol(new[] { "ocpp1.6", "ocpp2.0.1" }));

        var both = new OcppInboundHandlerRegistry(new[]
        {
            new OcppInboundHandlerRegistration(OcppProtocols.Ocpp16, "Heartbeat", typeof(object)),
            new OcppInboundHandlerRegistration(OcppProtocols.Ocpp201, "Heartbeat", typeof(object))
        });
        Assert.Equal(new[] { OcppProtocols.Ocpp201, OcppProtocols.Ocpp16 }, both.SupportedProtocols); // 2.0.1 preferred
    }

    [Fact]
    public async Task A_handled_CALL_is_answered_with_its_payload_each_in_a_fresh_scope()
    {
        for (var i = 0; i < 3; i++)
        {
            var reply = await Inbound($"[2,\"m{i}\",\"Heartbeat\",{{}}]");
            Assert.Equal(3, reply.Length);
            Assert.Equal(3, reply[0].GetInt32());
            Assert.Equal($"m{i}", reply[1].GetString());
            Assert.Equal("now", reply[2].GetProperty("currentTime").GetString());
        }
        Assert.Equal(3, _transport.Scopes.Scopes.Distinct().Count());
    }

    [Fact]
    public async Task An_unknown_action_is_answered_NotImplemented()
    {
        var reply = await Inbound("[2,\"m2\",\"Unknown\",{}]");

        Assert.Equal(5, reply.Length);
        Assert.Equal((4, "m2", "NotImplemented"), (reply[0].GetInt32(), reply[1].GetString(), reply[2].GetString()));
    }

    [Fact]
    public async Task Handler_failures_become_CALLERRORs_without_internal_details()
    {
        var thrown = await Inbound("[2,\"m3\",\"Heartbeat\",{\"mode\":\"throw\"}]");
        Assert.Equal("InternalError", thrown[2].GetString());
        Assert.DoesNotContain("secret", thrown[3].GetString());

        var failed = await Inbound("[2,\"m4\",\"Heartbeat\",{\"mode\":\"fail\"}]");
        Assert.Equal("FormatViolation", failed[2].GetString()); // the 2.0.1 wire name
    }

    [Fact]
    public async Task A_malformed_CALL_with_a_readable_id_gets_a_CALLERROR()
    {
        var badPayload = await Inbound("[2,\"m5\",\"Heartbeat\",\"oops\"]");
        Assert.Equal(("m5", "FormatViolation"), (badPayload[1].GetString(), badPayload[2].GetString()));

        var noPayload = await Inbound("[2,\"m6\",\"Heartbeat\"]");
        Assert.Equal(5, noPayload.Length);
        Assert.Equal("RpcFrameworkError", noPayload[2].GetString());
    }

    [Theory]
    [InlineData("garbage")]               // no readable id
    [InlineData("[3,\"zz\",\"bad\"]")]    // malformed CALLRESULT
    [InlineData("[3,\"unknown\",{}]")]    // late CALLRESULT
    public async Task Unreadable_frames_and_stray_replies_are_never_answered(string frame)
    {
        Assert.Empty(await Inbound(frame));
    }

    [Fact]
    public async Task A_command_is_sent_as_a_CALL_and_completed_by_the_CALLRESULT_of_the_same_connection()
    {
        var reset = _transport.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest { Type = ResetEnumType.OnIdle });
        var call = await _socket.SentFrame(0);

        Assert.Equal((2, "Reset", "OnIdle"), (call[0].GetInt32(), call[2].GetString(), call[3].GetProperty("type").GetString())); // camelCase payload
        Assert.Equal(1, _transport.Pending.Count);
        var other = OcppTransport.Connection("CP-OTHER", new ScriptedWebSocket());
        await Answer(call[1].GetString()!, "{\"status\":\"Rejected\"}", other);
        Assert.False(reset.IsCompleted); // a reply on another connection does not count

        await Answer(call[1].GetString()!, "{\"status\":\"Scheduled\"}");

        Assert.Equal(ResetStatusEnumType.Scheduled, (await reset).Status);
        Assert.Equal(0, _transport.Pending.Count);
    }

    [Fact]
    public async Task A_CALLERROR_answer_throws_with_its_code_and_description()
    {
        var unlock = _transport.Processor.SendRequestAsync<UnlockConnectorRequest, UnlockConnectorResponse>("CP1", "UnlockConnector", new UnlockConnectorRequest { EvseId = 1, ConnectorId = 1 });
        var call = await _socket.SentFrame(0);

        await _transport.Processor.ProcessIncomingAsync(_cp1, $"[4,\"{call[1].GetString()}\",\"NotSupported\",\"no unlock\",{{}}]", CancellationToken.None);

        var error = await Assert.ThrowsAsync<OcppCallErrorException>(() => unlock);
        Assert.Equal(("NotSupported", "no unlock"), (error.ErrorCode, error.ErrorDescription));
        Assert.Equal(0, _transport.Pending.Count);
    }

    [Fact]
    public async Task An_unanswered_command_times_out_and_is_cleaned_up()
    {
        await Assert.ThrowsAsync<TimeoutException>(() =>
            _transport.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest(), TimeSpan.FromMilliseconds(100)));
        Assert.Equal(0, _transport.Pending.Count);
    }

    [Fact]
    public async Task A_command_to_a_charger_that_is_not_connected_fails_fast()
    {
        await Assert.ThrowsAsync<WebSocketNotFoundException>(() => _transport.Processor.SendRequestAsync<ResetRequest, ResetResponse>("NOPE", "Reset", new ResetRequest()));
    }

    [Fact]
    public async Task Commands_to_one_charger_are_sent_one_at_a_time_and_each_gets_its_answer()
    {
        var first = _transport.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest());
        var firstCall = await _socket.SentFrame(0);

        var second = _transport.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest());
        // The second CALL waits for the charger's call slot, which only the first answer frees.
        Assert.Single(_socket.Sent);
        Assert.False(second.IsCompleted);

        await Answer(firstCall[1].GetString()!, "{\"status\":\"Accepted\"}");
        var secondCall = await _socket.SentFrame(1);
        await Answer(secondCall[1].GetString()!, "{\"status\":\"Rejected\"}");

        Assert.Equal(ResetStatusEnumType.Accepted, (await first).Status);
        Assert.Equal(ResetStatusEnumType.Rejected, (await second).Status);
    }

    [Fact]
    public async Task The_send_lock_serializes_concurrent_writes_to_a_socket()
    {
        var socket = new ScriptedWebSocket { SendDelay = TimeSpan.FromMilliseconds(1) };
        var connection = OcppTransport.Connection("CP-LOCK", socket);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(() => connection.SendTextAsync($"[3,\"{i}\",{{}}]", CancellationToken.None))));

        Assert.Equal(10, socket.Sent.Count);
        Assert.Equal(1, socket.MaxConcurrentSends);

        // The fake does see overlapping sends when nothing serializes them.
        var raw = new ScriptedWebSocket { SendDelay = TimeSpan.FromMilliseconds(1) };
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => raw.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("x")), WebSocketMessageType.Text, true, CancellationToken.None))));
        Assert.True(raw.MaxConcurrentSends > 1);
    }
}
