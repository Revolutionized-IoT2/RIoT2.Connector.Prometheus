using RIoT2.Connector.Prometheus.Services;
using RIoT2.Connector.Sdk;

var builder = WebApplication.CreateBuilder(args);
builder.AddRIoT2Connector<PrometheusSink, PrometheusExporterOptions>(PrometheusExporterOptions.Component, PrometheusExporterOptions.Read, PrometheusExporterOptions.Configure);
builder.Services.AddSingleton<ValueStore>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.MapRIoT2Connector();
app.MapPrometheusMetrics();
app.Run();
