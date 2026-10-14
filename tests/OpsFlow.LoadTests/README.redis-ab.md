# OpsFlow Redis A/B benchmark

This benchmark uses the existing `ServicesController`, `ServiceManagementService`, repository, DTOs, JWT authentication and response. It adds a separate benchmark host and does not change the production API, existing load-test script, or existing test projects.

## Safety and scenarios

The Compose project is `opsflow-redis-benchmark`. It has its own PostgreSQL volume and Redis container, bound to loopback ports 15433 and 16380. Do not point this benchmark at the existing `opsflow` database or Redis. The seeded organization and service records are created once in this disposable database and remain unchanged during all three scenarios.

- **Bypass (A):** benchmark host replaces `ICacheService` with `BenchmarkBypassCacheService`. Each call behaves as a cache miss; reads still go to PostgreSQL and writes do nothing.
- **Miss (B):** benchmark host uses Redis. The runner removes `services:org:{orgId}` before each timed GET. The timed GET queries PostgreSQL and repopulates Redis.
- **Hit (C):** benchmark host uses Redis. The runner primes the key before timing, checks that the key exists, then measures requests whose logs show `Cache HIT`.

The PowerShell runner times only the HTTP GET, not cache deletion or prewarming. It compares status, response byte count and SHA-256 across scenarios and writes raw samples, per-run percentiles and host logs under `%TEMP%\OpsFlowRedisBenchmark\<run-id>`.

## Start the isolated dependencies

From the repository root in PowerShell:

```powershell
docker compose -p opsflow-redis-benchmark -f docker-compose.redis-benchmark.yml up -d postgres redis
docker compose -p opsflow-redis-benchmark -f docker-compose.redis-benchmark.yml ps
```

Confirm that this project lists only the benchmark PostgreSQL and Redis containers, with ports 15433 and 16380, before continuing. The API and Worker from the existing `opsflow` project are not used.

## Sequential latency run

The runner builds the benchmark host, starts it in Bypass or Redis mode, logs in with the seed admin, and performs 100 samples per scenario across 3 repetitions by default. It discards 5 warm-up requests per scenario. The order rotates between repetitions. Each B sample deletes the key before the timer starts; each C repetition explicitly primes it.

```powershell
.\tests\OpsFlow.LoadTests\redis-ab-sequential.ps1
```

The default seed login is present in `OpsFlowDbSeeder`; it is only for the disposable local benchmark database. Override `-AdminEmail` and `-AdminPassword` if the seed credentials change.

## Controlled k6 load runs

The wrapper builds and starts the benchmark host separately for A and C. Defaults are 25 virtual users, 30 seconds, and 3 repetitions for each scenario. It saves k6 JSON summaries and logs under `%TEMP%\OpsFlowRedisBenchmark\k6-<run-id>`. The exported summaries remove the login token. The setup login and warm-up request happen before the measured scenario.

```powershell
.\tests\OpsFlow.LoadTests\redis-ab-load.ps1
```

For a shorter pilot, use `-Vus 2 -Duration 5s -Repetitions 1`. The script checks that every response has the same body hash as setup, confirms the cache path from API logs, and records end-to-end latency percentiles, throughput and request failures. Keep the host logs to inspect EF command durations for database-backed requests.

There is one cache key for this organization. A cold-cache concurrent burst naturally includes a miss followed by hits as soon as the first request repopulates the key. Therefore scenario B is measured as repeated cold **single requests**, not misreported as a sustained all-miss load test. A and C receive steady-state controlled load tests.

## Interpreting and reporting results

Report p50, p95 and p99 for each repetition, errors, and k6 requests per second. Report median and range (or standard deviation) across repetitions. Compare the same statistic from A and C:

```text
latency reduction % = ((A latency - C latency) / A latency) * 100
latency speedup ratio = A latency / C latency
```

Use p50-to-p50 and p95-to-p95 comparisons. Latency reduction and throughput change are different measurements. Do not describe the result as a user-capacity figure or claim Redis reduced PostgreSQL load without query-count evidence from the same run.

Redis logs time the GET operation but stop before JSON deserialization; the existing service does not separately time Redis SET. EF Core command logs contain database command durations. These timings are diagnostic and should be reported separately from end-to-end HTTP latency. Keep host logs for each run so the path evidence is auditable.

## Results captured on 2026-10-14

These are local measurements from the isolated benchmark stack, one seeded organization with four services, one API process, PostgreSQL and Redis in local Docker, and the same 1,264-byte response body (same SHA-256 in every sequential sample). The machine model, CPU, memory and Docker resource limits were not captured, so these figures describe only this run and are not portable capacity claims.

### Sequential requests (100 samples per scenario, 3 repetitions)

The table shows the median of the three per-run percentiles. No request failed and all response hashes matched. The full 900 sequential samples and per-run summary rows are checked in under `tests/OpsFlow.LoadTests/results/2026-10-14/`.

| Scenario | p50 | p95 | p99 |
| --- | ---: | ---: | ---: |
| PostgreSQL bypass | 5.105 ms | 7.563 ms | 8.352 ms |
| Redis cold miss + PostgreSQL + cache write | 11.777 ms | 17.668 ms | 19.741 ms |
| Redis hit | 5.208 ms | 7.481 ms | 8.097 ms |

Against bypass, the Redis-hit p50 was 2.02% slower (0.98x), p95 was 1.08% lower (1.01x), and p99 was 3.05% lower (1.03x). These small and mixed single-request differences do not establish a meaningful sequential speedup. A cold miss was slower because it reads PostgreSQL and writes the cache.

### Controlled load (25 VUs, 30 seconds, 3 repetitions per scenario)

| Scenario | Median requests/s | Median p50 | Median p95 | Median p99 | Errors |
| --- | ---: | ---: | ---: | ---: | ---: |
| PostgreSQL bypass | 371.197 | 63.198 ms | 106.148 ms | 135.131 ms | 0% |
| Redis hit | 437.897 | 52.459 ms | 86.557 ms | 108.572 ms | 0% |

Using the same percentile from each scenario, latency reduction was 16.99% at p50 (1.205x), 18.46% at p95 (1.226x), and 19.65% at p99 (1.245x). Median throughput was 17.97% higher, or 66.700 requests/s more. Throughput is a separate measurement from latency reduction. The three-run ranges were 61.090–63.344 ms vs 51.905–52.840 ms at p50, 99.021–108.283 ms vs 85.703–86.796 ms at p95, and 123.605–136.377 ms vs 108.153–109.300 ms at p99 (bypass vs Redis).

The API logs contained 12,063 / 11,571 / 11,471 `Services` SELECTs in the three bypass runs, and 1 / 0 / 0 in the Redis runs. The one Redis SELECT was the initial cache-populating read; subsequent requests in the measured hit run were served from cache. These are query-event counts from this one-key local test, not a measurement of PostgreSQL CPU, I/O, or production load. Redis GET logs separately recorded a median around 2–3 ms in the sequential samples, with occasional much larger outliers; they do not include JSON deserialization, and SET duration is not separately instrumented. The API logs and k6 console logs remain in the original machine's temporary directory and are not part of this repository; therefore the database query counts and Redis-operation timings are not independently reviewable from the checked-in CSVs.

### Limits on interpretation

The load runs were executed in A-then-C order rather than interleaved, on a single local environment with a tiny fixed data set. This leaves possible time/order effects and says nothing about production hardware or capacity. The test does not demonstrate a number of supported users. Repeat with alternating A/C runs, capture machine and Docker resource limits, and test representative data before using the result as a broader performance claim. The source CSVs and per-run summaries are in `tests/OpsFlow.LoadTests/results/2026-10-14/`. Original API and k6 logs remain in `%TEMP%\OpsFlowRedisBenchmark` on the machine that ran the benchmark and are not committed.

## Cleanup

After copying any desired result files from `%TEMP%`, verify the Compose project identity and then remove only the disposable benchmark stack and its database volume:

```powershell
docker compose -p opsflow-redis-benchmark -f docker-compose.redis-benchmark.yml ps -a
docker compose -p opsflow-redis-benchmark -f docker-compose.redis-benchmark.yml down --volumes --remove-orphans
```

The project name is deliberately different from `opsflow`; never omit it or substitute the development Compose file in the cleanup command.
