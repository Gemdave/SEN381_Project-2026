# M3 Scope Freeze Proposal (v0.1, draft for team agreement)

**Course:** SEN381 | **Milestone:** 3 | **Role:** Requirements & UX | **Basis:** `main` at 3462b5a

## 1. Purpose
M3 step 1 asks the team to agree which *implemented* requirements form the release candidate (RC), and then to stop adding features. This proposal starts from the M3 slice in the Test Plan (FR-001, 002, 004, 007, 008, 011, 012, 014, 020; NFR-003 to 006) and checks each item against what the CI-built tree (`app/`) actually contains today.

**Freeze rule:** once this is agreed, no requirement enters the RC without a CR. Work after the freeze goes to closing the gaps below, tests and evidence.

## 2. Key finding
CI builds and tests only `app/`. The requester slice (submit, categories, status, history, feedback) and the status/notes API exist only in `Backend/`, so they cannot count as implemented RC scope (RSK-022). `app/` holds the staff slice: queue, details, accept/assign, the lifecycle rules and the authorisation policy.

## 3. Decision: how to treat the `Backend/` requester slice

| Criterion | A. Port requester slice into `app/` | B. Freeze staff slice only | C. Make `Backend/` canonical |
| :--- | :--- | :--- | :--- |
| Covers the core requester journey (E2E-01, NFR-001) | Strong | Weak | Strong |
| Keeps M2 conformance (ADR-002, ADR-004, CI) | Strong | Strong | Weak (re-point CI, different data access) |
| Effort before freeze | Fair (4 pages, 2 services) | Strong (none) | Weak |
| Test Plan cases still usable | Strong (all) | Weak (loses UT-05, BB-01, BB-02, BB-05, IT-01, E2E-01) | Fair (IDs survive, code paths change) |

**Recommendation: Option A.** FR-001 to FR-004 are Musts and the only requester-facing journey. Without them the RC has no submit path and we cannot run the NFR-001 walkthrough. This also unblocks M3 item 5.

## 4. Proposed RC scope
Status key: **IN** is frozen in scope; **IN\*** is in scope only if the gap closes by the freeze date, otherwise it is deferred with this row as the reason; **OUT** is deferred.

| Req | Pri | In `app/` today | Gap before RC | Proposed | Test Plan cases |
| :--- | :--- | :--- | :--- | :--- | :--- |
| FR-001 | Must | No (`Backend/` only) | Port submit page, service, validation | IN\* | UT-05, BB-01, BB-02, IT-01, E2E-01 |
| FR-002 | Must | Category entity and seed only | Port category list into the submit form | IN\* | UT-02 |
| FR-003 | Must | No | Port requester details view (E2E-01 uses AC-003.1) | IN\* | E2E-01 |
| FR-004 | Must | Requester View rule only | Port history page | IN\* | BB-05, E2E-01 |
| FR-007 | Must | No | Port feedback; needs FR-012 wired first | IN\* (stretch) | E2E-02 |
| FR-008 | Must | Queue page, no team/category scope | Add scope, or narrow AC-008.1 by CR | IN | BB-03 |
| FR-010 | Must | Staff details page | None | IN (added, already built) | Walked in E2E-02 |
| FR-011 | Must | Yes: service, `POST .../assignment` with 409, UI, tests | CI test DB so IT-02 actually runs | IN | UT-03, IT-02, E2E-02 |
| FR-012 | Must | Rules and service, not wired to any page or endpoint | Status endpoint and Details actions | IN\* | UT-01, BB-04, E2E-02 |
| FR-014 | Must | Close rule in policy (assigned staff only), no flow | Comes with FR-012 (Resolved to Closed) | IN\* | UT-04, IT-03, E2E-02 |
| FR-020 | Must | Not built in either tree; roles are seeded | None feasible before freeze | OUT | Drop UT-04's role-change rows |
| NFR-001 | Should | No | Depends on FR-001 | IN\* | Usability walkthrough |
| NFR-003 | Must | Requester View rule | Depends on FR-004 | IN\* | BB-05 |
| NFR-004 | Should | Queue query (capped at 50 rows) | Staging (see B2) | IN\* | PT-01 |
| NFR-005 | Must | Guarded update and concurrency test | CI test DB (see B3) | IN | UT-03, IT-02 |
| NFR-006 | Must | Fail-closed policy, 403 on assign | None | IN | UT-04, IT-03 |

**OUT for M3:**
- FR-013 is a Must but exists in `Backend/` only, so AC-014.1 "resolution info recorded" is partial.
- FR-005, FR-006 and FR-009 are below the core journey.
- FR-015 to FR-019 and FR-021 to FR-023 cover management, admin and reporting, and are not built.
- NFR-002 (`status-poll.js` exists but has no requester view), NFR-007 and NFR-008 to NFR-009.

Out-of-scope items carry no verification claim.

## 5. Blockers to settle at the freeze meeting
| ID | Blocker | Affects | Owner (proposed) |
| :--- | :--- | :--- | :--- |
| B1 | Requester slice exists only in `Backend/` | FR-001 to 004, 007, NFR-001, 003 | Team decision (Section 3), then frontend and backend |
| B2 | `Program.cs` refuses to start outside Development until CR-003 closes, so no staging RC | E2E-01, E2E-02, PT-01, M3 deployment evidence | Decide: close CR-003, or run staging with the development identity and record it under RSK-012 |
| B3 | CI has no PostgreSQL, so ConcurrentAcceptTests is skipped (skipped counts as blocked, not passed) | IT-01 to IT-03, NFR-005 | Gerald (CI) |
| B4 | Test Plan seed assumes staff category scope and per-staff close permission; `app/` has neither | BB-03, UT-04 expected results | Implement scope, or raise a CR narrowing AC-008.1 and AC-014.2 |

## 6. Release candidate identifier
Proposed tag: `m3-rc1` on `main` once the IN\* gaps are closed or deferred. All execution evidence and the RTM M3 columns must reference that tag.

## 7. Agreement
| Member | Role | Agreed (Y/N, date) | Comment |
| :--- | :--- | :--- | :--- |
| | Requirements & UX | | |
| | Architecture | | |
| | Testing & CI | | |
| | | | |
