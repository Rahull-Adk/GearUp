# Load Testing – GearUp

This folder contains **load testing summaries and experiment notes** for the GearUp backend.

These are **not automated tests**.
They are manual, exploratory experiments used to:
- observe bottlenecks under concurrency
- identify slow queries
- validate fixes with before/after measurements

Raw tool output is **not committed** to git.
Only summaries, conclusions, and key metrics live here.

---

## Environment

- App: GearUp API
- Stack: ASP.NET Core + EF Core + MySQL
- Deployment: Local, single instance
- Realtime: SignalR
- Cache: None
- Queue: None
- Redis: None
- Rate limiting: Disabled in dev

---

## Tooling

- Load testing: `bombardier`
- DB analysis: MySQL slow query log, `EXPLAIN`
- Metrics of interest:
    - Avg latency
    - p95 / p99 latency
    - Error rate
    - Timeouts
    - DB connection pool exhaustion

---

## General Methodology

1. Warm up endpoint
2. Run load test with fixed concurrency
3. Capture output with metadata
4. Inspect slow query log
5. Apply **one** fix (index / query shape)
6. Re-run the same test
7. Compare p95 / p99 before vs after

No blind optimization.
No infra cosplay.
Measure → change → measure again.

---

## Endpoints Tested

- Feed (`GET /api/v1/posts`)
- Likes
- Comments
- Appointments (concurrency correctness)

Each endpoint has its own summary file describing:
- breaking point
- root cause
- fix applied
- measured impact
