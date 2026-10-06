using Microsoft.Extensions.Caching.Memory;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Handlers;

/// <summary>Collects an inventory across scoped NotifyReport handlers before replacing connectors.</summary>
public sealed class ConnectorReportBuffer : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 256 });
    private readonly object gate = new();

    private sealed class Report
    {
        public SortedDictionary<int, List<ReportDataType>> Parts { get; } = new();
        public int? LastSequence { get; set; }
    }

    public List<ReportDataType>? Add(string chargePointId, NotifyReportRequest request)
    {
        var sequence = request.SeqNo ?? 0;
        if (sequence < 0 || sequence > 4096)
            throw new ArgumentException("Invalid NotifyReport sequence number.");
        var connectors = (request.ReportData ?? new List<ReportDataType>())
            .Where(r => r?.Component?.Name == "Connector" && r.Variable?.Name == "AvailabilityState").ToList();
        if (connectors.Any(r => r.Component.Evse == null || r.Component.Evse.Id <= 0 ||
            r.Component.Evse.ConnectorId is not > 0))
            throw new ArgumentException("Connector inventory contains an invalid EVSE or connector ID.");
        if (connectors.Any(r => r.VariableAttribute?.Any(a =>
            a.Type == AttributeEnumType.Actual && !string.IsNullOrWhiteSpace(a.Value)) != true))
            throw new ArgumentException("Connector inventory must include an Actual availability value.");

        lock (gate)
        {
            var key = (chargePointId, request.RequestId);
            if (!cache.TryGetValue(key, out Report? report))
            {
                report = new Report();
                cache.Set(key, report, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    Size = 1
                });
            }
            report!.Parts[sequence] = connectors;
            if (request.Tbc != true) report.LastSequence = sequence;
            if (!report.LastSequence.HasValue) return null;
            var last = report.LastSequence.Value;
            if (report.Parts.Count != last + 1 ||
                Enumerable.Range(0, last + 1).Any(i => !report.Parts.ContainsKey(i))) return null;
            // Repeated entries in a report represent the same physical connector.
            return report.Parts.Values.SelectMany(p => p)
                .GroupBy(r => (r.Component.Evse!.Id, r.Component.Evse.ConnectorId))
                .Select(g => g.Last()).ToList();
        }
    }

    public void Complete(string chargePointId, int requestId)
    {
        lock (gate) cache.Remove((chargePointId, requestId));
    }

    public void Dispose() => cache.Dispose();
}
