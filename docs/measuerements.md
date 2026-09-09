## Baseline
Method: k6 open-model scenarios (`tools/load/`) against the release
build, seeded catalog, warm caches
(second run recorded). Numbers below are one machine's 

Same method, same machine — now three processes (gateway 5100,
host 5110, catalog 5120). Deltas against the Part I table:

| Run | Target | p95 | Errors | Verdict |
|---|---|---|---|---|
| browse.js — read path | 2000 rps, p95 <= 50 ms | 1.07 ms | 0.00% | PASS (reads) |
| shop.js — honest mix | the same rate | 2.54 ms | 0.00% | PASS
| spike.js — 10x for 1 min | survive the spike | 1.2 ms during spike | 7.3% during spike | PASS |
| restart during browse.js | 99.9% monthly | 8.2 s fully down | 100% for 8.2 s | FAIL |
| oversell probe | zero oversell | — | — |

## After the Catalog.Service cut

Same method, same machine — now three processes (gateway 5100,
host 5110, catalog 5120). Deltas against the Part I table:

| Run | Part I | After the cut | Reading |
|---|---|---|---|
| browse.js p95 | 1.07 ms | 1.16 ms | +0.09 ms — the gateway |
| shop.js p95 | 2.54 ms | 3.03 ms | +0.49 ms | checkout now makes one HTTP price call |
| spike.js p95 | 1.2 ms  | 1.22 | + 0.02 ms