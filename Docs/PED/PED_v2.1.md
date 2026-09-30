# CivicConnect Requirements & Engineering Baseline (PED) (v2.1)

**Authors:** Gerald Enright (577830) | Keletso Marota (601632) | Mogau Malope (600192)  
**Course Code:** SEN381  
**Date:** 30/09/2026  

---

## Table of Contents
1. [Problem Statement](#problem-statement)
2. [CivicConnect: Stakeholder Profiles](#civicconnect-stakeholder-profiles)
   - [Stakeholder Profiles](#stakeholder-profiles)
   - [Stakeholder Conflicts](#stakeholder-conflicts)
3. [Scope](#scope)
   - [In-Scope (M1 Baseline)](#in-scope-m1-baseline)
   - [Out-of-Scope (M1)](#out-of-scope-m1)
   - [Deferred / Future Scope](#deferred--future-scope)
   - [Defense of a Deliberate Deferment](#defense-of-a-deliberate-deferment)
4. [Functional Requirements](#functional-requirements)
   - [ Requester](#1-requester)
   - [ Service Staff](#2-service-staff)
   - [ Management / Oversight](#3-management--oversight)
   - [ Administrator](#4-administrator)
   - [ Sponsor](#5-sponsor)
5. [Non-Functional Requirements & Open Items](#non-functional-requirements--open-items)
6. [CivicConnect: Requirements Traceability Matrix (RTM)](#civicconnect-requirements-traceability-matrix-rtm)
7. [Technical Constraints and Assumptions](#technical-constraints-and-assumptions)
   - [Initial Architecture Strategy](#initial-architecture-strategy)
   - [Architecture, Technology & Initial Design Baseline (M2)](#architecture-technology--initial-design-baseline-m2)
   - [Implementation Notes: Points Requiring Reconciliation](#implementation-notes-points-requiring-reconciliation)
   - [Engineering Decision Log](#engineering-decision-log)
   - [Technical Constraints](#technical-constraints)
8. [Risk Register](#risk-register)
9. [Forward Engineering Considerations](#forward-engineering-considerations)
10. [Baseline Sign-Off](#baseline-sign-off)
11. [References](#references)

---

## Problem Statement

CivicConnect currently manages community service requests through an uncoordinated, ad-hoc mix of email, phone calls, WhatsApp messages, and unlinked spreadsheets. No central channel owns the full lifecycle of a request. As such, the organization lacks a reliable, verifiable method to answer three fundamental operational questions:

1. **What has been requested?**
2. **Who is accountable for acting on the issue?**
3. **Has the issue actually been resolved?**

Recent empirical research in public sector digital transformation highlights that replacing fragmented, message-based administrative processes with centralized, controlled request management platforms is critical for establishing operational transparency, resolving service bottlenecks, and rebuilding stakeholder trust (Gong, 2020; Mergel, 2019).

Thus, the underlying business need is not merely "a website to log requests," but a single, controlled record of the request lifecycle. This platform must grant requesters real-time visibility, provide operational staff with an accountable queue, and supply management with auditable, reportable performance data—all achieved without introducing unsustainable cost, complexity, or technical risk.

---

## CivicConnect: Stakeholder Profiles

### Stakeholder Profiles

| ID | Stakeholder | Description (CivicConnect context) | Key Needs | Influence | Interest |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ST-1** | Requester | Community/staff member who logs a request (fault, equipment, security, IT, maintenance, lost property…). Primary user for M1 FRs. | Easy submit; receipt; status; history; feedback. | Low to Medium | High |
| **ST-2** | Service Staff | Receive, own, action, and resolve requests; struggle to prioritize across channels today. | Queue; detail; assign; control status; record actions. | Medium | High |
| **ST-3** | Management | Accountable for service performance; reporting is manual/hard to audit today. | Open/overdue/resolved visibility; accountability data. | High | Medium to High |
| **ST-4** | Administrator | Maintains users, roles/permissions, and the category list. | Role-Based Access Control; reference data; reliable record. | Medium | Medium |
| **ST-5** | Sponsor | The organization funding and owning the platform. | Visibility and accountability without unsustainable cost. | High | High |

### Stakeholder Conflicts

| ID | Tension | Between | Implication for FRs |
| :--- | :--- | :--- | :--- |
| **C-1** | Instant visibility vs. controlled/accurate status | Requester – Service Staff / Management | Status shown to requester as read-only; advances only via staff approval or actions (FR-003). |
| **C-2** | Low-friction submit vs. enough detail to action | Requester – Service Staff | Balance required vs. optional fields (FR-001). |
| **C-3** | Free-text vs. reportable structured data | Requester – Management | Choosing between a controlled category list or free text (FR-002). |
| **C-4** | Rich notifications vs. sustainable cost/scope | Requester – Sponsor | In-app feedback baselined (FR-007); email/SMS deferred to future scope. |

---

## Scope

The baseline scope defines what CivicConnect will deliver as part of the M1 requirements baseline, what is excluded from this phase, and what has been left for a later milestone. This boundary helps guide the requirements, acceptance criteria, and architecture work that follows. It does not change the CivicConnect scenario but instead sets a clear point at which the M1 work ends.

### In-Scope (M1 Baseline)
* The M1 baseline includes the Requester (ST-1) request lifecycle:
  * Submitting a new request (FR-001).
  * Selecting a category from a controlled list (FR-002).
  * Viewing the status of a request (FR-003).
  * Viewing previous requests (FR-004).
  * Searching and filtering request history (FR-005).
  * Receiving feedback in the application when status changes (FR-007).
* Controlled category list maintained as reference data to support reporting by Management.
* Read-only status view for requesters; status updates restricted to Service Staff (resolves C-1).
* Engineering baseline documentation: stakeholder analysis, technical constraint analysis, initial architecture strategy, and Engineering Decision Log.

### Out-of-Scope (M1)
* Final technology stack selection, detailed software architecture, database schema design, user interface implementation, API implementation, design-pattern implementation, CI pipeline implementation, and production deployment (planned for M2+).
* Detailed functional requirements and acceptance criteria for Service Staff (ST-2), Management (ST-3), and Administrator (ST-4).
* Authentication and detailed authorization implementation (data privacy rule defined in NFR-003; technical strategy deferred to M2 per D-004).

### Deferred / Future Scope
* **Email & SMS Notifications:** In-app status feedback baselined via FR-007. Additional channels deferred pending Sponsor cost constraints (C-4).
* **Attachments (FR-006):** Allowed file types, size limits, and storage strategy to be confirmed; remains at *Could* priority.
* **Full Administrator RBAC:** Specific roles, permissions, and workflows to be defined in a subsequent milestone.

| Boundary | Represents | Controls |
| :--- | :--- | :--- |
| **In-Scope** | FR-001–FR-005, FR-007 and acceptance criteria; controlled category list; read-only status model. | What M1 commits to build and what later evidence (design, test, release) must trace to. |
| **Out-of-Scope** | Architecture, technology, schema, UI/API/CI implementation; ST-2/ST-3/ST-4 detailed FRs; authentication mechanism. | What is not assessed as an M1 deliverable and must not be prematurely implemented. |
| **Deferred** | Notification channels beyond in-app feedback; FR-006 attachment handling; full Administrator RBAC. | What is preserved as a future option pending evidence, not silently dropped. |

### Defense of a Deliberate Deferment
The team deliberately defers email/SMS notifications beyond the baselined in-app feedback (FR-007). Conflict C-4 identifies a direct tension between the Requester's preference for rich, multi-channel notifications and the Sponsor's (ST-5) need for a sustainable cost/scope footprint. Committing to email/SMS at M1 would introduce third-party integration, delivery, and privacy considerations before the team has evidence of actual demand or budget approval. In-app feedback satisfies the *Must*-priority need for the requester to know a request's outcome (AC-007.1, AC-007.2) without that added risk. This deferment is recorded as a formal decision, with owner and trigger for re-scoping, in the Engineering Decision Log rather than left as an implicit gap.

---

## Functional Requirements

### 1. Requester

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-001** | Submit a new service request with the information needed to action it. | ST-1 | Must | **AC-001.1:** Given all required fields (title, description, category, location, etc.), when submitted, then saved with a unique reference ID with “Received” status, shown to the requester.<br>**AC-001.2:** Given a missing required field, when submitted, then the entry is not saved; each missing field is flagged. |
| **FR-002** | Categorize the request from a controlled category list (not free text). | ST-1, ST-3 | Must | **AC-002.1:** Given the form, when opening the category field, then only controlled categories are selectable (e.g. Fault, Equipment, Security, IT, Maintenance, Lost Property, Other).<br>**AC-002.2:** Given no category selected, when submitted, then submission is blocked (category required); Empty category is flagged. |
| **FR-003** | View the current status of a submitted request. | ST-1 | Must | **AC-003.1:** Given an owned request, when opened, then status shows in plain language (e.g. Received/Assigned/In Progress/Resolved/Closed).<br>**AC-003.2:** Given a requester views a request, when they try to change status, then no such control exists (read-only). |
| **FR-004** | View a history/list of own previously submitted requests. | ST-1 | Must | **AC-004.1:** Given prior requests, when opening history, then only the requester's own requests show with request information (e.g. ref ID, title, category, status, date, description).<br>**AC-004.2:** Given requester’s own profile, when viewing history, then no other requester's request is visible (privacy). |
| **FR-005** | Search/filter own request history. | ST-1 | Should | **AC-005.1:** Given multiple prior requests, when filtering by status, then only the requester's matching prior requests are listed (clear empty-state if none). |
| **FR-006** | Attach supporting information (e.g. a photo). | ST-1, ST-2 | Could | **AC-006.1:** Given an allowed type/size, when attached, then stored and linked; a disallowed type/size is rejected with a message. (File-handling risk may be deferred.) |
| **FR-007** | Receive feedback when a request is accepted, rejected, updated or completed. | ST-1 | Must | **AC-007.1:** Given a status change to Accepted/Rejected/Updated/Completed, when the requester next visits, then an in-app feedback item with timestamp appears.<br>**AC-007.2:** Given a rejection, when feedback is viewed, then a reason is included. |

### 2. Service Staff

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-008** | View the service requests relevant to authorized staff (staff queue). | ST-2 | Must | **AC-008.1:** Given an authenticated staff member, when they open the requests queue, then only requests they are authorized to see are listed (by team/category/assignment).<br>**AC-008.2:** Given a request outside a staff member's authorization, when the queue loads, then that request is not visible (access control). |
| **FR-009** | Search, filter or sort requests using useful criteria. | ST-2 | Must | **AC-009.1:** Given the staff queue, when the staff member filters (status/category/date) or sorts, then only matching requests within their authorization are shown in the chosen order.<br>**AC-009.2:** Given a filter with no matches, when applied, then a clear empty-state message is shown. |
| **FR-010** | View the full details of a request. | ST-2 | Must | **AC-010.1:** Given an authorized request, when the staff member opens it, then full details are shown (requester, category, description, status, action history, attachments if any). |
| **FR-011** | Assign or accept responsibility for a request. | ST-2 | Must | **AC-011.1:** Given an unassigned request, when an authorized staff member accepts/assigns it, then ownership is recorded (owner + timestamp) and the request shows as assigned.<br>**AC-011.2:** Given a request already owned by another staff member, when a second staff member tries to take it, then reassignment follows controlled rules (blocked or authorized reassignment), preventing silent double-ownership. |
| **FR-012** | Update a request's status through controlled transitions. | ST-2 | Must | **AC-012.1:** Given a request in a status, when an authorized staff member advances it, then only valid transitions are allowed (Received $\rightarrow$ Assigned $\rightarrow$ In Progress $\rightarrow$ Resolved $\rightarrow$ Closed) and the change is recorded with actor + timestamp.<br>**AC-012.2:** Given an invalid transition (e.g. Received $\rightarrow$ Closed), when attempted, then it is blocked with a message. (Resolves C-1 with FR-003.) |
| **FR-013** | Record actions, comments or resolution information on a request. | ST-2 | Must | **AC-013.1:** Given an owned/authorized request, when a staff member adds an action, comment or resolution note, then it is saved to the request history with author + timestamp and cannot be silently deleted (accountability). |
| **FR-014** | Resolve or close requests where authorized. | ST-2 | Must | **AC-014.1:** Given an authorized staff member and a resolvable request, when they resolve/close it, then status updates to Resolved/Closed, resolution info is recorded, and the requester receives feedback (links to FR-007).<br>**AC-014.2:** Given a staff member without close permission, when they attempt to close, then the action is blocked (RBAC). |

### 3. Management / Oversight

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-015** | View useful service-activity information (oversight overview). | ST-3 | Must | **AC-015.1:** Given an authenticated manager, when they open the oversight view, then aggregate activity is shown (e.g. counts by status and category) across their authorized scope. |
| **FR-016** | Identify open, overdue, resolved and closed requests. | ST-3 | Must | **AC-016.1:** Given the oversight view, when a manager filters by lifecycle state, then open, resolved and closed requests are each identifiable.<br>**AC-016.2:** Given a request past its target/response time, when the view loads, then it is flagged as overdue. (Depends on a due-date/target field.) |
| **FR-017** | View request information by category / status (or another justified dimension). | ST-3 | Must | **AC-017.1:** Given the oversight view, when a manager groups or filters by category or status, then the request counts and lists update accordingly. |
| **FR-018** | Access enough information to support accountability and service-performance analysis. | ST-3 | Should | **AC-018.1:** Given a manager, when they open reporting, then service-performance information (volumes, resolved counts, overdue counts) is available in enough detail to support accountability.<br>**AC-018.2:** Given a report, when the manager exports it, then the exported data matches what is displayed. (Export may be scoped as Could/deferred) |

### 4. Administrator

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-019** | Manage user accounts. | ST-4 | Must | **AC-019.1:** Given an administrator, when they create or deactivate a user, then the account's access reflects the change (a deactivated user cannot sign in). |
| **FR-020** | Manage roles and permissions (Role-Based Access Control). | ST-4 | Must | **AC-020.1:** Given an administrator, when they assign a role to a user, then that user's permitted actions match the role (e.g. only staff can update status; only managers see oversight).<br>**AC-020.2:** Given a non-administrator, when they attempt to change roles or permissions, then the action is blocked. |
| **FR-021** | Maintain the controlled category list. | ST-4 | Must | **AC-021.1:** Given an administrator, when they add, edit or retire a category, then the requester submission form reflects the current list with no code change (supports FR-002/AC-002.3). |
| **FR-022** | Access an audit trail of key controlled actions. | ST-4 | Should | **AC-022.1:** Given an administrator, when they view the audit trail, then key controlled actions (status changes, assignments, role and category changes) are recorded with actor + timestamp. |

### 5. Sponsor

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-023** | Access high-level service-performance and accountability reporting. | ST-5 | Should | **AC-023.1:** Given a sponsor with oversight access, when they view high-level reporting, then summary service-performance and accountability information is available (may reuse the management reporting view at a summary level). |

---

## Non-Functional Requirements & Open Items

### Non-Functional Requirements

* **NFR-001 - Usability:** First-time requester submits without training in $\le 5$ steps / $\le 5$ min.
  * *Workflow:* Input requester information $\rightarrow$ select request category $\rightarrow$ enter required fields $\rightarrow$ submit request $\rightarrow$ receive submission confirmation.
* **NFR-002 - Feedback Timelines:** Status/feedback visible within $\le 15$ seconds of a staff change.
* **NFR-003 - Privacy:** A requester can never see another requester's request (supports AC-004.2).
* **NFR-004 - Performance:** Staff queue and management oversight views load within $\le 3$ seconds for 1,000 requests.
* **NFR-005 - Concurrency & Integrity:** Two staff members cannot both own the same request; status transitions are applied atomically without lost updates (supports FR-011, FR-012).
* **NFR-006 - Security / RBAC:** Every action is authorized against the user's role; unauthorized access is explicitly denied (supports FR-008, FR-014, FR-020).
* **NFR-007 - Auditability:** Key controlled actions are recorded with actor + timestamp and cannot be silently altered (supports FR-013, FR-022).
* **NFR-008 - Reporting Accuracy:** Management and sponsor reports reconcile completely with underlying request records without discrepancy.
* **NFR-009 - Cost Sustainability (Sponsor):** The solution runs within free/low-cost tiers where practical, and operational cost projections are documented.

### Open Items to Confirm in Later Milestones
1. Authentication model assumed; final selection deferred to M2.
2. Formal confirmation of the initial controlled category list options.
3. Decision on FR-006 (Attachments): file types, size limits, and storage policy.
4. Definition of 'overdue' logic: Target response time fields required. SLA automation is deferred; agree on a simple target mechanism in the Engineering Decision Log (affects FR-016).
5. Reassignment rules: Establish permissions regarding who can reassign an already-owned request (affects FR-011).
6. Reporting export scope: Decide whether reporting is view-only or exportable (affects FR-018).
7. Sponsor access model: Confirm whether the sponsor receives a dedicated view or utilizes management views (affects FR-023).
8. RBAC role definitions: Finalize specific role permissions in M2 (affects FR-020 and security ACs).

---

## CivicConnect: Requirements Traceability Matrix (RTM) v2.0

**Evolved from v1.5.** Five decision columns and a reference column were added at M2; no M1 column was removed. The single M1 column *Design / Architecture* is now expanded into the ASR, architecture, data and design and technology columns below.

**Live workbook:** `Docs/Risks/RTM/CivicConnect-RTM-v2.0.xlsx` is the authoritative matrix and carries three further columns not reproduced here: Issue / PR, Acceptance / Release Evidence and Owner. Full requirement wording stays in sections 4 and 5, and the quality drivers are in `Docs/Requirements/PED_v2.0_ASR_Register.md`.

> **Legend**
> * **Status:** *Baselined* approved and under change control. *In Development* code exists against the decision. *Changed* wording or target altered through a change request. *Candidate, may defer* still under discussion. Rows move to *Implemented* once the acceptance criteria are demonstrably met.
> * **Evidence rule:** implementation cells name files that exist. Verification cells name tests that exist. **No test run has been recorded yet**, so no row claims a passing result. *at M3* means the evidence is legitimately not due yet.
> * **Source IDs:** ST-1 Requester, ST-2 Service Staff, ST-3 Management, ST-4 Administrator, ST-5 Sponsor. **Priority:** MoSCoW.

| ID | Requirement | Src / Pri | AC | ASR | Architecture (M2) | Data / Persistence (M2) | Design and Technology (M2) | ADR / CR / Risk | Implementation | Verification | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **FR-001** | Submit request | ST-1 / Must | AC-001.1; AC-001.2 | ASR-06 | Pages/Requests/Submit.cshtml (SubmitModel) -> POST /api/v1/requests -> RequestService -> RequestRepository | Request row created (unique reference, status Received); Category FK; required fields NOT NULL | ADR-004 view composition; ADR-006 per-field validation errors; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-001; ADR-004; ADR-006; RSK-008; RSK-019 | Core/Services/RequestService.SubmitAsync; Data/Migrations/001_request.sql; Pages/Requests/Submit.cshtml | RequestServiceTests (Overlong_title_is_refused, Staff_cannot_submit_requests_without_the_permission); not yet executed | In Development |
| **FR-002** | Controlled category list | ST-1, ST-3 / Must | AC-002.1; AC-002.2 | ASR-04; ASR-06 | _CategorySelect partial on the submit page -> GET /api/v1/categories -> CategoryService -> CategoryRepository | Category reference table; Request.category_id FK NOT NULL; retired categories kept for history | ADR-004; ADR-006 reference-data endpoint; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-006; CR-008; RSK-018 | Core/Services/CategoryService; Data/Migrations/002_category.sql; category select on Pages/Requests/Submit.cshtml | Covered indirectly by RequestServiceTests; page test planned | In Development |
| **FR-003** | View own status | ST-1 / Must | AC-003.1; AC-003.2 | ASR-06; ASR-01 | Pages/Requests/Details.cshtml (DetailsModel) with the status badge tag helper -> GET /api/v1/requests/{id} | Request.status read only; constrained status vocabulary, not free text | ADR-004 read-only status display (resolves C-1); Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-004; conflict C-1; RSK-016 | Core/Services/RequestService.GetAsync; Pages/Requests/Details.cshtml; TagHelpers/StatusBadgeTagHelper | StatusRulesTests (Display_text_is_plain_language); page test for AC-003.2 planned | In Development |
| **FR-004** | Own request history | ST-1 / Must | AC-004.1; AC-004.2 | ASR-02; ASR-06 | Pages/Requests/Index.cshtml (IndexModel) -> GET /api/v1/requests in own scope -> AuthorisationPolicy | Query scoped by Request.requester_id; index on requester_id | ADR-005 server-side ownership scoping; ADR-004; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005; NFR-003; RSK-011 | RequestService.ListOwnAsync; Pages/Requests/Index.cshtml; index ix_request_requester | RequestServiceTests scope cases; negative access test planned | In Development |
| **FR-005** | Search own history | ST-1 / Should | AC-005.1 | ASR-04 | _FilterBar partial on Pages/Requests/Index.cshtml -> GET /api/v1/requests with status filter | Composite index (requester_id, status); pagination | ADR-004; ADR-006 query parameters; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-006 | Status filter on Pages/Requests/Index.cshtml; ListOwnAsync paging | Planned: filter and empty state page test | In Development |
| **FR-006** | Attachments | ST-1, ST-2 / Could | AC-006.1 | ASR-05 (cost/complexity) | Not allocated while deferred | Deliberately absent from the M2 schema; no Attachment entity or object storage | Deferred - no design decision taken; Deferred, storage not selected | PED Scope (Deferred); CR required to re-scope; RSK-003; RSK-018 | at M3 | at M3 | Candidate - may defer |
| **FR-007** | In application feedback | ST-1 / Must | AC-007.1; AC-007.2 | ASR-06; ASR-05 | StatusTransitionService -> FeedbackService -> GET /api/v1/feedback -> _FeedbackList partial on Pages/Requests/Index.cshtml | FeedbackItem written in the same transaction as the status change; rejection reason mandatory | ADR-002 same-transaction write; in-app pull only, no push; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; conflict C-4; CR-001; RSK-009; RSK-018 | Core/Services/FeedbackService; Data/Migrations/004_feedback.sql; FeedbackController. No requester screen yet | NoteAndFeedbackTests (Nobody_can_mark_someone_elses_feedback_as_read); timing check planned | In Development |
| **FR-008** | Staff queue | ST-2 / Must | AC-008.1; AC-008.2 | ASR-02; ASR-04 | Pages/Staff/Queue.cshtml (QueueModel) with StaffQueueViewComponent -> GET /api/v1/requests/queue -> AuthorisationPolicy -> RequestRepository | Rows filtered by role/team/assignment; composite index (status, category, assigned_to) | ADR-005 policy check per query; ADR-004; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005; NFR-006; RSK-011 | RequestService.QueueAsync; Pages/Staff/Queue.cshtml; index ix_request_queue | RequestServiceTests (The_queue_is_closed_to_requesters); not yet executed | In Development |
| **FR-009** | Filter and sort queue | ST-2 / Must | AC-009.1; AC-009.2 | ASR-04 | _FilterBar partial inside StaffQueueViewComponent -> GET /api/v1/requests query parameters | Indexes on status, category, created_at; sort whitelist; pagination | ADR-006; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-006; NFR-004 | Sort whitelist in RequestService.QueueAsync and RequestRepository; status filter on the queue page | Planned: sort and filter tests | In Development |
| **FR-010** | Full request detail | ST-2 / Must | AC-010.1 | ASR-02; ASR-03 | Pages/Staff/Details.cshtml (DetailsModel) with _RequestDetail and _HistoryList partials -> GET /api/v1/requests/{id} -> AuthorisationPolicy | Request joined to RequestStatusHistory and action notes; attachments absent (FR-006 deferred) | ADR-005; ADR-006; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005; RSK-011 | RequestService.GetAsync with history and notes; Pages/Staff/Details.cshtml; _RequestFields and _HistoryList | RequestServiceTests visibility cases; not yet executed | In Development |
| **FR-011** | Assign or accept | ST-2 / Must | AC-011.1; AC-011.2 | ASR-01 (primary) | Pages/Staff/Details.cshtml OnPostAcceptAsync -> POST /api/v1/requests/{id}/assignment -> AssignmentService -> RequestRepository | assigned_to + assigned_at; single-owner invariant by conditional UPDATE ... WHERE assigned_to IS NULL; history row appended | ADR-002 layered responsibility + conditional update (A2 Task 2 evidence); ADR-006; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; CR-004; NFR-005; RSK-007; RSK-009; RSK-010; RSK-018 | AssignmentService.AcceptAsync; RequestRepository.TryAssignAsync conditional update; Migrations/003_assignment.sql; Pages/Staff/Details.cshtml OnPostAcceptAsync | RealDatabaseTests.Ten_staff_accepting_at_once_leaves_exactly_one_owner (TC-011.2) and AssignmentServiceTests; needs a test database, not yet executed | In Development - M2 trace requirement |
| **FR-012** | Controlled transitions | ST-2 / Must | AC-012.1; AC-012.2 | ASR-01; ASR-03 | Pages/Staff/Details.cshtml OnPostTransitionAsync -> PATCH /api/v1/requests/{id}/status -> StatusTransitionService | Status constrained; RequestStatusHistory append-only (actor, from, to, timestamp); validated inside the transaction | ADR-002 explicit transition rules; ADR-006; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; NFR-005; NFR-007; conflict C-1; RSK-009; RSK-010 | StatusTransitionService.TransitionAsync; Core/Rules/StatusRules; Pages/Staff/Details.cshtml OnPostTransitionAsync | StatusTransitionServiceTests (7 cases) and StatusRulesTests; not yet executed | In Development |
| **FR-013** | Action and resolution notes | ST-2 / Must | AC-013.1 | ASR-03 (primary) | Pages/Staff/Details.cshtml OnPostAddNoteAsync -> POST /api/v1/requests/{id}/notes -> HistoryRepository | Note rows append-only; no update or delete path exposed | ADR-002 append-only history; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; NFR-007; RSK-009; RSK-013 | Core/Services/NoteService; request_note table with an append only trigger. No screen yet | NoteAndFeedbackTests (Only_the_owner_can_add_notes, An_empty_note_is_refused) | In Development |
| **FR-014** | Resolve or close | ST-2 / Must | AC-014.1; AC-014.2 | ASR-01; ASR-02 | Pages/Staff/Details.cshtml OnPostCloseAsync -> PATCH /api/v1/requests/{id}/status -> AuthorisationPolicy, StatusTransitionService and FeedbackService | Resolution information stored; feedback row written in the same transaction | ADR-002; ADR-005 close permission; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; ADR-005; NFR-006; RSK-011 | TransitionAsync with the close permission; offered through NextStatuses on the staff page | StatusTransitionServiceTests close cases; not yet executed | In Development |
| **FR-015** | Oversight overview | ST-3 / Must | AC-015.1 | ASR-04 | Pages/Management/Oversight.cshtml with OversightTotalsViewComponent -> GET /api/v1/reports/overview -> ReportingService | Aggregates computed from the transactional tables; no separate reporting store (NFR-008) | ADR-004; ADR-006; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-001; NFR-008 | at M3 | at M3 | Baselined |
| **FR-016** | Open, overdue, closed | ST-3 / Must | AC-016.1; AC-016.2 | ASR-04 | Pages/Management/Oversight.cshtml lifecycle filters -> ReportingService | Requires a target_response_at field on Request - schema addition pending CR-005 | ADR-004; PostgreSQL 17.x; target response column not added (CR-005) | CR-005; ADR-002; RSK-018 | Blocked pending CR-005 | at M3 | Changed - blocked, CR-005 pending approval |
| **FR-017** | By category or status | ST-3 / Must | AC-017.1 | ASR-04 | OversightTotalsViewComponent grouping -> ReportingService | Group-by on category and status with supporting indexes | ADR-004; ADR-006; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-006 | at M3 | at M3 | Baselined |
| **FR-018** | Performance information | ST-3 / Should | AC-018.1; AC-018.2 | ASR-04 | Pages/Management/Reports.cshtml (ReportsModel) -> GET /api/v1/reports/performance -> ReportingService | Metrics derived from request and history tables; export scope pending CR-006 | ADR-004; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | CR-006; NFR-008; RSK-018 | at M3 | at M3 | Baselined - export scope pending CR-006 |
| **FR-019** | Manage users | ST-4 / Must | AC-019.1 | ASR-02 | Pages/Admin/Users.cshtml (UsersModel) -> /api/v1/admin/users -> UserRoleService | User entity with active flag; deactivation blocks sign-in; no hard delete (audit retention) | ADR-005 identity and authorisation model; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005; CR-003; RSK-012 | app_user table with is_active in Migrations/001_request.sql; Data/UserRepository. No admin screen | at M3 | In Development |
| **FR-020** | Roles and permissions | ST-4 / Must | AC-020.1; AC-020.2 | ASR-02 (primary) | Pages/Admin/Roles.cshtml (RolesModel) -> /api/v1/admin/roles -> AuthorisationPolicy as the single decision point | Role, Permission and UserRole tables; permission checks resolved server-side | ADR-005 design decision 2: RBAC policy enforcement; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005 supersedes D-004; CR-003; NFR-006; RSK-012; RSK-016 | Migrations/005_rbac.sql (role, permission, role_permission, user_role) with the seeded matrix; Core/Rules/AuthorisationPolicy | RequestServiceTests and AssignmentServiceTests permission cases (A_requester_cannot_accept_requests, A_requester_cannot_change_status) | In Development |
| **FR-021** | Maintain category list | ST-4 / Must | AC-021.1 | ASR-04 | Pages/Admin/Categories.cshtml (CategoriesModel) -> /api/v1/admin/categories -> CategoryService | Category rows editable with retire flag; submission form reads the current list, no code change | ADR-006 reference-data endpoint; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | CR-008; RSK-018 | Category table supports retire; no admin screen | at M3 | Baselined |
| **FR-022** | Audit trail | ST-4 / Should | AC-022.1 | ASR-03 | Pages/Admin/Audit.cshtml (AuditModel) -> /api/v1/admin/audit -> AuditService | Reads append-only history/audit tables; tamper-evident, no update path exposed | ADR-002 append-only; ADR-005 read permission; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; NFR-007; RSK-013 | request_status_history and request_note with block_history_changes triggers. Admin view not built | RealDatabaseTests.History_rows_cannot_be_changed_or_deleted | In Development |
| **FR-023** | Sponsor reporting | ST-5 / Should | AC-023.1 | ASR-04 | Reuse Pages/Management/Reports.cshtml at summary scope (access model pending CR-007) | Same aggregates as FR-018 with a narrower scope; no additional entity | ADR-004; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | CR-007; RSK-018 | at M3 | at M3 | Baselined - access model pending CR-007 |
| **NFR-001** | Usability, 5 steps and 5 minutes | ST-1 / Should | ≤ 5 steps and ≤ 5 min (usability test) | ASR-06 | Pages/Requests/Submit.cshtml, one guided form with validation messages for each field | No schema impact | ADR-004 one guided flow with per-field validation; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-004; RSK-016 | One guided form on Pages/Requests/Submit.cshtml with a message against each field | Planned: moderated walkthrough with three first time users | In Development |
| **NFR-002** | Feedback within 15 s | ST-1 / Should | ≤ 15 s after a staff status change (measured) | ASR-06 | FeedbackService write; JavaScript fetch polling refreshes the status partial on Pages/Requests/Details.cshtml | FeedbackItem written in the same transaction as the status change | ADR-002 pull-based refresh; no push infrastructure; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | CR-001; ASR-06; RSK-018 | Feedback written in the same transaction as the status change (FeedbackService). No refresh on the requester screen yet | Planned: timed observation after a transition | Changed - CR-001 pending approval |
| **NFR-003** | Requester privacy | ST-1 / Must | Access-control test; supports AC-004.2 | ASR-02 | AuthorisationPolicy with ownership scoped repository queries in every page model | requester_id scoping on every request read; no unscoped read path | ADR-005; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005 supersedes D-004; RSK-011 | AuthorisationPolicy.CanSee; scoped queries in RequestRepository; requester pages load own rows only | RequestServiceTests visibility cases; direct reference negative test planned | In Development |
| **NFR-004** | 3 s at 1,000 requests | ST-2, ST-3 / Should | ≤ 3 s for 1,000 requests (confirm target) | ASR-04 | Queue and oversight query paths: StaffQueueViewComponent, RequestRepository, ReportingService | Indexing and pagination; aggregates read from the same store | ADR-002; ADR-006; PostgreSQL 17.x indexes; measured on the Render and Neon free plans (ADR-007) | CR-002; NFR-008; RSK-014; RSK-018 | Indexes ix_request_queue, ix_request_requester, ix_request_created in place. db/dev_seed.sql seeds a small set only | Planned: timed load at 1,000 seeded requests | Changed - CR-002 pending approval |
| **NFR-005** | Concurrency and integrity | ST-2 / Must | No two owners; atomic transitions (supports FR-011/012) | ASR-01 | AssignmentService and StatusTransitionService behind the API boundary | Conditional update / optimistic concurrency; single-owner invariant enforced at database level | ADR-002 (A2 Task 2 research evidence); ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; RSK-007; RSK-010 | RequestRepository.TryAssignAsync and TryChangeStatusAsync guarded updates; version column; ck_request_owner_pair and ck_request_owner_vs_status | RealDatabaseTests.Ten_staff_accepting_at_once_leaves_exactly_one_owner and The_database_itself_refuses_an_owner_on_a_Received_request | In Development - M2 trace requirement |
| **NFR-006** | RBAC on every action | ST-4 / Must | Unauthorised access denied (supports FR-008/014/020) | ASR-02 | AuthorisationPolicy invoked by every page handler and every endpoint | Role and permission tables; checks never rely on UI state | ADR-005; Razor Pages; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-005; CR-003; RSK-011; RSK-012 | AuthorisationPolicy.Require called by every service; capabilities on the pages come from the same permissions (UI/CapabilityFactory) | Permission refusal cases across the service tests; role matrix test planned | In Development |
| **NFR-007** | Auditability | ST-4 / Must | Actor+timestamp, not silently altered (supports FR-013/022) | ASR-03 | AuditService and append only history writes on every controlled action | Append-only tables; no UPDATE or DELETE endpoints over history rows | ADR-002; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-002; RSK-009; RSK-013; RSK-015 | Append only history and notes enforced by the block_history_changes triggers, not only by the application | RealDatabaseTests.History_rows_cannot_be_changed_or_deleted | In Development |
| **NFR-008** | Reporting accuracy | ST-3, ST-5 / Should | No discrepancy between report and data | ASR-04 | ReportingService reading the transactional tables | Aggregates computed from source tables; no duplicated store to drift | ADR-001 no separate reporting database; ASP.NET Core on .NET 10 (C# 13); Npgsql with SQL migrations; PostgreSQL 17.x (ADR-003) | ADR-001 | Reporting reads the same tables; no reporting screen yet | at M3 | Baselined |
| **NFR-009** | Cost sustainability | ST-5 / Should | Runs within free/low-cost tiers; cost documented | ASR-05 | Whole solution deployed as a single deployable unit | One managed PostgreSQL instance on a free or low-cost tier; backup limits recorded | ADR-007 deployment direction; Render free tier and Neon free plan (ADR-007) | ADR-007; RSK-006; RSK-014; RSK-015; RSK-019 | Not deployed yet; plan limits recorded in ADR-007 | Planned: cost sheet against the plan limits | Baselined |

**Demonstration trace (section 9).** FR-011 to ASR-01 to AssignmentService behind the API boundary (ADR-001) to the conditional update in RequestRepository.TryAssignAsync (ADR-002, informed by Assignment 2 Task 2) to the endpoint and page handler (ADR-006, ADR-004) to RealDatabaseTests.Ten_staff_accepting_at_once_leaves_exactly_one_owner. Every link exists in the repository. The last one has not been executed yet, because it needs a test database.

**Open change requests.** CR-001 to CR-008 remain open, plus CR-009 proposed for the Rejected status. FR-016 is blocked until CR-005 adds a target response column.

---

## Technical Constraints and Assumptions

* **Achievability:** Technical implementation must fit within team skills and schedule limitations.
* **Layer Separation:** User interface, backend API, and persistence layers must remain distinct.
* **Business Logic Centralization:** Backend layer must handle validation, rules, and security enforcement.
* **Privacy by Design:** System architecture must guarantee requesters cannot view data belonging to other users.

### Initial Architecture Strategy
The initial architecture utilizes a backend API layer situated between the frontend UI and data persistence layer. This provides a central point for validation, business rules, and authorization enforcement while decoupling domain logic from UI components. Backend framework, database choice, API standard, authentication scheme, and deployment details are directional for M1 and will be finalized in M2.

### Architecture, Technology & Initial Design Baseline (M2)

**Included in this baseline (backend and data):**

| Item | Where | Decision record |
| :--- | :--- | :--- |
| Modular monolith: Web, Core, Data projects, one deployable unit | `Docs/Architecture/Backend_Architecture.md` | ADR-001 |
| PostgreSQL 17 schema, constraints, indexes, migrations 001 to 005 | `Docs/Data/Data_Model.md`, `src/CivicConnect.Data/Migrations` | ADR-002, ADR-003 |
| REST contract v1 (`/api/v1`), problem+json errors, polling for status | `Docs/API/API_Contract_v1.md`, `openapi.yaml` | ADR-006 |
| Deployment direction: Render container, Neon PostgreSQL | as recorded in ADR-007 | ADR-007 |

Repository layout, run steps and test instructions are kept in `README_backend.md`. This PED and that README are updated in the same pull request whenever a decision above changes, so the two never drift apart (RSK-017).

**Open decisions and deferred concerns (kept separate on purpose, not silently folded into the baseline above):**

| Item | Needed to close it | Route |
| :--- | :--- | :--- |
| Sign-in mechanism and final RBAC matrix | ADR-005 result | CR-003 |
| Reassignment of an already-owned request | Answer from Service Staff | CR-004 |
| `target_response_at` for "overdue" | Approval, then one `ALTER TABLE` | CR-005 |
| Rejected status in the lifecycle | Approval of CR-009 | CR-009 |
| Staff visibility by team/category | Stakeholder answer | forward consideration; see reconciliation note below |
| Neon backup and restore limits | Read from the provider's own documentation | RSK-015 |
| Manager, admin, sponsor endpoints; attachments; email/SMS; push updates | Later milestones | Forward Engineering Considerations |

### Implementation Notes: Points Requiring Reconciliation

Recorded here so nobody discovers them later. Each one needs either an ADR note or a team decision before the baseline is signed off. None of these are silent scope changes — each is a specific, named gap between a decision record and what the running code currently does.

| Point | What the code does | Why it needs a decision |
| :--- | :--- | :--- |
| Validation status code | Returns `400` with a per-field `errors` object for every validation failure | ADR-006 distinguishes `400` (malformed request) from `422` (understood but fails a business-rule check). The code does not yet make this distinction; either `ApiExceptionHandler` and the contract both change to use `422` for business-rule failures, or ADR-006 is amended to drop the distinction — a decision either way, not a default |
| Data access | Plain SQL through Npgsql, no ORM | ADR-003 names Entity Framework Core as the ORM. This is a confirmed deviation from a recorded decision, not an interpretation gap, and needs either a migration to EF Core or a superseding note on ADR-003 explaining why plain SQL was kept |
| Migration names | `001_request`, `002_category`, `003_assignment` match the RTM; `004_feedback` and `005_rbac` have been added | The RTM's Data / Persistence column only names the first three migrations; it needs updating so the schema and the RTM stay in step |
| Staff visibility | Staff currently see unassigned requests and their own | AC-008.1 (Must priority) requires scoping "by team/category/assignment." The narrower rule is not yet a fulfillment of that acceptance criterion — it is an interim implementation pending the forward consideration above |
| Who can add notes / change status | The owner only, except rejecting an unowned request | Not stated in M1; needs confirmation with Service Staff before it is treated as settled |

### Engineering Decision Log

| ID | Decision | Reason | Status |
| :--- | :--- | :--- | :--- |
| **D-001** | Separate UI, backend/API, and data persistence layers. | Supports maintainability, clean separation of concerns, and modular testing. | Carried forward into ADR-001 |
| **D-002** | Use a central backend/API layer for application operations. | Provides a consistent location for validation, business rules, and access control. | Carried forward into ADR-001 |
| **D-003** | Defer final backend framework and database selection to M2. | Tech selection requires structured evaluation and proof-of-concept testing. | Closed – superseded by ADR-002 (database) and ADR-003 (stack) |
| **D-004** | Treat authentication and authorization as M2 decisions. | Specific security architecture depends on stack selection and RBAC refinement. | Closed – superseded by ADR-005; sign-in mechanism itself still open under CR-003 |

### Technical Constraints

| Constraint | Consideration |
| :--- | :--- |
| **Schedule** | Target frameworks must allow rapid development within strict milestone timelines. |
| **Stack Compatibility** | Selected libraries and tools must integrate seamlessly across the application stack. |
| **Learning Curve** | Priority is given to frameworks familiar to the team to minimize execution risk. |

---

## Risk Register

**Live register:** `Docs/Risks/Risk_Register_V2.md` is the authoritative, continuously maintained register (20 risks: RSK-001 to RSK-020, one table across the life of the project, no milestone-labelled sections). This section is a summary view of it, kept here so the PED stays readable on its own.

| Priority | Count |
| :--- | :---: |
| Critical | 2 (RSK-001, RSK-007) |
| High | 10 |
| Medium | 8 |
| Low | 0 |

**Top risks currently being managed:** RSK-007 (no application code exists yet against an undecided-until-recently stack), RSK-001 (team unfamiliarity with the chosen stack, still unmitigated by a proof-of-concept spike), RSK-008 (technology stack decision blocking dependent work), RSK-011 (authorization – a Must-priority privacy requirement), and RSK-010 (concurrent-ownership safety, the ASR-01 driver).

Owner note: G. Enright owns the register. New risk evidence produced during construction (RSK-007, RSK-009, RSK-010 and RSK-013 now have concrete test and trigger evidence rather than a planned mitigation; RSK-015's likelihood is under review following backup-limit findings) is reconciled directly in `Risk_Register_V2.md`, not duplicated here.

---

## Forward Engineering Considerations

| Consideration | Why not now | Evidence needed later |
| :--- | :--- | :--- |
| Real authentication | Waiting on ADR-005 / CR-003 | Chosen mechanism; replace `CurrentUserAccessor` only |
| Reporting endpoints for Management and Sponsor | Not in Slice 1 | CR-005, CR-006, CR-007 answers |
| Admin audit table (`audit_log`) | Request history already covers Slice 1 actions | FR-022 design in M3 |
| Push updates (SSE) | Polling meets NFR-002 | Measured polling load, or a requirement for instant updates |
| Attachments | FR-006 deferred | File types, size limits, storage policy |
| Email/SMS | Deferred by C-4 | Sponsor budget approval |
| Read replica / failover | Costs money; ASR-05 | Sponsor asks for higher availability |
| Concurrency via row locking | Assignment 2 assumes few simultaneous clashes | Test results showing frequent conflicts |
| Optimistic `version` checks on other edits | Column exists, only used by conditional updates today | First edit feature that needs it |

---

## Baseline Sign-Off

| Field | Details |
| :--- | :--- |
| **Project** | CivicConnect |
| **Baseline Type** | Milestone 1 (M1) Baseline |
| **Version** | v1.5 |
| **Date** | 09/09/2026 |
| **Scope Reviewed** | Yes |
| **Requirements / Traceability Checked** | Yes |
| **Risk Review Completed** | Yes |
| **Repository / Governance Controls Checked** | Yes |
| **Outcome** | **Accepted** |

| Field | Details |
| :--- | :--- |
| **Project** | CivicConnect |
| **Baseline Type** | Architecture, Technology & Initial Design Baseline (M2) |
| **Version** | v2.0 |
| **Date** | To be entered at sign-off |
| **Scope Reviewed** | Pending team review |
| **Requirements / Traceability Checked** | RTM v2.0 updated with M2 evidence; CR-001 to CR-009 open |
| **Architecture / Technology Decisions Reviewed** | ADR-001 to ADR-007 all recorded, all "Proposed, awaiting team approval" |
| **Risk Review Completed** | Risk Register v2.0 reviewed 29/09/2026; RSK-015 rating under review |
| **Repository / Governance Controls Checked** | Pending: GitHub secret scanning / push protection confirmation (RSK-005, RSK-020) |
| **Meaningful Development Evidence** | 21 requirement rows moved to In Development against real code, migrations and tests named in the RTM. No test run recorded yet |
| **Outcome** | **Pending – not yet signed off** |

---

## References

* Gong, Y. & Yan, J. & Soliman, X., 2020. *Towards a comprehensive understanding of digital transformation in government: Analysis of flexibility and enterprise architecture*. Government Information Quarterly, 37(3), p. 101487.
* Mergel, I., Edelmann, N. & Haug, N., 2019. *Defining digital transformation: Results from expert interviews*. Government Information Quarterly, 36(4), p. 1101385.