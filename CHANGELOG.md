# Changelog

All notable changes to `RIoT2.Connector.Prometheus`. A version is released by pushing a git tag. CI
then builds and pushes the Docker image to GitHub Container Registry.

## [Unreleased]

## [1.1.0] - 2026-11-03

- Changed: built on `RIoT2.Connector.Sdk` 2.0.0 ([I45](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-3-4.md#i45-core-20-remove-compatibility-layers-m)). The metrics and settings are unchanged.

## [1.0.0] - 2026-11-02

The first release ([I39](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-3-4.md#i39-connectors-in-the-ui-and-a-prometheus-exporter-m), [design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md) phase 4), on
RIoT2.Connector.Sdk 1.22.0.

- Added: `/metrics` with `riot2_value` and `riot2_value_timestamp_seconds` for every report (and, with
  `RIOT2_HANDLE_COMMANDS=true`, command) template field, labelled `message`, `id`, `name`, `device`,
  `node` and `field`.
- Added: `RIOT2_PROMETHEUS_MAX_AGE_MINUTES` (default 60): older values leave `/metrics`.
- Added: the image `ghcr.io/revolutionized-iot2/riot2-prometheus-exporter`, port 8080, user `app`, built
  with the release version, which the presence announces.
