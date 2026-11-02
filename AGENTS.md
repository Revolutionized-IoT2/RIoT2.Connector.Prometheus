# AGENTS.md — RIoT2.Connector.Prometheus

Applies to: this repository. Read the platform guide first:
[.github/AGENTS.md](https://github.com/Revolutionized-IoT2/.github/blob/main/AGENTS.md). It covers the
workspace map, platform-wide rules and the documentation rules. In the local workspace, every
`https://github.com/Revolutionized-IoT2/<Repo>/blob/main/<path>` link is the file
`C:\Src\RIoT2\<Repo>\<path>`; read the local file instead of fetching the URL.

## What this is

An ASP.NET Core connector that keeps the latest value of every RIoT2 report (and optionally command)
and serves it at `/metrics` for Prometheus: the second connector on the connector SDK
(`RIoT2.Connector.Sdk` in RIoT2.Core,
[design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md) phase 4,
[SDK README](https://github.com/Revolutionized-IoT2/RIoT2.Core/blob/main/Connector/README.md)). The SDK
does the settings, MQTT, the orchestrator's templates, the queue, health and metrics; this repository
keeps the values and writes the Prometheus text format.

## Commands

Run from the workspace root (`C:\Src\RIoT2`), in PowerShell:

```powershell
dotnet restore .\RIoT2.Connector.Prometheus\RIoT2.Connector.Prometheus.sln
dotnet build .\RIoT2.Connector.Prometheus\RIoT2.Connector.Prometheus.csproj
dotnet test .\RIoT2.Tests\RIoT2.Tests.csproj
dotnet run --project .\RIoT2.Connector.Prometheus\RIoT2.Connector.Prometheus.csproj
```

- The run command requires the connector environment variables from
  [env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md#connectors).
- To build the container locally, put a GitHub Packages token in `$env:NUGET_AUTH_TOKEN`, then:
  `docker build --secret id=nuget_token,env=NUGET_AUTH_TOKEN -t riot2-prometheus-exporter .\RIoT2.Connector.Prometheus`.
  The token is a BuildKit secret; never pass it as a build argument.
- Every push runs `.github/workflows/ci.yml` (build, image build, secret scan). To release, push
  a tag `x.y.z` on `main`: `release.yml` validates, then pushes
  `ghcr.io/revolutionized-iot2/riot2-prometheus-exporter:latest` and `:<tag>` ([CI guide](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/guides/ci.md)).
- To try an unreleased Core (SDK) version, pack it into `C:\Src\RIoT2\.localfeed` and restore with
  that folder as an extra NuGet source.

## Layout

| Path | Contents |
|---|---|
| `Program.cs` | `AddRIoT2Connector<PrometheusSink, PrometheusExporterOptions>(...)`, `MapRIoT2Connector()` and `MapPrometheusMetrics()` |
| `Services/PrometheusExporterOptions.cs` | `RIOT2_PROMETHEUS_MAX_AGE_MINUTES`; `Configure` fixes the name, turns the spool off and the batch delay to zero |
| `Services/PrometheusSink.cs` | `IConnectorSink`: maps reports and commands to samples (booleans 1/0, numbers, entity leaves) |
| `Services/ValueStore.cs` | The latest sample per message type, template id and field; drops old ones when read |
| `Services/PrometheusText.cs` | The text exposition format: `riot2_value`, `riot2_value_timestamp_seconds` |
| `Services/PrometheusEndpoint.cs` | `GET /metrics` (Viewer) |
| `Dockerfile` | `net10.0` image, web port 8080; base images pinned by digest (Renovate updates them) |

## Contracts consumed here

- [mqtt-topics.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/mqtt-topics.md),
  [configuration.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/configuration.md)
  and [http-api-services.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/http-api-services.md#prometheus-exporter-riot2connectorprometheus):
  through the SDK, plus `/metrics`.
- [metrics.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/metrics.md#prometheus-exporter):
  the families and labels this repository writes. docs-check compares the `"riot2_..."` names in this code
  with that section.
- [env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md#connectors):
  `PrometheusExporterOptions` and the SDK's `ConnectorOptions`; nothing else reads the environment.

## Rules

- The metric names and labels are a contract (Prometheus queries, the "RIoT2 values" dashboard):
  RIoT2.Tests `GoldenPrometheusTests` pins the output for every golden message
  (`Golden/prometheus-exposition.txt`). Change them only with the dashboard and `metrics.md`.
- Generic behaviour (MQTT, templates, queue, health) belongs in the SDK, not here. Entity flattening is
  the SDK's `EntityFlattener`, shared with the InfluxDB connector.
- Keep the spool off: Prometheus pulls the latest value, so a backlog has no use.
- Map each message on its own; a message that can't be mapped is counted `ignored`, not thrown.
- Reference Core only through `RIoT2.Connector.Sdk` (`1.22.0`, set in `Directory.Packages.props`).
  Keep `PackageReference` items versionless.
- Don't add a Prometheus client library: the format is small and pinned by the golden test.

## Pitfalls

- A value disappears from `/metrics` when it isn't sent again within `RIOT2_PROMETHEUS_MAX_AGE_MINUTES`
  (default 60); slow reporters need a larger value.
- Until the orchestrator's configuration command loads the templates, messages wait in memory.
- `/metrics` needs the Viewer role once the security mode is on; Prometheus then needs the API key
  (not before I49).

## Related work

- [Design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md):
  the connector SDK; Home Assistant discovery is the next connector.
- [I39](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-3-4.md#i39-connectors-in-the-ui-and-a-prometheus-exporter-m):
  this repository's first release.
