# Phase 8 load-test result

Run date: 2026-10-10. Target: a separate local Docker Compose stack with a newly created PostgreSQL volume, so generated incidents did not go into the existing development database. Load duration: 30 seconds per scenario, running concurrently; maximum configured concurrency: 160 VUs. k6 ran in the `grafana/k6:latest` Docker image.

| Operation | VUs | Requests | Requests/s | Errors | p50 | p95 | p99 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| GET services | 100 | 17,235 | 549.96 | 0% | 143.47 ms | 415.68 ms | 592.46 ms |
| GET incidents | 50 | 3,832 | 122.28 | 0% | 266.37 ms | 1.12 s | 1.58 s |
| POST incident | 10 | 759 | 24.22 | 0% | 378.27 ms | 738.58 ms | 1.08 s |

The run completed all 21,826 scenario iterations (21,828 HTTP requests including setup) at an overall 696.51 requests/s and passed all three error-rate thresholds (less than 1%). The request rates and latencies describe this one local run; they are useful as a baseline, not a production capacity guarantee. Hardware, Docker resource limits, database size, and background services affect the numbers.
