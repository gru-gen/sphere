# ADR-003: a modular monolith, modules as projects

## Status

Accepted.

## Context

Sphere starts with three jobs: catalog, basket,
ordering. A distributed system on day one would pay network, deployment,
and debugging costs before earning anything from them. But a single
project with folders slowly turns into a ball of mud: everything can
reach everything.

## Decision

One deployable process (`Sphere.Host`) and one module per business
job, each module a separate project under `src/Modules/`. Rules:

- The host wires modules; it contains no business logic.
- A module's public surface is one registration class. Everything else
  is `internal`.
- Modules do not reference each other. Until a real need appears, they
  only meet through the host.
- Each module owns its own database **schema** inside the shared
  PostgreSQL server (`catalog`, later `basket`, `ordering`).

## Consequences

- Positive: one process to run, debug, and deploy; refactoring across
  the whole application is a normal code change; module boundaries are
  compiler-checked, which keeps a later split cheap.
- Negative: the discipline is ours to keep — nothing physical stops a
  lazy shortcut; the application scales only as a whole.
