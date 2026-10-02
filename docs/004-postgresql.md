# ADR-004: PostgreSQL as the system of record

## Status

Accepted.

## Context

The application needs a relational database: products, categories, and soon
baskets and orders with real transactional rules.

## Decision

PostgreSQL, pinned to the `postgres:17.5-alpine` image. One server for
the whole monolith; one schema per module (ADR-003). For development it
runs as a single container with a named volume
(`tools/dev.ps1 > Start-SphereDb`); no orchestration until there is more
than one process to coordinate.

## Consequences

- Positive: the most widely used open-source relational engine; rich
  SQL for the deep work ahead; identical version on every machine.
- Negative: operations are on us (backups, upgrades) until the cloud;
  the dev password lives in `appsettings.json` and is
  explicitly a development-only value.
