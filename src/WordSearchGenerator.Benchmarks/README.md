# Estimator benchmark harness

This tool runs application-level puzzle generation cases serially and records
the measurements used to calibrate `WoSeCon.EstimateDifficulty`.

Run it from the repository root:

```powershell
dotnet run --project tools/WordSearchGenerator.Benchmarks -c Release -- `
  test-data/benchmarks/phase-1-smoke.json artifacts/benchmarks
```

Each manifest case defines its mode, matrix, words, secret message,
parallelism, deterministic base seed, repetition count and timeout. The
harness rejects timeouts above 180 seconds.

Results are written after every repetition to both JSON and CSV. Each row
contains machine/runtime information, summarized input parameters, estimator
prediction, outcome, wall and generator time, tested positions, backtracks,
rejection counters, occupancy and winning seed. Output under `artifacts/` is
intentionally ignored by Git.

Statuses are:

- `Succeeded`: an acceptable board was constructed.
- `NoSolution`: all workers exhausted their searches.
- `TimedOut`: the configured per-run time limit was reached.
- `Error`: an unexpected exception occurred; its details are retained.
