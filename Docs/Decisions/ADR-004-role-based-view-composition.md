# ADR-004 Role based view composition
>**Design pattern decision 1 of 2.**  
**Status:** Proposed, revision 2 (08/10/2026); to align with the revised ADR-001 (CR-010), ADR-005 and ADR-006  
**Owner:** Requirements Lead and Frontend Engineer  
**Quality drivers:** ASR-06 first, then ASR-02, ASR-05, ASR-07, ASR-04  
**Risks touched:** RSK-011, RSK-016, RSK-022  
**Assumes:** ADR-003 (ASP.NET Core Razor Pages) and CR-010 (microservices)  
**Research used:** Assignment 2, Task 1 on design quality and design patterns (Naghdipour, 2023; Silva, 2021)

| Rev | Date | Change |
| :--- | :--- | :--- |
| 1 | 29/09/2026 | How each role's pages are built inside one application (now P3). Full text in commit `ad2a501`. |
| 2 | 08/10/2026 | Adds Part 1 for microservices (CR-010) and a comparison table for each part; the P3 decision is unchanged. |

## Problem

Four roles use the same request data but need different fields and actions. Requesters see their own requests with read-only status (AC-003.2) and must never see another requester's data (NFR-003, AC-004.2). Staff see their permitted queue and actions, management sees totals, and administrators manage users, roles and categories. Left undecided, Razor Pages drifts into either copied markup per role or one page full of role checks.

With microservices (CR-010), one screen draws on several services. This record therefore answers two questions: where the screens get their data (Part 1) and how each role's pages are built (Part 2).

## Scoring

Each option is rated Strong, Fair or Weak per criterion; criteria come from the ASRs, requirements or risks named in them and are listed in priority order (the first three are equally important in Part 1, the first two in Part 2). Growth (ASR-07) ranks below cost in Part 1 because no growth figures exist and the scope is deliberately small. Build effort ranks lower in Part 2 than in Part 1, because those options differ only in coding effort inside one service, not in services to build, host and pay for.

## Part 1: where the screens get their data

* **A. The browser calls each service.** Every service faces the internet, and the screens are rewritten client-side.
* **B. A gateway merges responses.** One call per screen, but role shaping moves into shared infrastructure.
* **C. One web frontend service.** A single backend-for-frontend serves every role from role-separated page folders, calls the services server-side and reuses the existing pages.
* **D. A frontend service per role group.** Each role scales and releases alone, at the cost of three or four more deployables.
* **E. Micro-frontends.** Each service deploys its own piece of the screen: the most independence and the most work.

| Criterion (priority order) | A | B | C | D | E |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ASR-02** Privacy | **Weak** Every service exposed to the browser | **Fair** Role shaping in shared infrastructure | **Strong** Services hidden; controls set server-side | **Strong** Smallest exposure per role | **Fair** Many exposed pieces |
| **ASR-06** Consistent screens | **Fair** Status logic repeated in browser code | **Fair** Consistent only if the gateway shapes data alike | **Strong** One shared status partial | **Fair** Shared partials copied or packaged per service | **Weak** Pieces can look and behave differently |
| **ASR-05** Cost for a team of three | **Weak** Screens rewritten client-side | **Fair** Custom merge code in the gateway | **Strong** One extra deployable reusing existing pages | **Weak** Three or four deployables, each with its own pipeline, configuration, secrets and hosting slot (NFR-009) | **Weak** Most build and deployment work |
| **ASR-07** Growth | **Fair** Screens change when services split | **Fair** Every screen change passes the gateway | **Fair** Runs as copies; splits per role later | **Strong** Each role scales and releases alone | **Strong** Each piece scales and releases alone |
| **ASR-04** Oversight speed | **Weak** Many browser round trips | **Strong** One call per screen | **Fair** Several server-side calls | **Strong** Tailored per role | **Fair** Pieces load separately |
| **Overall** | No Strong, 3 Weak | 1 Strong, no Weak | **3 Strong, no Weak** | 3 Strong, Weak on cost | 1 Strong, 2 Weak |

## Part 2: how each role's pages are built

* **P1.** A page per role with its own markup (option A in revision 1).
* **P2.** One page with role conditions in the markup (B).
* **P3.** Shared partials and view components, a page model per role, and a capability object (C).
* **P4.** One page that hands rendering to a per-role strategy (D), which also carries Strategy's own maintenance cost (Silva, 2021).

| Criterion (priority order) | P1 | P2 | P3 | P4 |
| :--- | :--- | :--- | :--- | :--- |
| **ASR-06** Status the same for all roles | **Weak** Four copies drift | **Fair** Branches multiply | **Strong** One partial per status piece | **Fair** Rendering split across strategies |
| **ASR-02** No role logic in markup | **Fair** Loading rules repeated | **Weak** Role checks spread through views | **Strong** Server-built capability object; handlers re-check | **Fair** Logic in strategies |
| **ASR-05** Build effort | **Fair** Every change made four times | **Strong** Fastest | **Fair** Capability object to build and pass | **Weak** Extra layer against Razor conventions |
| **Changeability**, from FR-023 and RSK-016 | **Weak** Copy everything | **Weak** New branches affect all roles | **Strong** New page folder on existing partials | **Fair** New strategy plus wiring |
| **Testability**, from RTM verification evidence | **Fair** Each page tested four times | **Weak** Every branch per role | **Fair** Page tests with a seeded user | **Strong** Strategies tested alone |
| **Overall** | No Strong, 2 Weak | 1 Strong, 3 Weak | **3 Strong, no Weak** | 1 Strong, 1 Weak |

## Decision

**Option C, with P3 inside it.**
* The browser talks only to the frontend service. It calls the back-end services through the ADR-006 interfaces and keeps no in-memory session state, so it can run as several copies.
* Shared partials (`_RequestSummary`, `_RequestDetail`, `_HistoryList`, `_EmptyState`, the status badge tag helper) carry no role knowledge. View components cover the queue list and the oversight totals.
* Each role has its own page folder (`Pages/Requests`, `Pages/Staff`, `Pages/Management`, `Pages/Admin`), and its page models load only what that role may see.
* `RequestCapabilities` decides only whether a control is drawn. Its values come from the owning service using the ADR-005 policy, and that service re-checks every action, so a hidden button is a courtesy, not a control. The polled status fragment uses the same partial and capability object.

C and D both have three Strong ratings, but D is Weak on cost, one of the three top-priority criteria, because it adds three or four deployables now (ASR-05), while C's page folders are where D can be split out later.

## Consequences

**Buys:** status renders in one place for every role; a new role is a new page folder over existing partials, which is how FR-023 is expected to arrive; the services stay hidden from the browser; the frontend can run as several copies.  
**Costs:** indirection across page model, capability object and partial; one more deployable and network hop per screen; a frontend defect can affect every role; a multi-service screen is as slow as its slowest service; the capability contract changes together with ADR-005 and ADR-006; page tests need the services faked or running.  
**Revision triggers:** simplify P3 if the sponsor view (FR-023, CR-007) cannot reuse the partials (RSK-016). Split per role (option D) only if measurement shows one role holding the others back.

## Components

**Exist:** both code trees (RSK-022) have `_RequestSummary`, `_HistoryList`, `_EmptyState`, `StatusBadgeTagHelper`, the staff Queue and Details pages, `RequestCapabilities` and `CapabilityFactory`. The capability fields differ: `Backend/` uses `CanAccept`, `CanTransition`, `CanSeeInternals` and `NextStatuses`; `app/` uses `CanAccept`, `CanTransition`, `CanAddNote` and `CanClose`. Only `Backend/` has the requester pages and `_RequestFields`, and only `app/` has `status-poll.js`.  
**Planned:** the frontend service, `_RequestDetail`, `_FilterBar`, `StaffQueueViewComponent`, `OversightTotalsViewComponent`, and the management and admin pages.

## Verification

* **TC-003.2:** no status control on any requester page.
* **TC-004.1, TC-004.2:** own requests only; another requester's reference is refused, not just hidden.
* **TC-008.1:** the staff queue shows only permitted rows.
* **TC-N001:** three first-time users submit in five steps or fewer, inside five minutes.
* Page tests with and without each capability check both the markup and that the action is refused.
* The browser never calls a back-end service directly, and two frontend copies serve the same pages (NFR-010).

## Evidence

**RTM:** FR-001 to FR-005, FR-008, FR-010, FR-015 to FR-021, FR-023, NFR-001, NFR-003, NFR-010. **Requirements:** AC-003.1, AC-003.2, AC-004.2; conflicts C-1 and C-5. **Change requests:** CR-007, CR-010, CR-011. **Works with:** the revised ADR-001, ADR-003, ADR-005 and ADR-006.
