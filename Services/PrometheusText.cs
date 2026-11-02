using System.Globalization;
using System.Text;

namespace RIoT2.Connector.Prometheus.Services
{
    /// <summary>
    /// Writes the values in the Prometheus text exposition format (0.0.4): the gauge <c>riot2_value</c> and its
    /// <c>riot2_value_timestamp_seconds</c>, labelled <c>message</c>, <c>id</c>, <c>name</c>, <c>device</c>,
    /// <c>node</c> and <c>field</c>. RIoT2.Tests pins the output for every golden message
    /// (<c>Golden/prometheus-exposition.txt</c>).
    /// </summary>
    public static class PrometheusText
    {
        public const string ContentType = "text/plain; version=0.0.4; charset=utf-8";
        public const string ValueName = "riot2_value";
        public const string TimestampName = "riot2_value_timestamp_seconds";

        public static string Write(IReadOnlyList<PrometheusSample> samples)
        {
            ArgumentNullException.ThrowIfNull(samples);
            var text = new StringBuilder();
            text.Append("# HELP ").Append(ValueName).Append(" Latest value of each RIoT2 report and command template field; booleans are 1 or 0.\n");
            text.Append("# TYPE ").Append(ValueName).Append(" gauge\n");
            foreach (var sample in samples)
                Line(text, ValueName, sample, sample.Value);
            text.Append("# HELP ").Append(TimestampName).Append(" When the latest value was reported (commands: received by the exporter), in Unix seconds.\n");
            text.Append("# TYPE ").Append(TimestampName).Append(" gauge\n");
            foreach (var sample in samples)
                Line(text, TimestampName, sample, sample.Reported.ToUnixTimeMilliseconds() / 1000d);
            return text.ToString();
        }

        private static void Line(StringBuilder text, string name, PrometheusSample sample, double value)
        {
            text.Append(name).Append('{');
            Label(text, "message", sample.Message).Append(',');
            Label(text, "id", sample.Id).Append(',');
            Label(text, "name", sample.Name).Append(',');
            Label(text, "device", sample.Device).Append(',');
            Label(text, "node", sample.Node).Append(',');
            Label(text, "field", sample.Field);
            text.Append("} ").Append(value.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }

        private static StringBuilder Label(StringBuilder text, string name, string value)
        {
            text.Append(name).Append("=\"");
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': text.Append("\\\\"); break;
                    case '"': text.Append("\\\""); break;
                    case '\n': text.Append("\\n"); break;
                    default: text.Append(c); break;
                }
            }
            return text.Append('"');
        }
    }
}
