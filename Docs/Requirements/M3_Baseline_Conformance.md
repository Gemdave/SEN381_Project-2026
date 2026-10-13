# M3 Baseline Conformance (requirements side)

**Release candidate:** the merged build on `frontend-milestone-3-development` (to be tagged `m3-rc1`) | **Owner:** M. Malope | **Registers:** `Docs/Risks/RTM/CivicConnect-RTM-v3.0.xlsx`, `Docs/Risks/RTM/CivicConnect-Change-Register-v3.0.xlsx`

**Key:**
- **As planned:** the release candidate follows the M2 baseline.
- **Controlled:** it deviates under a CR or a recorded defect.
- **Open:** the deviation still needs a decision.

| Area | M2 baseline | M3 release candidate | Conformance | Reference |
| :--- | :--- | :--- | :--- | :--- |
| Staff queue scope (FR-008) | Staff see only requests they are authorised for (AC-008.1, AC-008.2) | Queue and details filtered by staff category; an out-of-scope request returns 404 | As planned | UT-04, BB-03 |
| Queue filters (FR-009) | Filter, sort and search within the staff member's scope | Filter bar and `GET /api/v1/queue` with filter, search, sort and paging | As planned | QueueTests, StaffScreensTests |
| Single owner (FR-011, NFR-005) | Conditional update, history in the same transaction (ADR-002) | A second accept gets 409 | As planned | UT-03, IT-02, CR-004 |
| Reassignment (FR-011, AC-011.2) | Open item 5: blocked or authorised reassignment | Blocked; no reassignment in M3 | As planned (decided) | CR-004 |
| Lifecycle (FR-012) | Explicit transition rules (ADR-002) | Staff details page and `POST /api/v1/requests/{id}/status`; invalid moves refused with a message | As planned | UT-01, AC-012.1, AC-012.2 |
| Notes (FR-013) | Notes kept in the history with author and time | Add-note form and notes API | As planned | NoteServiceTests, StaffScreensTests |
| History (NFR-007) | Key actions recorded and never silently altered | Request history is insert-only in the database | As planned | HistoryInsertOnlyTests |
| Rejected status (FR-007, FR-012) | Proposed by CR-009 | Not in the `app/` lifecycle | Controlled | CR-009 (deferred) |
| Closing (FR-014, AC-014.2) | Only staff with close permission may close (ADR-005) | The owning, in-scope staff member or an administrator may close | Controlled | CR-012 |
| Categories (FR-002) | Controlled list, content open (CR-008) | Seven categories held as reference data | As planned | CR-008 |
| Details page (FR-010, AC-010.1) | Requester, category, description, status, history | All shown after the DEF-001 fix | As planned | AC-010.1 |
| Requester screens (FR-001 to FR-004, NFR-003) | Requester pages in the web frontend (ADR-004) | Ported into `app/` (Option A) through Application services | As planned | AC-001 to AC-004, BB-05 |
| Request location (FR-001, AC-001.1) | Location is a required field (AC-001.1, ADR-006) | `Location` column added by migration `AddStaffScopesLocationAndReferences` | As planned | AC-001.1, BB-01 |
| Feedback (FR-007) | In-app feedback on status change | A timestamped update for every change, shown to the requester | As planned | E2E-02, AC-007.1 |
| Deferred Musts (FR-015 to FR-017, FR-019 to FR-021) | Must priority, baselined for M2 | Not built | Controlled | CR-013 |
| Sign-in (NFR-006, D-004) | Mechanism open (CR-003); ADR-005 covers authorisation | Development identity, Development only | Controlled | CR-003 (deferred), RSK-012 |
| Architecture (CR-010, NFR-010) | Separate frontend and service deployables (ADR-001, amended M3) | One deployable; pages go through Application services | Open (architecture) | CR-014, K. Marota |
| API contract (ADR-006) | PATCH for status changes; integer category ids | POST for status changes; GUID category ids | Open (minor) | CR-015, K. Marota |

## Change records

**CR-012: who may close a request**
- **Original decision:** ADR-005 and AC-014.2 said only staff "with close permission" may close.
- **Evidence:**
  - The build has no separate close permission.
  - The policy lets the owning staff member close if their category scope covers the request; an administrator may also close.
  - IT-03, AuthorisationTests and the administrator-close acceptance check pass.
- **Revised decision:** AC-014.2 now reads "a staff member who does not own the request or does not cover its category, and is not an administrator, is blocked from closing".
- **Consequence:**
  - FR-014 keeps its intent.
  - ADR-005 and the Test Plan seed need aligning.
  - A per-staff close permission can return with FR-020 in M4.

**CR-013: Must requirements deferred from the release candidate**
- **Original decision:** all 23 FRs were baselined, and 18 of them are Must.
- **Evidence:** FR-015 to FR-017 and FR-019 to FR-021 are not built (Scope Freeze v2.0). FR-009 and FR-013 were built after the freeze and left this CR.
- **Revised decision:** defer them to M4, with priority unchanged and nothing removed.
- **Consequence:**
  - They carry no M3 verification claim.
  - The release summary lists them as not delivered.
  - CR-005 (overdue, FR-016) follows them.
