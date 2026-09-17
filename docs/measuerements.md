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

## The strangler completes

Same method, same machine — checkout is event-driven and the monolith
container is gone. What changed and what it cost:

| Run | After ch10 | After ch11 | Reading |
|---|---|---|---|
| checkout p95 (the 202) | 45 ms | 9 ms | the reply no longer waits for prices or the order INSERT — it waits for one local transaction and one acks=all publish |
| checkout -> order visible | 0 ms (same reply) | p95 ~120 ms | the honest price of 202: the fact travels the log; the order arrives a beat later |
| basket restart, during shop.js | basket steps fail ~6 s | checkout fails ~6 s; ORDERS still complete for facts already published | the blast radius moved one door down and shrank |
| ordering restart, during shop.js | checkout fails ~6 s | checkout UNTOUCHED; orders pause, then catch up from the log | the whole point, measured — the consumer replays what it missed |
| compose stack memory | ~1.5 GB | ~1.4 GB | one JVM stays, one empty host leaves |

The second row is the contract change in numbers: nobody waits on the
slowest service anymore, and the cost is a visible 404-then-200 window.
The fourth row is why the chapter exists.