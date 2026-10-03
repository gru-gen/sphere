# ADR-009: measure before splitting

## Status

Accepted.

## Context

modular monolith completed and measured it (docs/measurements.md).
Some limits are real; the temptation to "go microservices" is now at
its strongest — and most dangerous, because distribution's costs start
on day one and its benefits only pay where a MEASURED limit exists.

## Decision

Every distribution step in this repository must cite a measured limit:

- a number from docs/measurements.md (or its successors) that the step
  is expected to move, and
- the trigger it answers: independent scaling need, blast-radius
  incident, deploy contention, or team queueing — demonstrated, not
  predicted.

Absent such a citation, the answer to "should we split X?" is "not
yet". The monolith remains the default until it loses on evidence.

## Consequences

- Positive: architecture changes become falsifiable; every extraction
  opens by naming the number it attacks.
- Negative: slower to satisfy fashion; someone must keep the
  measurement table honest as the system evolves.
