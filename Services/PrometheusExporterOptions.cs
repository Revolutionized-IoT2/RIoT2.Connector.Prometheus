using RIoT2.Connector.Sdk;
using RIoT2.Core.Configuration;

namespace RIoT2.Connector.Prometheus.Services
{
    /// <summary>
    /// The exporter's own setting (<c>RIOT2_PROMETHEUS_MAX_AGE_MINUTES</c>); the shared connector settings (id, name,
    /// URL, MQTT, logging, telemetry, security) are the connector SDK's <see cref="ConnectorOptions"/>.
    /// </summary>
    public sealed class PrometheusExporterOptions
    {
        public const string Component = "Prometheus exporter";
        public const string MaxAgeVariable = "RIOT2_PROMETHEUS_MAX_AGE_MINUTES";

        /// <summary>A value not reported again within this time is left out of <c>/metrics</c>; 60 minutes by default.</summary>
        public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(60);

        public static PrometheusExporterOptions Read(SettingsReader settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return new PrometheusExporterOptions
            {
                MaxAge = TimeSpan.FromMinutes(settings.Integer(MaxAgeVariable, 60, 1, 10080))
            };
        }

        /// <summary>
        /// The SDK settings this connector fixes: the name <c>Prometheus</c> (unless <c>RIOT2_CONNECTOR_NAME</c> is set),
        /// no disk spool (Prometheus pulls the latest values; a backlog has no use) and no batch delay.
        /// </summary>
        public static void Configure(ConnectorOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Name = "Prometheus";
            options.Spool.Enabled = false;
            options.BatchInterval = TimeSpan.Zero;
        }

        /// <summary>Reads every setting as the start does, and throws <see cref="ConfigurationValidationException"/> listing every problem.</summary>
        public static (ConnectorOptions Connector, PrometheusExporterOptions Exporter) Load(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
            ConnectorOptions.Load(configuration, Component, Read);
    }
}
