using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Collects the parts (tbc / seqNo) of a NotifyMonitoringReport across the scoped handlers and hands the whole
    /// report back once every part from 0 to the last one (tbc = false) has arrived. Singleton.
    /// Also remembers which requestIds asked for every monitor (no filter): only those replace the stored monitors.
    /// </summary>
    public sealed class MonitoringReportAssembler : IDisposable
    {
        private const int MaxSequence = 4096;
        private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 512 });
        private readonly object _gate = new();
        private readonly TimeSpan _partTimeout;

        public MonitoringReportAssembler() : this(TimeSpan.FromMinutes(10)) { }

        public MonitoringReportAssembler(TimeSpan partTimeout) => _partTimeout = partTimeout;

        private sealed class Report
        {
            public SortedDictionary<int, List<MonitoringDataType>> Parts { get; } = new();
            public int? LastSequence { get; set; }
        }

        /// <summary>Marks <paramref name="requestId"/> as a full (unfiltered) monitoring report of the charger.</summary>
        public void MarkFullReport(string chargePointId, int requestId)
        {
            lock (_gate)
                _cache.Set(("full", chargePointId, requestId), true, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1), Size = 1 });
        }

        public bool IsFullReport(string chargePointId, int requestId)
        {
            lock (_gate) return _cache.TryGetValue(("full", chargePointId, requestId), out _);
        }

        /// <summary>Adds a part; returns every monitor of the report when it is complete, else null.</summary>
        public List<MonitoringDataType>? Add(string chargePointId, NotifyMonitoringReportRequest request)
        {
            var sequence = request.SeqNo ?? 0;
            if (sequence < 0 || sequence > MaxSequence)
                throw new ArgumentException("Invalid NotifyMonitoringReport sequence number.");

            lock (_gate)
            {
                var key = ("parts", chargePointId, request.RequestId);
                if (!_cache.TryGetValue(key, out Report? report))
                {
                    report = new Report();
                    _cache.Set(key, report, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _partTimeout, Size = 1 });
                }
                report!.Parts[sequence] = (request.Monitor ?? new List<MonitoringDataType>()).Where(m => m != null).ToList();
                if (request.Tbc != true) report.LastSequence = sequence;
                if (!report.LastSequence.HasValue) return null;
                var last = report.LastSequence.Value;
                if (Enumerable.Range(0, last + 1).Any(i => !report.Parts.ContainsKey(i))) return null;
                _cache.Remove(key);
                return report.Parts.Where(p => p.Key <= last).SelectMany(p => p.Value).ToList();
            }
        }

        public void Dispose() => _cache.Dispose();
    }

    /// <summary>Assembles NotifyCustomerInformation parts (seqNo) of one request into the full data, stored between parts as JSON.</summary>
    public static class CustomerInformationAssembler
    {
        public const int MaxParts = 4096;

        public sealed record Result(string PartsJson, int PartsReceived, string Data, bool Complete);

        /// <summary>
        /// Adds part <paramref name="seqNo"/> to <paramref name="partsJson"/> (null for the first one). The report is complete
        /// when the part with tbc = false has arrived and no seqNo before it is missing.
        /// </summary>
        public static Result Append(string? partsJson, int seqNo, string? data, bool tbc)
        {
            if (seqNo < 0 || seqNo > MaxParts) throw new ArgumentException("Invalid NotifyCustomerInformation sequence number.");
            var state = string.IsNullOrEmpty(partsJson)
                ? new State()
                : JsonSerializer.Deserialize<State>(partsJson) ?? new State();
            state.Parts[seqNo] = data ?? "";
            if (!tbc) state.Last = seqNo;
            var ordered = state.Parts.OrderBy(p => p.Key).ToList();
            var complete = state.Last.HasValue && Enumerable.Range(0, state.Last.Value + 1).All(state.Parts.ContainsKey);
            var assembled = string.Concat(ordered.Where(p => !state.Last.HasValue || p.Key <= state.Last.Value).Select(p => p.Value));
            return new Result(JsonSerializer.Serialize(state), state.Parts.Count, assembled, complete);
        }

        private sealed class State
        {
            public Dictionary<int, string> Parts { get; set; } = new();
            public int? Last { get; set; }
        }
    }
}
