# ADR-002: client tools run as pinned containers

## Status

Accepted (day one).

## Context

The project needs psql, redis-cli, mongosh, kcat, and jq. Native
Windows installers exist for some, are painful for others, and every
machine ends up with different versions.

## Decision

Database and broker client tools run as throwaway containers behind
small PowerShell functions (`tools/sphere-tools.ps1`). Every image
is pinned to an exact tag. `--rm` deletes the container after each
use, so nothing accumulates.

Two exceptions, for honest reasons:
- **k6** installs natively (`winget install grafana.k6`). It measures
  latency to localhost; running it inside a container would add the
  container network to every number.
- **git** and the **.NET SDK** are the workshop itself, not clients.

## Consequences

- Positive: identical tool versions on every machine; zero install
  steps beyond Docker; upgrades are a one-line tag change.
- Negative: first use of each tool downloads an image; interactive
  flags (`-it`) are needed and easy to forget.
