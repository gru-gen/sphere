# ADR-001: record architecture decisions

## Status

Accepted (day one).

## Context

This project will make hundreds of technical choices. Six months from
now, nobody remembers why a choice was made. The choice then gets
re-argued, or worse, silently reversed.

## Decision

Every real technology or design choice gets a short Architecture
Decision Record in `docs/adr/`, written BEFORE the code lands. The
format is this file's format: Status, Context, Decision, Consequences.
ADRs are append-only. A changed decision gets a new ADR that
supersedes the old one.

## Consequences

- Positive: decisions have a home; reviews argue with a document, not
  with a memory.
- Negative: a small writing tax on every choice. We pay it.
