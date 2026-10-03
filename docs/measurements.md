# baseline — the numbers everything after must beat

Method: k6 open-model scenarios (`tools/load/`) against the release
build on the day-one machine, seeded catalog, warm caches
(second run recorded). Numbers below are one machine's EXAMPLE; record
yours in this table — the shapes transfer, the digits do not.

| Run | Target | p95 | Errors | Verdict |
|---|---|---|---|---|
| browse.js — read path | N1: 2000 rps, p95 <= 50 ms | 1.5 ms | 0.00% | PASS (reads) |
| shop.js — honest mix | N1 at the same rate | 3.23 ms | 0.21% | BORDERLINE — checkout drags the tail |
| spike.js — 10x for 1 min | N2: survive the spike | 2.5 ms during spike | 0.00% during spike | FAIL |
| restart during browse.js | N3: 99.9% monthly | 8.2 s fully down | 100% for 8.2 s | FAIL as a mechanism |
| oversell probe | N4: zero oversell | — | — | NOT TESTABLE |

Readings, honestly:

- The read path is comfortable — index work is visible here.
- The mix fails on its tail: checkouts contend on the write path and on
  database connections; the spike turns that contention into an error
  avalanche.
- Deploy-equals-restart makes N3 unreachable by CONSTRUCTION, not by
  tuning: every deploy burns ~8 s of total outage, and any crash is a
  full outage. This is a property of the single process.
- N4 cannot fail yet, which is not the same as passing.

## After the first cut

Same method, same machine — now three processes (gateway 5100,
monolith 5110, catalog 5120). Deltas against the baseline table:

| Run | Baseline | After the cut | Reading |
|---|---|---|---|
| browse.js p95 | 1.5 ms | 2.8 ms | +1.3 ms — the gateway hop, bought on purpose |
| shop.js p95 | 3.23 ms | 4.5 ms | +1.27 checkout now makes one HTTP price call |
| monolith restart, during browse.js | 100% down for 8.2 s | browse: 0% errors | reads live elsewhere now — the blip shrank to the deploy's blast radius |
| catalog restart, during browse.js | (the same 8.2 s) | browse down ~7 s; checkouts fail in that window | the new dependency, named honestly — the 2 s timeout caps each call's pain; |
| spike.js | 2.5 ms | 3.9 ms | + 1.4 ms

The first cut moved exactly the number it cited — the deploy blast
radius for catalog changes — and no other. That is what ADR-010 calls
success.