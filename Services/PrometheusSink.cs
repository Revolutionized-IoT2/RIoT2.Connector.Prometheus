using RIoT2.Connector.Sdk;
using RIoT2.Core.Models;

namespace RIoT2.Connector.Prometheus.Services
{
    /// <summary>
    /// Keeps the latest value of every report (and command, with <c>RIOT2_HANDLE_COMMANDS=true</c>) for <c>/metrics</c>
    /// (design 7.3 phase 4, the second connector). Booleans become 1 or 0, numbers stay, and an entity's boolean and
    /// number leaves become fields named by their path (<see cref="EntityFlattener"/>); text is not exported. A report
    /// keeps its own time, a command the time the exporter received it. Until the templates are loaded, a batch is
    /// transient, so the messages wait for them.
    /// </summary>
    public sealed class PrometheusSink : IConnectorSink
    {
        private const string ScalarField = "value";
        private static readonly long MinUnixSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();
        private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        private readonly ValueStore _values;
        private readonly ILogger<PrometheusSink> _logger;
        private readonly TimeProvider _time;

        public PrometheusSink(ValueStore values, ILogger<PrometheusSink> logger, TimeProvider? time = null)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _time = time ?? TimeProvider.System;
        }

        public Task WriteAsync(IReadOnlyList<ConnectorMessage> batch, ITemplateCatalog templates, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(templates);
            var snapshot = templates.Snapshot
                ?? throw new TransientSinkException("The templates are not loaded yet; waiting for the orchestrator's configuration command.");
            var now = _time.GetUtcNow();
            foreach (var message in batch)
            {
                IReadOnlyList<PrometheusSample> samples;
                try
                {
                    samples = Map(message, snapshot, now);
                }
                // A message that can't be mapped is skipped on its own; letting it escape would drop the whole batch.
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    _logger.LogWarning(error, "Could not map a {Kind} from node {Node}", message.Kind, message.NodeId);
                    samples = [];
                }
                if (samples.Count == 0)
                    ConnectorMetrics.RecordIgnored(message);
                foreach (var sample in samples)
                    _values.Set(sample);
            }
            return Task.CompletedTask;
        }

        /// <summary>The samples a message sets; empty when it has no template or nothing numeric.</summary>
        public static IReadOnlyList<PrometheusSample> Map(ConnectorMessage message, TemplateSnapshot templates, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(templates);
            if (message.Kind == ConnectorMessageKind.Command)
            {
                var command = Command.Create(message.Json);
                var template = command == null ? null : templates.FindCommand(command.Id);
                return template == null ? [] : Samples(template, "command", command!.Id, command.Value, message.ReceivedUtc, now);
            }
            var report = Report.Create(message.Json);
            var reportTemplate = report == null ? null : templates.FindReport(report.Id);
            if (reportTemplate == null)
                return [];
            if (report!.TimeStamp < MinUnixSeconds || report.TimeStamp > MaxUnixSeconds)
                throw new InvalidDataException($"Report {report.Id} has a time stamp out of range: {report.TimeStamp} (Unix seconds expected).");
            return Samples(reportTemplate, "report", report.Id, report.Value, DateTimeOffset.FromUnixTimeSeconds(report.TimeStamp), now);
        }

        private static List<PrometheusSample> Samples(ConnectorTemplate template, string message, string id, ValueModel? value,
            DateTimeOffset reported, DateTimeOffset now)
        {
            if (value == null)
                return [];
            List<(string Field, object Value)> fields = value.Type switch
            {
                Core.ValueType.Boolean => [(ScalarField, value.GetValue<bool>())],
                Core.ValueType.Number => [(ScalarField, NumericValue(value))],
                Core.ValueType.Entity => [.. EntityFlattener.Flatten(value.GetAsObject())],
                _ => []
            };
            var samples = new List<PrometheusSample>(fields.Count);
            foreach (var (field, leaf) in fields)
            {
                var number = leaf switch
                {
                    bool b => b ? 1d : 0d,
                    int i => i,
                    long l => l,
                    float f => f,
                    double d => d,
                    decimal m => (double)m,
                    _ => double.NaN
                };
                if (double.IsFinite(number))
                    samples.Add(new PrometheusSample(message, id, template.Name, template.Device, template.Node, field, number, reported, now));
            }
            return samples;
        }

        private static double NumericValue(ValueModel value)
        {
            using var document = System.Text.Json.JsonDocument.Parse(value.ToJson());
            return document.RootElement.GetDouble();
        }
    }
}
