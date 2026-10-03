# ADR-011: catalog data moves to its own database

## Status

Accepted.

## Context

After the process split, Catalog and the monolith still shared the
shopsphere database. A shared database is the widest hidden API in the
system: any schema change would need cross-service coordination, and
one SQL join could quietly re-couple what the code split separated.

## Decision

Catalog owns catalog_db. 

Other services reach catalog data ONLY through Catalog's API. In
development both databases share one engine;

## Consequences

- Positive: schema changes, backups, and scaling become Catalog's
  private business; the boundary cannot be bypassed with a join.
- Negative: cross-database joins and transactions.
