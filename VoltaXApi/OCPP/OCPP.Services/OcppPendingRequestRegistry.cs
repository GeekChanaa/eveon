using System.Collections.Concurrent;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// CSMS-initiated CALLs waiting for the charger's CALLRESULT/CALLERROR, keyed by message id.
    /// A reply only completes a request that was sent on the same connection.
    /// </summary>
    public class OcppPendingRequestRegistry
    {
        private sealed record PendingRequest(OcppConnection Connection, string Action, TaskCompletionSource<string> Reply);

        private readonly ConcurrentDictionary<string, PendingRequest> _pending = new();

        public int Count => _pending.Count;

        /// <summary>Registers the request before it is sent; the task completes with the CALLRESULT payload.</summary>
        public Task<string> Register(OcppConnection connection, string uniqueId, string action)
        {
            var request = new PendingRequest(connection, action, new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
            if (!_pending.TryAdd(uniqueId, request))
                throw new InvalidOperationException($"A request with message id {uniqueId} is already pending.");
            return request.Reply.Task;
        }

        public bool TryComplete(OcppConnection connection, string uniqueId, string payloadJson, out string? action) =>
            TryFinish(connection, uniqueId, r => r.Reply.TrySetResult(payloadJson), out action);

        public bool TryFail(OcppConnection connection, string uniqueId, Exception error, out string? action) =>
            TryFinish(connection, uniqueId, r => r.Reply.TrySetException(error), out action);

        public void Remove(string uniqueId) => _pending.TryRemove(uniqueId, out _);

        /// <summary>Fails every request still waiting on <paramref name="connection"/> (used when it closes).</summary>
        public int FailAll(OcppConnection connection, Exception error)
        {
            var failed = 0;
            foreach (var entry in _pending)
            {
                if (entry.Value.Connection == connection && _pending.TryRemove(entry))
                {
                    entry.Value.Reply.TrySetException(error);
                    failed++;
                }
            }
            return failed;
        }

        private bool TryFinish(OcppConnection connection, string uniqueId, Action<PendingRequest> finish, out string? action)
        {
            action = null;
            if (!_pending.TryGetValue(uniqueId, out var request) || request.Connection != connection)
                return false;
            if (!_pending.TryRemove(new KeyValuePair<string, PendingRequest>(uniqueId, request)))
                return false;
            action = request.Action;
            finish(request);
            return true;
        }
    }
}
