# ADR-014: Internal transport stays JSON over HTTP — for now

Status: accepted

## Context

Two internal calls exist today: checkout → catalog (prices) and
checkout → basket (read, clear). gRPC offers binary framing on HTTP/2,
contract-first codegen, and streaming. Our internal calls are low-rate
request/response, the payloads are a few hundred bytes, and both sides
are .NET sharing nothing but the wire.

## Decision

Keep JSON over HTTP for internal calls.

## Consequences

+ Every internal call stays curl-able and test-rig friendly.

## Revisit when

An internal call becomes high-rate fan-out or needs streaming, or the
performance part measures serialization as a real cost. The decision is
reversible per call: the ports and anti-corruption layers mean each
transport hides behind exactly one class.
