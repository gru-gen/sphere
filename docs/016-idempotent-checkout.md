# ADR-016: Idempotency-Key on checkout — the same answer, once

Status: accepted

## Context

Checkout now crosses the network. Clients retry on timeouts, and a
retried POST without protection places a second order. The retry is
correct client behaviour; the duplicate is our bug to prevent.

## Decision

An optional `Idempotency-Key` request header. The key row commits in
the SAME transaction as the order, so the primary key is the race
fence. A replay returns the stored result and does no work at all —
no basket read, no price call, no order.

## Consequences

+ Retries with a key are safe; concurrent duplicates collapse to one
  order (the loser's whole transaction rolls back on the key).
- The table grows until a TTL cleanup job exists — deferred, honestly.
- A replay does not detect a DIFFERENT body under the same key;
  production APIs often store a request hash and compare.
