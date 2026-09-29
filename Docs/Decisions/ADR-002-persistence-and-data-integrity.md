# ADR-002 Conditional update and append only history
>**Status:** Proposed, awaiting team approval  
**Owner:** Systems Architect and Backend Lead  
**Replaces part of:** D-003, for the database choice  
**Quality drivers:** ASR-01, ASR-03, ASR-04  
**Risks touched:** RSK-009, RSK-010, RSK-013  
**Research used:** Assignment 2, Task 2 on persistence and data integrity. [insert final section reference]  

## Problem

Two staff members open the queue together and accept the same unassigned request moments apart. AC-011.2 says that must not produce two owners in silence, and NFR-005 extends the promise to status changes. Separately, NFR-007 and AC-013.1 say assignments, transitions and notes must be attributable and must not be quietly altered, because accountability data nobody trusts is worth nothing to management.

These are properties of concurrent writes, so they are decided in the persistence layer rather than left to whichever request arrives first.

## Options

**A. Read the row, test it in code, then save.** Easy to read and wrong under load. Both transactions see an empty owner before either writes, so the second overwrites the first. This is the lost update the research describes, and it fails AC-011.2 with no visible error, which is the worst way to fail.

**B. Lock the row while working on it.** Correct. The price is locks held on a queue several people browse at once, and lock ordering to reason about as operations multiply.

**C. Version column with optimistic checking.** Correct and general, but it needs a version on the entity, that version travelling through the interface, and a conflict path on every write. Heavy machinery for a team whose real exposure is one operation.

**D. Put the condition inside the write.** Update the row only while the owner is empty, then read how many rows changed. One row means the accept worked, zero means somebody else owns it. No gap exists between checking and writing.

## Decision

Option D for assignment and status changes, on PostgreSQL.

1. Accepting updates the row only while it is unowned, and the service checks the affected row count. Zero rows returns a conflict, and success is never reported on a zero row update.
2. Status changes are validated against the allowed set inside the same transaction as the write (AC-012.2). Status is a constrained value, not free text.
3. History is append only. Every assignment, transition and note writes a row carrying actor, action, old value, new value and time, and no path exists to change those rows. Corrections are new entries.
4. Requester feedback is written in the same transaction as the status change, so a requester never sees a state the system did not record.

Option C stays available for any later operation whose condition is too complex for one predicate. That would be a new record.

## What it costs

* Part of a business rule now lives in a database condition rather than domain code, so a reader has two places to look.
* Correctness depends on checking how many rows changed. Ignore that value and the original bug returns with no test failing, which is why the concurrency test is compulsory.
* It cannot be proven with mocked repositories. It needs an integration test with real parallel transactions.
* History tables only grow. Fine at this size, and archiving is a later concern.

## Open point

CR-004 must settle whether an owned request can be reassigned and by whom. If it can, the condition becomes owner plus permission instead of owner is empty, and this record changes through controlled change.

## Verification

* **TC-011.1** one accept records owner and time.
* **TC-011.2** several parallel accepts, exactly one succeeds.
* **TC-012.1 and TC-012.2** valid change recorded with actor and time, invalid change refused.
* **TC-N005** concurrent status writes do not lose an update.
* **TC-N007** history entries appear for each controlled action and no application path alters them.

## Evidence

**RTM:** FR-007, FR-011, FR-012, FR-013, FR-014, FR-022, NFR-005, NFR-007, NFR-008.  
**Requirements:** AC-011.2, AC-012.1, AC-012.2, AC-013.1.  
**Depends on:** ADR-001. **Feeds:** ADR-006 for the conflict response.  
