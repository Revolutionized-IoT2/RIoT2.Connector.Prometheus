using RIoT2.Core.Security;

namespace RIoT2.Connector.Prometheus.Services
{
    public static class PrometheusEndpoint
    {
        /// <summary>
        /// <c>GET /metrics</c>: the values in the Prometheus text format, for Prometheus to scrape. Viewer, like the other
        /// read routes; while the security mode is <c>off</c> anyone may read it.
        /// </summary>
        public static WebApplication MapPrometheusMetrics(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            app.MapGet("/metrics", (ValueStore values, TimeProvider time) =>
                    Results.Text(PrometheusText.Write(values.Snapshot(time.GetUtcNow())), PrometheusText.ContentType))
                .RequireAuthorization(SecurityPolicies.Viewer);
            return app;
        }
    }
}
