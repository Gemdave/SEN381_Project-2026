# ADR-004 Role based view composition
>**Design pattern decision 1 of 2.**  
**Status:** Proposed, awaiting team approval  
**Owner:** Requirements Lead and Frontend Engineer  
**Quality drivers:** ASR-06 first, then ASR-02, ASR-04  
**Risks touched:** RSK-011, RSK-016  
**Assumes:** ADR-003, which selected ASP.NET Core Razor Pages  
**Research used:** Assignment 2, Task 1 on design quality and design patterns.  

## Problem

Four roles look at the same request data and need different things from it. Requesters see their own requests with status they cannot change (AC-003.2). Staff see the queue they are entitled to, with the actions that move a request along. Management sees totals and lifecycle states. Administrators manage users, roles, categories and the audit trail.

The overlap is large, since every role shows identity, category, status and dates. The differences are not decoration. They decide which fields appear and which actions exist, and one of them is a correctness rule: a requester must have no status control anywhere, and must never see another requester's data (NFR-003, AC-004.2).

Left undecided this settles itself badly in Razor Pages. Either each role gets its own `.cshtml` with its own copy of the request markup and the copies drift apart, or one shared page fills with `@if (User.IsInRole(...))` blocks until nobody can say what a manager actually sees without reading every branch.

## Options

**A. A page for each role with its own markup.** Easy to read alone, but every visual change has to be made four times, and the drift lands on the rule that says status must read the same everywhere.

**B. One page with role conditions in the markup.** No duplication, but role logic spreads through the view. Each new rule adds a branch, so one edit can affect everybody. This is the low cohesion outcome the research describes, and in Razor it also puts authorisation vocabulary into the markup.

**C. Shared partials and view components, with a page model for each role.** The partials and view components know how things look and nothing about roles. Each role gets its own page and page model, which loads only what that role may see and hands the view a capability object naming the actions this viewer has.

**D. One page that delegates rendering to a per role service.** A strategy chosen at request time decides what to render. Reasonable where an algorithm varies by role, but here the variation is in composition and visibility, so it adds a layer that does not match the problem and fights the Razor Pages convention of one page per screen.

## Decision

Option C, expressed in Razor Pages terms.

* **Shared partials** hold markup with no role knowledge: `_RequestSummary.cshtml`, `_RequestDetail.cshtml`, `_HistoryList.cshtml`, `_EmptyState.cshtml`, and a status badge tag helper.
* **View components** cover the pieces that need their own query, such as the queue list and the oversight totals, so a page does not have to fetch on their behalf.
* **A page model for each role** is the composition point. `Pages/Requests/*` for the requester, `Pages/Staff/*` for service staff, `Pages/Management/*` for oversight, `Pages/Admin/*` for administration. Each page model loads only the rows that role may see and passes a **capability object** to its view.
* **The capability object** is a small immutable view model, for example `RequestCapabilities` carrying `CanAssign`, `CanTransition`, `CanAddNote` and `CanClose`. It is built on the server by the authorisation policy from ADR-005. The page never works it out from the signed in user itself, so one source of truth decides what a role may do.

The capability object decides whether a control is rendered. It never decides whether an action is allowed. Every handler method, such as `OnPostAcceptAsync`, calls the same policy again before it does anything, so a request forged against a hidden control is refused on its merits. Not rendering a button is a courtesy to the user, not a control.

The status fragment that ADR-003 polls with JavaScript returns the same partial and the same capability object, so the polled view cannot drift from the rendered page.

## What it buys

Status renders in one place, so AC-003.1 and AC-003.2 cannot drift apart between roles. A new role becomes a new folder of pages over partials that already exist, which is how the sponsor view in FR-023 is expected to arrive. The read only requester view, which is how M1 settled conflict C-1, becomes structural: no capability, no control, and the page model never even loads the transition options.

## What it costs

* Indirection. Tracing why a button appears means reading the page model, the capability object and the partial.
* The capability object has to be built and passed on every page that renders an action, which is more ceremony than putting a role check in the markup.
* The capability contract is shared with the backend, so changing it changes ADR-004, ADR-005 and the polled fragment in ADR-006 together.
* Testing costs more than it would with a client side component model. A partial cannot be rendered in isolation as cheaply, so the checks below run as page tests through `WebApplicationFactory` with a seeded user rather than as unit tests.

RSK-016 warns against abstraction that never earns its place, so this record carries a revision trigger. If adding the sponsor view (FR-023, waiting on CR-007) does not reuse the existing partials and view components, the pattern has not paid for itself and should be simplified through controlled change.

## Components

**Partials:** `_RequestSummary.cshtml`, `_RequestDetail.cshtml`, `_HistoryList.cshtml`, `_FilterBar.cshtml`, `_EmptyState.cshtml`, status badge tag helper.
**View components:** `StaffQueueViewComponent`, `OversightTotalsViewComponent`.
**Pages and page models:** `Pages/Requests/Submit.cshtml`, `Pages/Requests/Index.cshtml`, `Pages/Requests/Details.cshtml`, `Pages/Staff/Queue.cshtml`, `Pages/Staff/Details.cshtml`, `Pages/Management/Oversight.cshtml`, `Pages/Admin/Users.cshtml`, `Pages/Admin/Roles.cshtml`, `Pages/Admin/Categories.cshtml`.
**Contract:** the `RequestCapabilities` view model, built by the ADR-005 policy and returned with the polled status fragment defined in ADR-006.

## Verification

* **TC-003.2** no status control is rendered on any requester page, and the requester page model does not load transition options.
* **TC-004.1 and TC-004.2** the requester history page shows own requests only, and another requester's reference is refused by the page model rather than merely hidden.
* **TC-008.1** the staff queue view component returns only permitted rows.
* **TC-N001** three first time users submit in five steps or fewer, inside five minutes.
* Page tests render each role's pages with and without each capability, and assert both the rendered markup and that the matching handler refuses the action when the capability is absent.

## Evidence

**RTM:** FR-001 to FR-005, FR-008, FR-010, FR-015 to FR-021, FR-023, NFR-001, NFR-003.  
**Requirements:** AC-003.1, AC-003.2, AC-004.2, and stakeholder conflict C-1.  
**Change requests:** CR-007 on the sponsor view.  
**Works with:** ADR-003 for the runtime, ADR-005 for enforcement, ADR-006 for the polled fragment.
