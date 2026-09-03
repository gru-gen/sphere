## Method

1. Start the probe: `dotnet run --project tools/baseline -c Release --urls http://127.0.0.1:5000`
2. Run the load: `k6 run tools/baseline/baseline.js`
