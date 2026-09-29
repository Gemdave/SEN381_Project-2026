# ADR-004 Role based view composition
>**Design pattern decision 1 of 2.**  
**Status:** Proposed, awaiting team approval  
**Owner:** Requirements Lead and Frontend Engineer  
**Quality drivers:** ASR-06 first, then ASR-02, ASR-04  
**Risks touched:** RSK-011, RSK-016  
**Research used:** Assignment 2, Task 1 on design quality and design patterns.  

## Problem

Four roles look at the same request data and need different things from it. Requesters see their own requests with status they cannot change (AC-003.2). Staff see the queue they are entitled to, with the actions that move a request along. Management sees totals and lifecycle states. Administrators manage users, roles, categories and the audit trail.

The overlap is large, since every role shows identity, category, status and dates. The differences are not decoration. They decide which fields appear and which actions exist, and one of them is a correctness rule: a requester must have no status control anywhere, and must never see another requester's data (NFR-003, AC-004.2).

Left undecided this settles itself badly. Either each role gets its own copy of the request markup and the copies drift apart, or one shared template fills with role conditions until nobody can say what a manager actually sees without reading every branch.

## Options

**A. A page for each role.** Easy to read alone, but every visual change has to be made four times, and the drift lands on the rule that says status must read the same everywhere.

**B. One template with role conditions inside.** No duplication, but role logic spreads through the markup. Each new rule adds branches, so one edit can affect everybody. This is the low cohesion outcome the research describes.

**C. Presentation pieces plus a container for each role.** Presentation components know how things look and nothing about roles. A container for each role fetches what that role may see, arranges those pieces, and passes a capability object naming the actions this viewer has.

**D. A strategy for each role behind one view.** Sound where an algorithm varies by role. Here the variation is in what is shown and arranged, so it adds a layer that does not match the problem.

## Decision

Option C. Presentation components stay role blind. Containers for each role arrange them and pass a capability object issued by the backend.

The capability object decides whether a control is drawn. It never decides whether an action is allowed. Every action is authorised on the server by ADR-005 whatever the browser renders. A hidden button is a courtesy, not a control, and that separation is what makes this safe to decide in the interface layer at all. Capabilities come from the interface rather than being worked out again in the browser, so one source of truth decides what a role may do.

## What it buys

Status renders in one place, so AC-003.1 and AC-003.2 cannot drift apart. A new role becomes a new container over parts that already exist. Presentation pieces can be tested without signing in, which keeps the usability walkthrough cheap. The read only requester view, which is how M1 settled conflict C-1, becomes structural: no capability, no control.

## What it costs

* Indirection. Tracing why a button appears passes through container, capability and presentation component.
* Data and capabilities are passed down the tree, which is more ceremony than rendering directly.
* The capability contract is shared with the backend, so changing it changes ADR-004 and ADR-006 together.

RSK-016 warns against abstraction that never earns its place, so this record carries a revision trigger. If adding the sponsor view (FR-023, waiting on CR-007) does not reuse the existing presentation pieces, the pattern has not paid for itself and should be simplified through controlled change.

## Components

Presentation: request summary, request detail, status badge, history list, filter bar, empty state.
Containers: requester request view, requester history view, staff queue view, staff detail view, management oversight view, administrator console.
Contract: the capability payload served with the request resource by ADR-006.

## Verification

* **TC-003.2** no status control appears in any requester view.
* **TC-004.1 and TC-004.2** the history container shows own requests only, and another requester's reference is refused by the interface rather than merely hidden.
* **TC-008.1** the staff queue shows only permitted rows.
* **TC-N001** three first time users submit in five steps or fewer, inside five minutes.
* Component tests render each presentation piece with and without each capability.

## Evidence

**RTM:** FR-001 to FR-005, FR-008, FR-010, FR-015 to FR-021, FR-023, NFR-001, NFR-003.  
**Requirements:** AC-003.1, AC-003.2, AC-004.2, and stakeholder conflict C-1.  
**Change requests:** CR-007 on the sponsor view.  
**Works with:** ADR-005 for enforcement, ADR-006 for the payload.  
