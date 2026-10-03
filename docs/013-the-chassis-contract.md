# ADR-013: the chassis contract

## Status

Accepted.

## Context

Three hosts had begun duplicating startup plumbing — health endpoints,
telemetry, HTTP client defaults. Every future service would copy the
same block again, and copies drift silently until an incident makes
the drift visible.

## Decision

Sphere.ServiceDefaults is the chassis: a plain shared class
library that every host references and calls first.

In scope:

- telemetry wiring (OpenTelemetry traces, metrics, logs), with the
  exporter active only when the standard
  OTEL_EXPORTER_OTLP_ENDPOINT variable is set;
- default resilience for every HttpClient in the process (the
  standard handler); a stricter per-client budget always wins;
- uniform health endpoints: /health/live and /health/ready on every
  service.

Out of scope, permanently: business logic, module wiring, shared
DTOs or contracts, per-service tuning. The chassis stays additive —
nothing it wires may prevent a host from overriding it.

## Consequences

- Positive: a new service starts with a reference and two calls; the
  operational surface is uniform; an improvement lands everywhere at
  once.
- Negative: a chassis change ships with every service's next deploy,
  so changes must be boring, reviewed, and rare; the chassis works
  per language — a second stack would need its own.
