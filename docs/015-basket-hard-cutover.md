# ADR-015: basket_db by hard cutover

Status: accepted

## Context

Carts are : short-lived, user-recoverable, low value at rest.

## Decision

Create `basket_db`, point the service at it, accept that in-flight development carts vanish.

## Consequences

+ One SQL file and one connection-string flip.