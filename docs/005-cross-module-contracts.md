# ADR-005: cross-module contracts

## Status

Accepted.

## Context

Checkout needs three modules to cooperate: the basket's content, the
catalog's current prices, and the ordering rules. ADR-003 said modules
do not reference each other "until a real need appears". The need has
appeared.

## Decision

A module may expose a small `Contracts` surface: public interfaces and
plain records, nothing else. Rules:

- Contracts are read-oriented and narrow; internals stay internal.
- Allowed directions form a DAG: Ordering may use Basket and Catalog
  contracts. No cycles, ever.
- Calls are in-process method calls today. The contract shapes are
  designed so the same interface can become a remote call later
  without changing its consumer.
- **No shared database transactions across modules.** Each module
  commits its own work. A flow that spans modules is a sequence of
  commits, not one transaction — because a boundary you lean across
  today cannot become a service boundary tomorrow.

## Consequences

- Positive: cooperation without coupling; the compiler still guards
  the walls; a later extraction changes the contract's transport, not
  its consumers.
- Negative: cross-module flows must handle the gap between commits
  (checkout clears the basket after the order exists — if the clear
  fails, the basket lingers and the flow must tolerate that).
