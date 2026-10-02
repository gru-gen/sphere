# The day-one baseline

Every performance target in this project is read AGAINST this page.
A number without a baseline is a guess.

## Method

1. Start the probe: `dotnet run --project tools/baseline -c Release --urls http://127.0.0.1:5000`
2. Run the load: `k6 run tools/baseline/baseline.js`
3. Record the two numbers below. Repeat three times; keep the middle run.

## Results

| Machine | Date | requests/s | p95 (ms) |
|---|---|---|---|
| author's laptop (example) | 2026-10-02 | ~58,000 | 1.9 |
| your machine | | | |

## The 10x rule

The gap between a requirement and this baseline tells you how hard the
work is. A target within 10x of the baseline is engineering. A target
beyond 10x of what one machine shows here needs a different design,
not a faster loop. The four project targets (N1-N4) are judged this
way in every later measurement.
