# PT-01 Results (NFR-004)

**Target:** the staff queue loads with a 95th percentile at or below 3 s with 1,000 requests, and zero errors.

**Script:** `Test/Performance/PT-01_queue.sh`. It seeds 1,000 requests through the API, then runs the workload on `/Staff/Queue` as Dev Staff One: 3 warm-up requests, 20 sequential, then 5 users for 30 s.

| Build (2026-10-13) | Phase | Requests | Median | p95 | Max | Errors |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Before the merge | Sequential | 20 | 0.014 s | 0.052 s | 0.075 s | 0 |
| Before the merge | 5 users | 376 | 0.031 s | 0.065 s | 0.242 s | 0 |
| Release candidate | Sequential | 20 | 0.011 s | 0.016 s | 0.030 s | 0 |
| Release candidate | 5 users | 403 | 0.033 s | 0.064 s | 0.302 s | 0 |

**Result:** Pass. The automated `QueueTests` also check the 3 s target at 1,000 requests.

**Limits:**
- Local Release build, not staging, so free-tier cold starts are not measured (B2).
- curl starts a process per request, which caps the load at about 13 requests a second.
- The page shows 20 rows at a time.
