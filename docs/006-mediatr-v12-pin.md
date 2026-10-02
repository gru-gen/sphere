# ADR-006: MediatR, pinned to the 12.x line

## Status

Accepted.

## Context

The Ordering module gets an application layer: commands, handlers, and
cross-cutting behaviors. The most widely used .NET library for this
shape is MediatR. From version 13, MediatR moved to a commercial
license for larger organizations; the 12.x line is Apache-2.0.

## Decision

MediatR **12.4.1**, centrally pinned. The pin is a license decision,
not only a version decision: moving to 13+ is a procurement event, to
be taken consciously — never as a casual package update.

## Consequences

- Positive: the standard command/behavior shape with zero license
  risk;
- Negative: the project forgoes 13.x features; if 12.x ever stops
  receiving fixes, the successor decision must be made on purpose.
