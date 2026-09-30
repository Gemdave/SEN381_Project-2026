# ADR-005 One authorization policy for roles and ownership
>**Design pattern decision 2 of 2.**  
**Status:** Proposed, awaiting team approval  
**Owner:** DevOps, Security and Quality Lead  
**Replaces:** D-004, through change request CR-003  
**Quality drivers:** ASR-02 first, then ASR-01, ASR-03  
**Risks touched:** RSK-011, RSK-012  
**Research used:** Assignment 2, Task 1 on design quality and design patterns.  

## Problem

Access control here is two questions, not one.

**Role questions.** Only staff may change a status (AC-012.1). Only an administrator may change roles (AC-020.2). Only staff with the right permission may close a request (AC-014.2).

**Ownership questions.** A requester may read their own requests and nobody else's (NFR-003, AC-004.2). A staff member sees only the queue they are entitled to (AC-008.2).

The second kind cannot be answered from the role alone, because the answer depends on which record is being touched. That rules out anything that only labels endpoints with role names.

## Options

**A. Checks written into each controller.** Local and obvious, and it fails by forgetting. Every new endpoint is another chance to miss one, the same rule gets written repeatedly, and nobody can state the whole policy without reading everything.

**B. Role labels on endpoints only.** Tidy for role questions and structurally unable to answer ownership questions, because the label runs before the record is known. It would satisfy AC-020.2 and leave NFR-003 unprotected.

**C. One policy component asked about user, action and resource.** Both kinds of rule live together in one readable place. List queries additionally carry the caller's scope, so rows they may not see are never loaded.

**D. Enforcement inside PostgreSQL through row level security.** The hardest thing to bypass. It needs per request database context, splits the policy between database and application, and is unfamiliar to the team (RSK-001). Strong, and more than this stage needs.

## Decision

Option C, refusing by default.

1. One policy component runs in the request pipeline before any protected handler. A route with no policy is refused rather than allowed, so forgetting one breaks a feature loudly instead of opening a door quietly.
2. Roles and permissions are stored as data, so granting a role changes behavior without changing code (AC-020.1).
3. Lists are filtered in the query, not after it. The repository receives the caller's scope and applies it, so unauthorized rows never load (AC-004.2, AC-008.2).
4. The same policy issues the capability object ADR-004 consumes, so what the interface shows and what the server allows come from one place.
5. Decisions on controlled actions are written to history with the actor, which gives NFR-007 its record.

How people sign in is a separate matter, still open under CR-003. For the first build slice only, seeded development users behind a clearly marked configuration that is never deployed are acceptable, which is also the contingency RSK-012 records. This record governs what a signed in user may do, whichever mechanism is chosen.

## What it costs

* Indirection. Answering whether someone may do something means opening the policy, not the handler.
* The policy can become a dumping ground. Rules are grouped by resource to limit that.
* Refusing by default surprises developers. A forgotten registration looks like a broken feature, so it has to be written or it gets debugged as a fault.
* Enforcement lives in the application. Anyone with direct database access goes around it. Accepted for now, with option D as the escalation if the threat picture changes.

## Verification

* **TC-N003** a requester asking for somebody else's reference is refused, not shown data.
* **TC-N006** a role matrix test. For every protected endpoint, each role is tried and the expected allow or refuse is asserted.
* **TC-008.2** a request outside a staff member's scope never reaches the browser.
* **TC-014.2** staff without close permission cannot close.
* **TC-020.1 and TC-020.2** granting a role changes what is permitted, and a non administrator cannot change roles.
* A coverage check confirms every protected route has a policy registered.

## Evidence

**RTM:** FR-004, FR-008, FR-010, FR-014, FR-019, FR-020, FR-022, NFR-003, NFR-006.  
**Requirements:** AC-004.2, AC-008.2, AC-014.2, AC-020.1, AC-020.2.  
**Change requests:** CR-003, which closes D-004.  
**Depends on:** ADR-001. **Feeds:** ADR-004 for capabilities, ADR-006 for refusal behavior.  
