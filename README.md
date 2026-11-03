# RIoT2.Connector.Prometheus

Prometheus exporter for the [RIoT2](https://github.com/Revolutionized-IoT2) platform. It listens to
RIoT2 MQTT reports (and optionally commands), resolves template metadata from the orchestrator, and
serves the latest value of each one at `/metrics` for Prometheus and Grafana. It is the second
connector on the [connector SDK](https://github.com/Revolutionized-IoT2/RIoT2.Core/blob/main/Connector/README.md)
(Core 2.0.0, [design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md)).

- Type: ASP.NET Core service
- Target framework: .NET 10
- Image: `ghcr.io/revolutionized-iot2/riot2-prometheus-exporter`
- Root namespace: `RIoT2.Connector.Prometheus`

How connectors fit into the platform: [architecture overview](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/architecture/overview.md).

## What it exports

```text
riot2_value{message="report",id="<template id>",name="Temperature",device="Garage",node="Node 1",field="value"} 21.5
riot2_value_timestamp_seconds{message="report",id="<template id>",name="Temperature",device="Garage",node="Node 1",field="value"} 1760000000
```

- One series per report (and command, with `RIOT2_HANDLE_COMMANDS=true`) template field. Booleans are
  1 or 0, numbers stay; an entity's boolean and number leaves are fields named by their path
  (`wind.speed`). Text is not exported.
- `riot2_value_timestamp_seconds` is when the value was reported (commands: received).
- An older report doesn't replace a newer one. A value not sent again within
  `RIOT2_PROMETHEUS_MAX_AGE_MINUTES` leaves `/metrics`.

The families and labels are in the
[metrics contract](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/metrics.md#prometheus-exporter).

## Configuration

- `RIOT2_CONNECTOR_ID`, `RIOT2_MQTT_IP`, and optionally `RIOT2_MQTT_USERNAME`/`RIOT2_MQTT_PASSWORD`,
  `RIOT2_MQTT_TLS`, `RIOT2_MQTT_CA_FILE`
- `RIOT2_CONNECTOR_URL` (optional): where the orchestrator reaches the exporter, so the UI shows its
  health; `RIOT2_CONNECTOR_NAME` (optional, default `Prometheus`)
- `RIOT2_HANDLE_COMMANDS` (optional, `true` also exports commands)
- `RIOT2_PROMETHEUS_MAX_AGE_MINUTES` (optional, 1–10080, default 60)
- `RIOT2_SECURITY_MODE`, `RIOT2_API_KEY`, `RIOT2_LOG_FORMAT`, `RIOT2_LOG_LEVEL`,
  `OTEL_EXPORTER_OTLP_ENDPOINT`, `RIOT2_TRACING` as for every connector

The exporter keeps no spool (`RIOT2_CONNECTOR_SPOOL` is ignored). Full list:
[env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md#connectors).

## Endpoints

- `GET /metrics`: the values in the Prometheus text format 0.0.4.
- `GET /health` and `GET /healthz`: from the SDK
  ([http-api-services.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/http-api-services.md#prometheus-exporter-riot2connectorprometheus)).

## Build, test and run

From the workspace root (`C:\Src\RIoT2`):

```powershell
dotnet build .\RIoT2.Connector.Prometheus\RIoT2.Connector.Prometheus.csproj
dotnet test .\RIoT2.Tests\RIoT2.Tests.csproj
dotnet run --project .\RIoT2.Connector.Prometheus\RIoT2.Connector.Prometheus.csproj
```

The tests live in [RIoT2.Tests](https://github.com/Revolutionized-IoT2/RIoT2.Tests)
(`PrometheusExporterTests`, `GoldenPrometheusTests`), which project-references this repository.

## Docker

The compose install runs it with the observability profile (`deploy/compose.observability.yml`), with a
Prometheus job `riot2-values` and the Grafana dashboard **RIoT2 values**. On its own:

```powershell
docker run -d --restart=on-failure:5 `
  --publish 8080:8080 `
  --env RIOT2_MQTT_IP=192.168.0.30 `
  --env RIOT2_MQTT_USERNAME=user `
  --env RIOT2_MQTT_PASSWORD=<mqtt-password> `
  --env RIOT2_CONNECTOR_ID=riot2-prometheus-exporter `
  ghcr.io/revolutionized-iot2/riot2-prometheus-exporter:latest
```

Then add `<host>:8080` as a scrape target. The image runs as the non-root `app` user.

## Versions and releases

- Release notes are in [CHANGELOG.md](CHANGELOG.md).
- To release, push a tag `x.y.z` on `main`. The release workflow validates, then publishes the
  Docker image to GitHub Container Registry ([CI guide](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/guides/ci.md)).
- This repository references `RIoT2.Connector.Sdk` `2.0.0` from GitHub Packages.

## Contributing

- Instructions for AI coding agents: [AGENTS.md](AGENTS.md).
- Platform documentation: [.github/docs](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/README.md).

## License

See [LICENSE](LICENSE).
