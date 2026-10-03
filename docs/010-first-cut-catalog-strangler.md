# ADR-010: extract Catalog first, behind a strangler facade

## Status

Accepted.

## Context

ADR-009 requires every extraction to cite measured numbers. The 
baseline (docs/measurements.md) supplies two:

- N3 fails BY CONSTRUCTION: one process means every deploy is ~8 s of
  total outage — and catalog code is where change is cheapest and most
  frequent, so it triggers most of those deploys.
- The read path carries most of the traffic (the honest mix is 80%
  browse) and passes N1 with a 7x margin — the safest, most valuable
  slice to move first.

The module boundary and the price contract 
mean this cut changes transport, not consumers.

## Decision

- A YARP gateway (Sphere.Gateway, port 5100) becomes the only
  public door. The monolith moves to 5110; the Catalog service runs on
  5120.
- Routes move by configuration: /api/products and /api/categories go
  to the Catalog service; everything else stays on the monolith.
- Ordering owns its price port (IProductPriceReader) and reaches
  Catalog through an anti-corruption layer over HTTP with a 2-second
  timeout. The internal endpoint /internal/prices has no gateway
  route.

## Consequences

- Positive: catalog deploys stop taking the whole shop down; the
  biggest traffic slice can later scale on its own; every step of the
  migration is reversible with a route flip.
- Negative: one more hop on every request; checkout gains a runtime
  dependency on a second process (only a timeout guards it for now —
  deeper resilience is deliberately deferred);
