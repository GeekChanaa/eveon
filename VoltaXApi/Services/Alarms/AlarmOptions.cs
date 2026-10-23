using System.Collections.Concurrent;

namespace VoltaXApi.Services.Alarms
{
    /// <summary>
    /// Alarm settings (section "Alarms"): offline delay, boot loop threshold, how often the availability monitor runs
    /// and who gets the critical alarm emails (default: SupportEmails:Admin).
    /// </summary>
    public sealed class AlarmOptions
    {
        public TimeSpan OfflineAfter { get; init; } = TimeSpan.FromMinutes(15);
        public int BootLoopCount { get; init; } = 3;
        public TimeSpan BootLoopWindow { get; init; } = TimeSpan.FromMinutes(15);
        public TimeSpan MonitorInterval { get; init; } = TimeSpan.FromMinutes(1);
        public IReadOnlyList<string> EmailRecipients { get; init; } = Array.Empty<string>();

        public static AlarmOptions From(IConfiguration configuration)
        {
            var section = configuration.GetSection("Alarms");
            var recipients = (section["EmailRecipients"] ?? configuration["SupportEmails:Admin"] ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new AlarmOptions
            {
                OfflineAfter = TimeSpan.FromMinutes(Math.Max(1, section.GetValue("OfflineAfterMinutes", 15))),
                BootLoopCount = Math.Max(2, section.GetValue("BootLoopCount", 3)),
                BootLoopWindow = TimeSpan.FromMinutes(Math.Max(1, section.GetValue("BootLoopWindowMinutes", 15))),
                MonitorInterval = TimeSpan.FromSeconds(Math.Max(10, section.GetValue("MonitorIntervalSeconds", 60))),
                EmailRecipients = recipients
            };
        }
    }

    /// <summary>
    /// Accepted boots of each charger in memory (singleton). A charger's connection lives on one instance, so its boots
    /// are counted where they arrive; a restart of the API forgets the history, which only delays a boot loop alarm.
    /// </summary>
    public sealed class BootHistory
    {
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _boots = new(StringComparer.Ordinal);

        /// <summary>Records a boot and returns how many boots happened within <paramref name="window"/> up to it.</summary>
        public int Record(string chargePointId, DateTime at, TimeSpan window)
        {
            var queue = _boots.GetOrAdd(chargePointId, _ => new Queue<DateTime>());
            lock (queue)
            {
                queue.Enqueue(at);
                while (queue.Count > 0 && queue.Peek() < at - window) queue.Dequeue();
                // Keep the history bounded even for a charger that boots continuously.
                while (queue.Count > 50) queue.Dequeue();
                return queue.Count;
            }
        }

        /// <summary>Time of the last recorded boot, if any.</summary>
        public DateTime? LastBoot(string chargePointId)
        {
            if (!_boots.TryGetValue(chargePointId, out var queue)) return null;
            lock (queue) return queue.Count == 0 ? null : queue.Last();
        }
    }
}
