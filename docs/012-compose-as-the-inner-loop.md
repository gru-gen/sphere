# ADR-012: docker compose is the inner development loop

## Status

Accepted.

## Context

After the first cut, running the shop means three hand-started
terminals plus a database container. Configuration fits exactly one
laptop. Every future extraction adds a terminal, and every "works on
my machine" gets harder to answer.

## Decision

One compose file describes the whole system, and F5 starts it through
the compose project in the solution:

- postgres on the exact production pin, with a real healthcheck
  (pg_isready) and an init script that creates catalog_db on the
  first start of an empty volume;
- the three services built from their own multi-stage Dockerfiles;
- ONLY the gateway publishes a host port (5100). The monolith and the
  catalog exist solely on the compose network — the public/private
  line is now enforced by the network, not just by a
  missing route;
- services find each other by compose DNS names, supplied through
  environment overrides (Catalog__BaseUrl, the gateway's cluster
  addresses).

Bare `dotnet run` per project stays supported: appsettings keeps the
localhost defaults, and postgres publishes 5432 for that mode.

## Consequences

- Positive: one keystroke runs the system; developers share one
  parity; images are exercised daily, long before production sees
  them; /internal is unreachable from outside by construction.
- Negative: the first build is slow (cached restore layers soften the
  next ones);
