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
