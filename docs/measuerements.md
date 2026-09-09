Method: k6 open-model scenarios (`tools/load/`) against the release
build, seeded catalog, warm caches
(second run recorded). Numbers below are one machine's 

| Run | Target | p95 | Errors | Verdict |
|---|---|---|---|---|
| browse.js — read path | 2000 rps, p95 <= 50 ms | 1.07 ms | 0.00% | PASS (reads) |
| shop.js — honest mix | the same rate | 2.54 ms | 0.00% | PASS
| spike.js — 10x for 1 min | survive the spike | 1.2 ms during spike | 7.3% during spike | PASS |
| restart during browse.js | 99.9% monthly | 8.2 s fully down | 100% for 8.2 s | FAIL |
| oversell probe | zero oversell | — | — |
