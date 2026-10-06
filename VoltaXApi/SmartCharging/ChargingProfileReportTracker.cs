using System.Collections.Concurrent;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.SmartCharging
{
    /// <summary>
    /// What a GetChargingProfiles asked for: profiles stored as Accepted inside this scope and missing from the
    /// report are no longer on the charger. <see cref="IncludesCso"/> is false when the request filtered out
    /// CSMS-set profiles (chargingLimitSource without CSO), which then cannot be reconciled.
    /// </summary>
    public sealed record ChargingProfileReportScope(
        int? EvseId,
        ChargingProfilePurposeEnum? Purpose,
        int? StackLevel,
        IReadOnlyCollection<int>? ProfileIds,
        bool IncludesCso);

    /// <summary>
    /// Collects the ReportChargingProfiles parts of a GetChargingProfiles request (tbc = true until the last one),
    /// keyed by charger and requestId. Singleton; unfinished reports are dropped after <see cref="Expiry"/>.
    /// </summary>
    public sealed class ChargingProfileReportTracker
    {
        public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

        private sealed class Pending
        {
            public ChargingProfileReportScope? Scope { get; init; }
            public DateTime StartedAt { get; init; }
            public List<ReportChargingProfilesRequest> Parts { get; } = new();
        }

        private readonly ConcurrentDictionary<(string ChargePointId, int RequestId), Pending> _pending = new();

        public void Register(string chargePointId, int requestId, ChargingProfileReportScope scope)
        {
            Sweep();
            _pending[(chargePointId, requestId)] = new Pending { Scope = scope, StartedAt = DateTime.UtcNow };
        }

        public void Forget(string chargePointId, int requestId) => _pending.TryRemove((chargePointId, requestId), out _);

        /// <summary>Adds a part; returns the complete report (and the scope, null for an unsolicited report) after the last part.</summary>
        public (bool Complete, ChargingProfileReportScope? Scope, IReadOnlyList<ReportChargingProfilesRequest> Parts) AddPart(
            string chargePointId, ReportChargingProfilesRequest part)
        {
            Sweep();
            var key = (chargePointId, part.RequestId);
            var pending = _pending.GetOrAdd(key, _ => new Pending { Scope = null, StartedAt = DateTime.UtcNow });
            lock (pending)
            {
                pending.Parts.Add(part);
                if (part.Tbc == true)
                    return (false, pending.Scope, Array.Empty<ReportChargingProfilesRequest>());
            }
            _pending.TryRemove(key, out _);
            lock (pending)
                return (true, pending.Scope, pending.Parts.ToList());
        }

        private void Sweep()
        {
            var limit = DateTime.UtcNow - Expiry;
            foreach (var (key, pending) in _pending)
                if (pending.StartedAt < limit)
                    _pending.TryRemove(key, out _);
        }
    }
}
