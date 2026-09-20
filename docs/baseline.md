# The day-one baseline

Every performance target in this project is read AGAINST this page.

## Method

1. Start the probe: `dotnet run --project tools/baseline -c Release --urls http://127.0.0.1:5000`
2. Run the load: `k6 run tools/baseline/baseline.js`
3. Record the two numbers below. Repeat three times; keep the middle run.

## Results
| requests/s | p95 (ms) |
| ~250,000    | 1.3      |

