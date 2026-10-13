# M3 Acceptance Checks

**Build:** the merged release candidate (`frontend-milestone-3-development`) | **Run:** 2026-10-13, 10:27 UTC | **Environment:** local, Development, .NET 10 Release, PostgreSQL 17, freshly seeded database | **Method:** HTTP requests, one cookie session per seeded user

| Checks | What was checked | Result |
| :--- | :--- | :--- |
| BB-03a to BB-03e (FR-008) | Each queue shows only its staff member's categories; a requester is told the queue is for staff; another category's request opened by ID gives 404 with no data | Pass |
| NFR-006 | Accepting an out-of-scope request is refused and it stays Received | Pass |
| AC-010.1 | Staff details show the requester, category and location | Pass |
| AC-011.1, AC-011.2 | Accept records the owner; a second accept is refused; the Accept button is hidden once owned | Pass |
| OBS-02 | The history shows names, not IDs | Pass |
| AC-001.1, AC-001.2, AC-002.1, AC-002.2, BB-02 | Submit gives a unique reference and Received; an empty form flags all 4 fields and saves nothing; 7 categories offered; a 121-character title is refused | Pass |
| AC-003.1, AC-003.2, AC-004.1 | Plain-language status with no control to change it; the new request is listed in My requests | Pass |
| BB-05 | Requester One cannot see Requester Two's 2 requests, in the list or by ID | Pass |
| FR-009 | The queue finds a request by reference; a Closed filter shows "No requests match these filters." | Pass |
| AC-012.1, AC-012.2 | Assigned to Closed is refused with a message; In progress and Resolved are recorded | Pass |
| FR-013 | The owner adds a note; it is saved and shown | Pass |
| AC-014.1, AC-007.1 | Resolving needs a note; the owner closes; the requester sees Closed and 4 timestamped updates, including the resolution | Pass |
| AC-014.2, IT-03 | Closing through the API as Staff Two or as the requester returns 403; the status is unchanged | Pass |
| CR-012 | An administrator closes a resolved request | Pass |
| IT-01 | API create returns 201 with a reference; an empty request returns 400 with an error per field | Pass |

**Result:** all 26 checks pass. Earlier rounds on the builds before the merge found DEF-001 (details missing the requester and category), OBS-01 (Accept button shown on owned requests) and OBS-02 (IDs in the history). All three are fixed and re-checked above.

**Limits:**
- Development identity instead of sign-in (CR-003, RSK-012).
- Local, not staging (B2).
- Pages are driven over HTTP, not in a browser.
- Seed data only, single run.

**Not covered:**
- The NFR-001 walkthrough, which needs a timed first-time user.
- AC-007.2, deferred under CR-009.
