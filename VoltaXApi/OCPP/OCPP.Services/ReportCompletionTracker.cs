using System.Collections.Concurrent;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Lets a caller that sent GetBaseReport wait until the charger has delivered the whole report.
    /// A report arrives as one or more NotifyReport messages; the last one has tbc = false.
    /// </summary>
    public class ReportCompletionTracker
    {
        private readonly ConcurrentDictionary<(string ChargePointId, int RequestId), TaskCompletionSource> _pending = new();

        public Task Register(string chargePointId, int requestId)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[(chargePointId, requestId)] = tcs;
            return tcs.Task;
        }

        public void Forget(string chargePointId, int requestId) => _pending.TryRemove((chargePointId, requestId), out _);

        public void PartReceived(string chargePointId, int requestId, bool toBeContinued)
        {
            if (!toBeContinued && _pending.TryRemove((chargePointId, requestId), out var tcs))
                tcs.TrySetResult();
        }
    }
}
