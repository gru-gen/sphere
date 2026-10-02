# ADR-007: Dapper for the Ordering read side

## Status

Accepted.

## Context

Reading an order is a different job from changing one. The write side
needs the aggregate, the change tracker, and the domain events. A read
needs rows, shaped for the response, as fast as possible.

## Decision

Ordering's read endpoints use **Dapper** (Apache-2.0, pinned): plain
SQL, mapped straight into response records, no change tracker. The
rule that keeps this safe: Dapper connections in this module are
**read-only by convention** — a write through Dapper would bypass the
aggregate's rules, and code review treats it as a defect.

## Consequences

- Positive: the SQL is visible and tunable; reads carry no tracking
  cost; response shapes stop depending on the domain model.
- Negative: two data-access styles in one module — the convention (and
  reviewers) must hold the line; SQL strings need the same care as
  code.
