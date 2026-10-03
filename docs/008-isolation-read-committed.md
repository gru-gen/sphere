# ADR-008: Read Committed, on purpose

## Status

Accepted.

## Context

PostgreSQL offers an isolation ladder: Read Committed (default),
Repeatable Read, Serializable. Higher rungs remove anomalies and add
costs — snapshot lifetimes, serialization failures, mandatory retries.

## Decision

Sphere stays on **Read Committed** as the default. Correctness
comes from tools, in this order:

1. Database constraints (unique indexes, foreign keys, checks) — the
   guards that cannot lose a race.
2. Aggregate boundaries — one aggregate, one transaction, one owner
   per rule.
3. Explicit conflict handling — SQLSTATE 23505 becomes a 409; a lost
   race is an answered request, not a corrupted row.

A single operation may raise its own isolation level only when a
concrete anomaly has been demonstrated for it, and it must then carry
a retry policy for serialization failures. No global raise, ever.

## Consequences

- Positive: the default path stays fast and lock-light; anomalies are
  handled where they are real instead of taxed everywhere.
- Negative: engineers must KNOW the anomaly table
  each escalation is a small design, not a config flip.
