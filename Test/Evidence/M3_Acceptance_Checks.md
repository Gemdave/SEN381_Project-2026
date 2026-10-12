# M3 Acceptance Checks (local release candidate build)

**Environment:** local, Development, .NET 10 Release build, PostgreSQL 17 (`civicconnect_dev`, development seed) | **Method:** HTTP requests with one cookie session per user, reading status codes and page HTML

**Seeded scope:** Staff One covers Fault, Equipment, Security and Other. Staff Two covers IT, Maintenance and Lost Property. The requester has no staff scope.

## Round 1: staff scope and assignment
**Run:** 2026-10-12, 17:59 UTC | **Build:** `frontend-milestone-3-development` at 44759ab plus the development user switcher (committed as e588691)

| ID | Basis | Steps | Expected | Actual | Result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| BB-03a | FR-008, AC-008.1 | Open the queue as Staff One | Only Staff One's categories | REQ-0001 (Equipment), REQ-0002 (Security) | Pass |
| BB-03b | FR-008, AC-008.1 | Open the queue as Staff Two | Only Staff Two's categories | REQ-0003 (IT), REQ-0004 (Maintenance), REQ-0005 (Lost Property) | Pass |
| BB-03c | FR-008, AC-008.2 | Open the queue as the requester | Empty | Empty | Pass |
| BB-03d | FR-008, AC-008.2, NFR-006 | As Staff One, open REQ-0003 directly by its ID | 404, no request data | HTTP 404, reference and title absent from the response | Pass |
| BB-03e | FR-008 (control) | As Staff One, open REQ-0001 by its ID | 200 with details | HTTP 200 | Pass |
| NFR-006 | NFR-006, AC-008.2 | As Staff One, post Accept on REQ-0003 | Refused, request unchanged | HTTP 404; REQ-0003 still Received with no history (checked as Staff Two) | Pass |
| AC-011.1 | FR-011 | As Staff One, accept REQ-0001 (Received) | Owner and timestamp recorded, shown as Assigned | "You now own this request."; status Assigned; history "Assigned to Staff One by Staff One, 18:00:01Z" | Pass |
| AC-011.2 (UI) | FR-011 | Accept REQ-0001 again | Refused, owner unchanged | "Another staff member has already accepted this request."; owner unchanged | Pass |
| AC-010.1 | FR-010 | Open REQ-0001 details as Staff One | Requester, category, description, status, history | Reference, title, status, description and history shown; **requester and category missing** | Partial (DEF-001) |

## Round 2: requester screens and the status flow
**Run:** 2026-10-12, 19:16 UTC | **Build:** `frontend-milestone-3-development` at 593e739 plus the requester port, the status wiring and migration `AddRequestLocation` (committed with this record) | **Automated tests on the same build:** 42 passed, 0 failed, 0 skipped (21 Application, 20 Domain, 1 integration against local PostgreSQL)

| ID | Basis | Steps | Expected | Actual | Result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| AC-002.1 | FR-002 | Open Submit as the requester | Category is a list of the controlled categories only | A select with 7 options: Equipment, Fault, IT, Lost Property, Maintenance, Other, Security | Pass |
| AC-001.2, AC-002.2 | FR-001, FR-002, BB-01 | Submit with every field empty | Each field flagged, nothing saved | HTTP 200, 4 field messages; requester's requests 5 before and after | Pass |
| BB-02 | FR-001 | Submit a title of 121 characters | Refused with a message, no server error | HTTP 200, "Please keep this to 120 characters or fewer." | Pass |
| AC-001.1 | FR-001 | Submit a complete Fault request | Saved with a unique reference and status Received, shown to the requester | Redirect to details: "Saved. Your reference is REQ-001000", status Received | Pass |
| AC-003.1, AC-003.2 | FR-003 | Open the request as the requester | Plain-language status, no way to change it | Status "Received"; no form, select or button on the page | Pass |
| AC-004.1 | FR-004 | Open My requests | The new request is listed with its information | Listed with reference, title, category, status and date; 5 to 6 requests | Pass |
| AC-010.1 (retest) | FR-010 | Open REQ-001000 as Staff One | Requester, category, location, description, reference | All shown | Pass (DEF-001 fixed) |
| AC-012.2 | FR-012, BB-04 | Staff One accepts, then posts Assigned to Closed | Refused with a message, status unchanged | "That status change is not allowed from the current status."; still Assigned | Pass |
| AC-012.1 | FR-012, BB-04 | Move Assigned to In progress to Resolved, with a resolution note | Each step allowed and recorded with actor and time | Both steps applied; history lists Submitted, Assigned and two StatusChanged entries, each with actor and time; note shown | Pass |
| AC-014.2 | FR-014, NFR-006, IT-03 | `PATCH /api/v1/requests/{id}/status` to Closed as Staff Two, then as the requester | Refused, status unchanged | HTTP 403 for both; still Resolved | Pass |
| AC-014.1 | FR-014 | Staff One closes | Closed, resolution recorded, requester gets feedback | Closed for staff and requester; resolution note recorded; **no feedback item** because FR-007 is not built | Partial |
| IT-01 (manual) | FR-001, ADR-006 | `POST /api/v1/requests` valid, then all fields empty | 201 with a reference; 400 with one error per field | HTTP 201 (REQ-001001); HTTP 400 with title, description, location and categoryId | Pass |

## Findings
- **DEF-001 (Minor):** fixed in round 2. The staff details page now shows the requester, category and location.
- **OBS-01:** fixed in round 2. The Accept button shows only on a Received request.
- **OBS-02:** still open. The action history shows user IDs instead of display names.
- **AC-014.1 partial:** the requester gets no feedback item on close, because FR-007 is not built. It is tracked under FR-007 in the scope freeze.

## Limits of this evidence
- **Development identity:** the stand-in user replaces sign-in (CR-003, RSK-012), so these checks say nothing about real authentication.
- **Local only:** this is a local build, not staging (B2). It uses seed data only, with a single run per round.
- **Changed data:** both rounds changed `civicconnect_dev`. REQ-0001 is owned by Staff One, REQ-001000 is Closed and REQ-001001 exists. Rerun on a freshly seeded database.
- **Not covered:**
  - **BB-05 (AC-004.2 between two requesters):** the seed has only one requester. The rule itself is covered by UT-04.
  - **The NFR-001 walkthrough:** it needs a first-time user, timed. The form has the five designed steps, but that is not usability evidence.
