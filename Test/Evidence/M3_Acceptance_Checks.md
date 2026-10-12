# M3 Acceptance Checks (local release candidate build)

**Run:** 2026-10-12, 17:59 UTC | **Build:** `frontend-milestone-3-development` at 44759ab plus the development user switcher (committed with this record) | **Environment:** local, Development, .NET 10 Release build, PostgreSQL 17 (`civicconnect_dev`, development seed) | **Method:** HTTP requests with one cookie session per user, reading status codes and page HTML

**Seeded scope:** Staff One covers Fault, Equipment, Security and Other. Staff Two covers IT, Maintenance and Lost Property. The requester has no staff scope.

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
| AC-010.1 | FR-010 | Open REQ-0001 details as Staff One | Requester, category, description, status, history | Reference, title, status, description and history shown; **requester and category missing** | Partial |

## Findings
- **DEF-001 (proposed, Minor):** the staff details page does not show the requester or the category, so AC-010.1 is only partly met. Fix: add both fields to `Pages/Staff/Details`.
- **OBS-01:** the Accept button still shows on a request that is already owned. The service refuses the action, so AC-011.2 holds, but the message says "another staff member" even when you own it yourself.
- **OBS-02:** the action history shows user IDs instead of display names.

## Limits of this evidence
- **Development identity:** the stand-in user replaces sign-in (CR-003, RSK-012), so these checks say nothing about real authentication.
- **Local only:** this is a local build, not staging (B2). It uses seed data only and is a single run.
- **Changed data:** AC-011.1 changed `civicconnect_dev`, so REQ-0001 is now owned by Staff One. Rerun it on a freshly seeded database.
- **Not covered:**
  - The NFR-001 walkthrough and the FR-001 to FR-004 checks wait on the requester screens (B1).
  - The FR-012 and FR-014 checks wait on the status wiring.
