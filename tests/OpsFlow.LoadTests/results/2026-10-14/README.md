# Captured Redis benchmark results

These files contain the raw sequential HTTP latency samples and per-run summaries captured on 2026-10-14, plus the six per-run k6 summaries from the controlled load run. The CSVs have been copied from the generated benchmark output with machine-specific temporary log paths removed; the recorded measurements themselves are unchanged.

| File | Contents |
| --- | --- |
| `sequential-samples.csv` | 900 timed HTTP request samples: 3 scenarios × 3 repetitions × 100 requests. |
| `sequential-summary.csv` | p50, p95, and p99 for each of the nine sequential scenario/repetition combinations. |
| `load-summary.csv` | Requests/s, errors, p50/p95/p99, and cache-path event counts for each of the three bypass and three Redis-hit load runs. |

Each run used the same 1,264-byte response body and SHA-256. The CSVs let readers recompute the published percentiles and repetition medians. The original API/k6 logs are not included, so query counts, individual Redis timings, and log-based cache verification cannot be independently checked from this folder. Hardware model, CPU/memory limits, and Docker resource limits were not recorded. Treat these as one local demonstration run, not portable performance or capacity evidence.
