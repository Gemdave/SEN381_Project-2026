# ADR-002 Conditional update and append-only history

> **Status:** Proposed, awaiting team approval
> **Owner:** Systems Architect and Backend Lead
> **Builds on:** Task 2 persistence and data-integrity decisions
> **Quality drivers:** ASR-01, ASR-03, ASR-04
> **Risks touched:** RSK-009, RSK-010, RSK-013
> **Research used:** Assignment 2, Task 2 — Persistence and Data-Integrity Decisions

## Problem

CivicConnect needs to keep request data consistent when staff update a request.

The main concern is when two staff members try to assign the same unassigned request at nearly the same time. Only one staff member should become the owner, and the second attempt must not silently overwrite the first one (NFR-005).

The system also needs to keep a reliable history of assignments, status changes and notes. These actions need to show who made the change and when it happened (NFR-007).

## Options

**A. Read the row, check it in code, then save.** This is simple, but two staff members could both see an unassigned request before either one saves. The second update could then overwrite the first one.

**B. Lock the row while working on it.** This can prevent conflicting updates, but it adds locking behaviour that becomes harder to manage as more operations are added.

**C. Use a version column with optimistic checking.** This can handle concurrency, but it adds version tracking to the entity and requires a conflict check for each write.

**D. Put the condition inside the update.** The database only updates the request when it is still unassigned. The backend checks the number of affected rows. One row means the assignment succeeded, while zero rows means another staff member already assigned the request.

## Decision

**Option D is selected for assignment and status-related updates, using PostgreSQL.**

For request assignment, the system will:

1. Update the request only when it is still unassigned.
2. Check the number of affected rows.
3. Treat one affected row as a successful assignment.
4. Treat zero affected rows as a conflict instead of reporting success.
5. Save the related request history as part of the same business operation.

Status changes will also be validated against the allowed status values before they are saved.

Request history will be append-only. Each assignment, status transition or note will create a new history entry containing the actor, action, previous value, new value and time. Existing history entries will not be changed or deleted.

## What it costs

* Part of the concurrency rule is handled by the database condition, so developers need to understand both the service logic and the database update.
* The backend must check the affected-row count. Ignoring it could allow the original concurrency problem to return.
* Concurrency behaviour needs integration testing with real database transactions.
* History records will continue to grow. Archiving can be considered later if the system grows.

## Open point

The project still needs to confirm whether an already-owned request can be reassigned and which roles are allowed to do so. If reassignment is allowed, the database condition and authorisation rules will need to be updated through a controlled ADR change.

## Evidence

**RTM:** FR-007, FR-011, FR-012, FR-013, FR-014, FR-022, NFR-005, NFR-007, NFR-008.
**Requirements:** The assignment, status-change and history requirements from Task 2.
**Verification:** Test a successful assignment, two concurrent assignment attempts, valid and invalid status changes, and creation of history entries.
**Depends on:** ADR-001. **Feeds:** ADR-006 for the conflict response.
