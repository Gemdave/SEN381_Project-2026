# ADR-006 REST interface between the browser and the backend
>**Status:** Proposed, awaiting team approval  
**Owner:** Systems Architect and Backend Lead  
**Builds on:** D-002  
**Quality drivers:** ASR-05 for proportion, then ASR-02, ASR-06, ASR-04  
**Research used:** Assignment 2, Task 3 on interfaces and integration. [insert final section reference]  

## Problem

ADR-001 puts a backend between the browser and the data, and ADR-005 makes that the place where permission is decided. The boundary needs a stated contract: what the browser may ask for, what comes back, and what happens when something is refused.

Two behaviours make this more than routine. Validation failures must name each missing field, because AC-001.2 requires every missing field to be flagged rather than the form failing as a whole. And the accept conflict from ADR-002 needs its own outcome, because somebody else already owning a request is nothing like a malformed request.

There are no external integrations at this milestone. Email and text messages stay deferred under conflict C-4, so this is a contract between the team's own browser code and its own backend.

## Options

**A. REST with JSON over HTTPS.** Resource shaped addresses, ordinary status codes, familiar to the team, testable with plain tooling, and a natural fit for a pipeline that runs one permission check before each handler.

**B. GraphQL.** Would suit the oversight views, which want different shapes of totals. It brings a schema layer and per resolver permission checks, which is harder to police than one check per route, and nobody on the team has used it (RSK-001).

**C. Server rendered forms with no interface at all.** Fewest moving parts, but it clashes with ADR-004, which assumes a browser that fetches data and capabilities.

**D. One address per action, all posted.** Maps neatly onto accept and close, but gives up the uniform meanings and status codes that let a contract explain itself.

## Decision

Option A. REST with JSON over HTTPS, versioned at the front of the path as `/api/v1`.

**Operations**

* Post `/requests` to submit, returning the reference and starting status (AC-001.1).
* Get `/requests` to list, with scope applied on the server and filters as query values.
* Get `/requests/{id}` for detail, returning the record plus the caller's capabilities for ADR-004.
* Post `/requests/{id}/assignment` to accept. Assignment is its own thing, not a status field being written.
* Patch `/requests/{id}/status` to move a request, checked against the allowed set.
* Post `/requests/{id}/notes` to add a note. Append only, with no verb offered for changing or removing one.
* Get `/categories` for the controlled list and `/reports/overview` for totals within scope.

**Replies**

* 400 for validation, with one entry per failing field carrying the field and the message (AC-001.2, AC-002.2).
* 401 when not signed in. 403 when not permitted. 404 when the record is outside the caller's scope, so the interface never confirms that somebody else's request exists.
* 409 when another staff member already owns it, naming the owner where the caller may know (AC-011.2).
* 422 for an invalid status move, naming the attempt and the allowed set (AC-012.2).

Every failure carries a stable code as well as a readable message, so the browser reacts to the code rather than matching text. New optional fields and new addresses ship inside v1. Anything removed or given a new meaning needs v2 and a new record.

## What it costs

* Oversight views may need several calls where a query language would have taken one.
* Returning 404 for scope failures makes a genuinely missing record harder to diagnose. The privacy is worth it.
* The capability payload ties this contract to ADR-004, so the two change together.
* Addresses multiply as features arrive, and each must register a permission check or ADR-005 refuses it.

## Verification

* Contract tests for each address covering success and each documented failure.
* **TC-001.2** a missing field returns 400 naming that field.
* **TC-011.2** the losing accept of a concurrent pair returns 409.
* **TC-012.2** an invalid status move returns 422.
* **TC-004.2** another requester's reference returns 404.

## Evidence

**RTM:** FR-001 to FR-005, FR-007 to FR-015, FR-017, FR-021.  
**Requirements:** AC-001.1, AC-001.2, AC-002.2, AC-011.2, AC-012.2.  
**Depends on:** ADR-001, ADR-002, ADR-005. **Feeds:** ADR-004.  
