# M3 Scope Freeze (v2.0, final for team sign-off)

**Course:** SEN381 | **Milestone:** 3 | **Role:** Requirements & UX | **Basis:** the release candidate on `frontend-milestone-3-development`, scope set by CR-016 | **Replaces:** v0.1 and v1.0 (`Scope_Freeze_Proposal_v0.1.md`)

**Freeze rule:** the requirements below form the M3 release candidate. Nothing else enters it without a CR; remaining work is evidence and defects only. CR-016 brings in the work built after the first freeze, and CR-013 is withdrawn.

**Decision taken:** Option A from v1.0. The requester screens were ported into `app/`, the tree CI builds, so the whole core journey is in one release candidate.

## Release candidate scope

| Req | Pri | What the release candidate does | Evidence | Status |
| :--- | :--- | :--- | :--- | :--- |
| FR-001 to FR-004 | Must | Submit with per-field checks, controlled categories, read-only status, own history | UT-02, UT-05, BB-01, BB-02, BB-05, IT-01, E2E-01, acceptance | IN |
| FR-005 | Should | Requester filters own requests by status | ManagementScreensTests | IN (CR-016) |
| FR-006 | Could | Attach a JPG, PNG or PDF up to 5 MB; type checked from the file itself | RequestAttachmentTests, AttachmentServiceTests, AttachmentTests | IN (CR-016) |
| FR-007 | Must | A timestamped update for every change; a rejection tells the requester why | E2E-02, StatusTransitionServiceTests, UpdatesPollingTests | IN (AC-007.2 by CR-009) |
| FR-008, FR-009 | Must | Category-scoped queue with filter, search, sort and paging | UT-04, BB-03, QueueTests, StaffScreensTests | IN |
| FR-010, FR-011 | Must | Full details; one owner per request, a second accept refused | AC-010.1, UT-03, IT-02, StaffScreensTests | IN |
| FR-012 to FR-014 | Must | Controlled status changes including Rejected, notes, closing by the owner or an administrator (CR-012) | UT-01, BB-04, IT-03, E2E-02, StatusTransitionTests, NoteServiceTests | IN |
| FR-015 to FR-017 | Must | Oversight figures, open and overdue (per-category targets, CR-005), filters by status and category | ManagementScreensTests, ReportingTests, ResponseTargetTests | IN (CR-016) |
| FR-018 | Should | Performance table and CSV export matching the display (CR-006) | ReportingTests | IN (CR-016) |
| FR-019, FR-020 | Must | Create and deactivate users, change roles and staff categories | UserServiceTests, UserAdminTests, RoleMatrixTests | IN (CR-016; development sign-in only, CR-003) |
| FR-021, FR-022 | Must, Should | Category maintenance; audit trail of controlled actions | CategoryServiceTests, ManagementScreensTests, UserAdminTests | IN (CR-016) |
| FR-023 | Should | Sponsor summary with no request details (CR-007) | ReportingTests | IN (CR-016) |
| NFR-002 | Should | A staff change shows on the requester's page within 15 s (5 s polling) | UpdatesPollingTests | IN (CR-016) |
| NFR-003 to NFR-006 | Must, Should | Privacy, queue speed, single owner, role and scope checks | BB-05, PT-01, IT-02, IT-03, RoleMatrixTests | IN |
| NFR-007, NFR-008 | Must, Should | Insert-only history and audit; report figures reconcile with the records | HistoryInsertOnlyTests, ReportingTests | IN (CR-016) |
| NFR-001 | Should | Submit form in five steps | Timed walkthrough | IN\* (walkthrough pending) |

**OUT for M3:** NFR-009 (nothing deployed to cost) and NFR-010 (needs the four-service split, CR-010 and CR-014). Out-of-scope items carry no verification claim.

## Blockers

| ID | Blocker | State | Owner |
| :--- | :--- | :--- | :--- |
| B1 | Requester screens only in `Backend/` | Closed: ported into `app/` | Requirements & UX |
| B2 | No staging until CR-003 closes or the development identity is accepted there | Open: evidence is local only | Team |
| B3 | No test database in CI | Closed on main: CI runs the integration tests against PostgreSQL; the release candidate's run follows its merge. 211 of 211 tests pass locally | Testing & CI |
| B4 | Close permission and category scope | Closed: scope built; the owner or an administrator closes (CR-012) | Testing & CI to align the Test Plan seed |
| B5 | No EF Core migrations | Closed: migrations committed | Architecture |

## Release candidate
Tag `m3-rc1` on `main` once this branch and `frontend-milestone-3-development` are merged.

**Evidence:**
- `Docs/Risks/RTM/CivicConnect-RTM-v3.0.xlsx`
- `Docs/Risks/RTM/CivicConnect-Change-Register-v3.0.xlsx` (CR-016)
- `Test/Evidence/M3_Acceptance_Checks.md` (26 of 26 pass)
- `Test/Evidence/PT-01_Results.md` (p95 0.064 s)

## Agreement

| Member | Role | Agreed (Y/N, date) | Comment |
| :--- | :--- | :--- | :--- |
| | Requirements & UX | | |
| | Architecture | | |
| | Testing & CI | | |
