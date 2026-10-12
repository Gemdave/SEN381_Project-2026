# M3 Baseline Conformance (requirements side)

**Release candidate:** `frontend-milestone-3-development` at 3ea53d1 (to be tagged `m3-rc1`) | **Owner:** M. Malope | **Registers:** `Docs/Risks/RTM/CivicConnect-RTM-v3.0.xlsx`, `Docs/Risks/RTM/CivicConnect-Change-Register-v3.0.xlsx`

**Key:** **As planned** means the RC follows the M2 baseline. **Controlled** means it deviates under a CR or a recorded defect. **Open** means the deviation is not yet controlled and needs a decision.

| Area | M2 baseline | M3 release candidate | Conformance | Reference |
| :--- | :--- | :--- | :--- | :--- |
| Staff queue scope (FR-008) | Staff see only requests they are authorised for (AC-008.1, AC-008.2) | Queue and details filtered by staff category; an out-of-scope request returns 404 | As planned | UT-04, BB-03 |
| Single owner (FR-011, NFR-005) | Conditional update, history in the same transaction (ADR-002) | Implemented; a second accept gets 409 | As planned | UT-03, IT-02, CR-004 |
| Reassignment (FR-011, AC-011.2) | Open item 5: blocked or authorised reassignment | Blocked; no reassignment in M3 | As planned (decided) | CR-004 |
| Lifecycle (FR-012) | Explicit transition rules (ADR-002) | Rules and service built, not yet wired to a page or endpoint | Open (IN\* gap) | UT-01, Scope Freeze v1.0 |
| Rejected status (FR-007, FR-012) | Proposed by CR-009 | Not in the `app/` lifecycle | Controlled | CR-009 (deferred) |
| Closing (FR-014, AC-014.2) | Only staff with close permission may close (ADR-005) | Only the owning, in-scope staff member may close | Controlled | CR-012 |
| Categories (FR-002) | Controlled list, content open (CR-008) | Seven categories held as reference data | As planned | CR-008 |
| Details page (FR-010, AC-010.1) | Requester, category, description, status, history | Requester and category not shown | Controlled (defect) | DEF-001 (proposed, Minor) |
| Requester slice (FR-001 to FR-004, FR-007, NFR-001, NFR-003 page) | Requester pages in the web frontend (ADR-004) | Present only in `Backend/`, which CI does not build | Open | Scope Freeze B1 (Option A) |
| Deferred Musts (FR-009, FR-013, FR-015 to FR-017, FR-019 to FR-021) | Must priority, baselined for M2 | Not built | Controlled | CR-013 |
| Sign-in (NFR-006, D-004) | Mechanism open (CR-003); ADR-005 covers authorisation | Development identity, Development only | Controlled | CR-003 (deferred), RSK-012 |
| Architecture (CR-010, NFR-010) | Separate frontend and service deployables (ADR-001, amended M3) | One deployable; pages read the database directly | Open (architecture) | CR-014, raised for K. Marota |

## Change records

**CR-012: who may close a request**
- **Original decision:** ADR-005 and AC-014.2 said only staff "with close permission" may close.
- **Evidence:** the M3 build has no separate close permission. `RequestAccess.MayWorkOn` lets only the staff member who owns the request, and whose category scope covers it, move or close it. UT-04 passed 8/8. The Test Plan seed assumed a per-staff permission (scope freeze B4).
- **Revised decision:** AC-014.2 now reads "a staff member who does not own the request, or whose category scope does not cover it, is blocked from closing".
- **Consequence:**
  - FR-014 keeps its intent: closing is still restricted by role and ownership.
  - The ADR-005 wording and the Test Plan seed need aligning, and UT-04 already tests the new rule.
  - A per-staff close permission can return with FR-020 in M4.

**CR-013: Must requirements deferred from the RC**
- **Original decision:** all 23 FRs were baselined, and 18 of them are Must.
- **Evidence:** by the freeze, FR-009, FR-013, FR-015 to FR-017 and FR-019 to FR-021 are not built in `app/` (Scope Freeze v1.0).
- **Revised decision:** defer them to M4, with priority unchanged and nothing removed.
- **Consequence:**
  - They carry no M3 verification claim.
  - The release summary must list them as not delivered.
  - FR-013's absence leaves AC-014.1 "resolution recorded" partial.
  - CR-005 (overdue, FR-016) follows them.
