# M3 Scope Freeze (v2.0, final for team sign-off)

**Course:** SEN381 | **Milestone:** 3 | **Role:** Requirements & UX | **Basis:** the merged release candidate on `frontend-milestone-3-development` | **Replaces:** v0.1 and v1.0 (`Scope_Freeze_Proposal_v0.1.md`)

**Freeze rule:** the requirements below form the M3 release candidate. Nothing else enters it without a CR; remaining work is evidence and defects only.

**Decision taken:** Option A from v1.0. The requester screens were ported into `app/`, the tree CI builds, so the whole core journey is in one release candidate.

## Release candidate scope

| Req | Pri | What the release candidate does | Evidence | Status |
| :--- | :--- | :--- | :--- | :--- |
| FR-001 to FR-004 | Must | Submit with per-field checks, controlled categories, read-only status, own history | UT-02, UT-05, BB-01, BB-02, BB-05, IT-01, E2E-01, acceptance | IN |
| FR-007 | Must | A timestamped update for every change, shown to the requester | E2E-02, AC-007.1 | IN (AC-007.2 deferred, CR-009) |
| FR-008, FR-009 | Must | Category-scoped queue with filter, search, sort and paging | UT-04, BB-03, QueueTests, StaffScreensTests | IN |
| FR-010, FR-011 | Must | Full details; one owner per request, a second accept refused | AC-010.1, UT-03, IT-02 | IN |
| FR-012 to FR-014 | Must | Controlled status changes, notes, closing by the owner or an administrator (CR-012) | UT-01, BB-04, IT-03, E2E-02, NoteServiceTests | IN |
| NFR-003 to NFR-006 | Must, Should | Privacy, queue speed, single owner, role and scope checks | BB-05, PT-01, IT-02, IT-03 | IN |
| NFR-007 | Must | Request history is insert-only in the database | HistoryInsertOnlyTests | IN (FR-022 view OUT) |
| NFR-001 | Should | Submit form in five steps | Timed walkthrough | IN\* (walkthrough pending) |

**OUT for M3:**
- **Deferred Musts, under CR-013:** FR-015 to FR-017 (oversight) and FR-019 to FR-021 (administration).
- **Shoulds and Coulds:** FR-005, FR-006, FR-018, FR-022 and FR-023.
- **Non-functional:** NFR-002, NFR-008, NFR-009 and NFR-010.

Out-of-scope items carry no verification claim.

## Blockers

| ID | Blocker | State | Owner |
| :--- | :--- | :--- | :--- |
| B1 | Requester screens only in `Backend/` | Closed: ported into `app/` | Requirements & UX |
| B2 | No staging until CR-003 closes or the development identity is accepted there | Open: evidence is local only | Team |
| B3 | No test database in CI, so integration and E2E tests skip there | Open: all 76 automated tests pass locally | Testing & CI |
| B4 | Close permission and category scope | Closed: scope built; the owner or an administrator closes (CR-012) | Testing & CI to align the Test Plan seed |
| B5 | No EF Core migrations | Closed: migrations committed | Architecture |

## Release candidate
Tag `m3-rc1` on `main` once this branch and `frontend-milestone-3-development` are merged.

**Evidence:**
- `Docs/Risks/RTM/CivicConnect-RTM-v3.0.xlsx`
- `Test/Evidence/M3_Acceptance_Checks.md` (26 of 26 pass)
- `Test/Evidence/PT-01_Results.md` (p95 0.064 s)

## Agreement

| Member | Role | Agreed (Y/N, date) | Comment |
| :--- | :--- | :--- | :--- |
| | Requirements & UX | | |
| | Architecture | | |
| | Testing & CI | | |
