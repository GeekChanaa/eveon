using System.Net.WebSockets;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using static VoltaX.Tests.Infrastructure.ScriptedWebSocket;

namespace VoltaX.Tests.Ocpp;

/// <summary>A charger connection's receive loop: bad frames, fragments, nested commands, size limit, close and reconnect.</summary>
public class WebSocketReceiveLoopTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private readonly OcppTransport _transport = new();

    public void Dispose() => _transport.Dispose();

    [Fact]
    public async Task The_loop_survives_bad_frames_reassembles_fragments_and_skips_binary_ones()
    {
        var socket = new ScriptedWebSocket();
        var connection = OcppTransport.Connection("CP-LOOP", socket);
        var run = _transport.Handler.RunConnectionAsync(connection);

        socket.Enqueue("this is not json");
        socket.Enqueue("[2,\"b1\",\"Heartbeat\",{\"mode\":\"throw\"}]");
        socket.EnqueueFragments("[2,\"b2\",\"Heart", "beat\",{}]");
        socket.EnqueueBinary(new byte[] { 1, 2, 3 });
        socket.Enqueue("[2,\"b3\",\"Heartbeat\",{}]");
        await socket.WaitForSent(3);

        Assert.Same(connection, _transport.Connections.GetConnection("CP-LOOP"));
        var replies = socket.Sent.Select(Elements).ToList();
        Assert.Equal(("b1", "InternalError"), (replies[0][1].GetString(), replies[0][2].GetString())); // socket kept open
        Assert.Equal((3, "b2"), (replies[1][0].GetInt32(), replies[1][1].GetString()));
        Assert.Equal("b3", replies[2][1].GetString());

        socket.EnqueueClose();
        await run.WaitAsync(Wait);
    }

    [Fact]
    public async Task A_handler_awaiting_a_command_to_the_same_charger_does_not_block_the_loop()
    {
        var socket = new ScriptedWebSocket
        {
            AutoReply = frame =>
            {
                var e = Elements(frame);
                return e[0].GetInt32() == 2 && e[2].GetString() == "GetVariables" ? $"[3,\"{e[1].GetString()}\",{{\"getVariableResult\":[]}}]" : null;
            }
        };
        var run = _transport.Handler.RunConnectionAsync(OcppTransport.Connection("CP-NESTED", socket));

        socket.Enqueue("[2,\"b4\",\"NotifyEvent\",{}]");
        await socket.WaitForSent(2); // GetVariables, then the NotifyEvent reply

        var reply = Elements(socket.Sent[^1]);
        Assert.Equal((3, "b4", "answered"), (reply[0].GetInt32(), reply[1].GetString(), reply[2].GetProperty("nested").GetString()));
        socket.EnqueueClose();
        await run.WaitAsync(Wait);
    }

    [Fact]
    public async Task An_oversized_message_closes_the_connection_with_MessageTooBig()
    {
        var socket = new ScriptedWebSocket();
        var run = _transport.Handler.RunConnectionAsync(OcppTransport.Connection("CP-BIG", socket));

        socket.Enqueue("[2,\"b5\",\"Heartbeat\",{\"pad\":\"" + new string('x', 9000) + "\"}]"); // limit: 8192 bytes
        await run.WaitAsync(Wait);

        Assert.Equal(WebSocketCloseStatus.MessageTooBig, socket.CloseStatus);
        Assert.Null(_transport.Connections.GetConnection("CP-BIG"));
    }

    [Fact]
    public async Task A_close_frame_is_answered_and_fails_the_pending_commands()
    {
        var socket = new ScriptedWebSocket();
        var run = _transport.Handler.RunConnectionAsync(OcppTransport.Connection("CP-CLOSE", socket));
        var waiting = _transport.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP-CLOSE", "Reset", new ResetRequest());
        await socket.WaitForSent(1);

        socket.EnqueueClose();
        await run.WaitAsync(Wait);

        Assert.Equal(WebSocketState.Closed, socket.State);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, socket.CloseStatus);
        await Assert.ThrowsAsync<WebSocketNotFoundException>(() => waiting);
        Assert.Null(_transport.Connections.GetConnection("CP-CLOSE"));
        Assert.Equal(0, _transport.Pending.Count);
    }

    [Fact]
    public async Task A_reconnect_closes_the_old_connection_without_removing_the_new_one()
    {
        var oldRun = _transport.Handler.RunConnectionAsync(OcppTransport.Connection("CP-RE", new ScriptedWebSocket()));
        var newSocket = new ScriptedWebSocket();
        var newConnection = OcppTransport.Connection("CP-RE", newSocket);
        var newRun = _transport.Handler.RunConnectionAsync(newConnection);

        await oldRun.WaitAsync(Wait);

        Assert.Same(newConnection, _transport.Connections.GetConnection("CP-RE"));
        newSocket.EnqueueClose();
        await newRun.WaitAsync(Wait);
    }
}
