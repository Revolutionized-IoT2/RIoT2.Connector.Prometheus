using System.Collections.Concurrent;

namespace RIoT2.Connector.Prometheus.Services
{
    /// <summary>One exported value: its labels, the value, and when it was reported and last updated here.</summary>
    public sealed record PrometheusSample(string Message, string Id, string Name, string Device, string Node, string Field,
        double Value, DateTimeOffset Reported, DateTimeOffset Updated);

    /// <summary>
    /// The latest value of each report and command template field (keyed by message type, template id and field), as
    /// <c>/metrics</c> shows it. Values not updated within <see cref="PrometheusExporterOptions.MaxAge"/> are dropped
    /// when the metrics are read, so a removed device doesn't stay in Prometheus forever.
    /// </summary>
    public sealed class ValueStore
    {
        private readonly ConcurrentDictionary<(string Message, string Id, string Field), PrometheusSample> _samples = new();
        private readonly TimeSpan _maxAge;

        public ValueStore(PrometheusExporterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _maxAge = options.MaxAge;
        }

        public int Count => _samples.Count;

        /// <summary>Keeps <paramref name="sample"/> unless a newer report of the same field is already there.</summary>
        public void Set(PrometheusSample sample)
        {
            ArgumentNullException.ThrowIfNull(sample);
            _samples.AddOrUpdate((sample.Message, sample.Id, sample.Field), sample,
                (_, existing) => existing.Reported > sample.Reported ? existing with { Updated = sample.Updated } : sample);
        }

        /// <summary>The current values in a stable order, after dropping the ones older than the maximum age.</summary>
        public IReadOnlyList<PrometheusSample> Snapshot(DateTimeOffset now)
        {
            var cutoff = now - _maxAge;
            foreach (var (key, sample) in _samples)
            {
                if (sample.Updated < cutoff)
                    _samples.TryRemove(new KeyValuePair<(string, string, string), PrometheusSample>(key, sample));
            }
            return _samples.Values
                .OrderBy(s => s.Message, StringComparer.Ordinal)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ThenBy(s => s.Id, StringComparer.Ordinal)
                .ThenBy(s => s.Field, StringComparer.Ordinal)
                .ToList();
        }
    }
}
