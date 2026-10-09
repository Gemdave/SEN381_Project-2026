# CivicConnect Requirements & Engineering Baseline (PED) (v3.0)

**Authors:** Gerald Enright (577830) | Keletso Marota (601632) | Mogau Malope (600192)  
**Course Code:** SEN381  
**Date:** 05/10/2026  

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
6. [Architecturally Significant Requirements (ASRs)](#architecturally-significant-requirements-asrs)
7. [CI & Automated Verification](#ci--automated-verification)
8. [CivicConnect: Requirements Traceability Matrix (RTM)](#civicconnect-requirements-traceability-matrix-rtm)
9. [Controlled Changes to the Baseline](#controlled-changes-to-the-baseline)
10. [Technical Constraints and Assumptions](#technical-constraints-and-assumptions)
    - [Initial Architecture Strategy](#initial-architecture-strategy)
    - [Engineering Decision Log](#engineering-decision-log)
    - [Technical Constraints](#technical-constraints)
11. [Engineering Decisions and Comparisons](#engineering-decisions-and-comparisons)
12. [Risk Register](#risk-register)
13. [Baseline Sign-Off](#baseline-sign-off)
14. [References](#references)

---

## Problem Statement

CivicConnect currently manages community service requests through an uncoordinated, ad-hoc mix of email, phone calls, WhatsApp messages, and unlinked spreadsheets. No central channel owns the full lifecycle of a request. As such, the organization lacks a reliable, verifiable method to answer three fundamental operational questions:

1. **What has been requested?**
2. **Who is accountable for acting on the issue?**
3. **Has the issue actually been resolved?**

Recent empirical research in public sector digital transformation highlights that replacing fragmented, message-based administrative processes with centralized, controlled request management platforms is critical for establishing operational transparency, resolving service bottlenecks, and rebuilding stakeholder trust (Gong, 2020; Mergel, 2019).

Thus, the underlying business need is not merely "a website to log requests," but a single, controlled record of the request lifecycle. This platform must grant requesters real-time visibility, provide operational staff with an accountable queue, and supply management with auditable, reportable performance data, all achieved without introducing unsustainable cost, complexity, or technical risk.

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
| **C-5** | Future growth vs. keeping cost and scope small today (added by CR-011) | Sponsor / project constraints | No FRs added; growth is met through the architecture (NFR-010, ASR-07), while features and hosting stay at the baseline (NFR-009). |

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
| **Growth readiness (CR-010, CR-011)** | An architecture able to grow later (NFR-010, ASR-07). The move to microservices changes how the system is built, not what it does; functional scope is unchanged. | Growth readiness must be shown without adding features. Only the services current requirements need are built, and a new feature still needs its own CR. |

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
| **FR-021** | Maintain the controlled category list. | ST-4 | Must | **AC-021.1:** Given an administrator, when they add, edit or retire a category, then the requester submission form reflects the current list with no code change (supports FR-002/AC-002.1; previously cited as AC-002.3, see EC-001). |
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
* **NFR-010 - Growth Readiness (CR-011):** CivicConnect can later support more users or features without a redesign. No volume target is set, since no growth figures exist. Checks: a second copy of a service changes no behaviour; no service touches another service's schema in the shared PostgreSQL instance (ADR-001); a new service needs no schema change elsewhere.

### Open Items to Confirm in Later Milestones
1. Authentication model assumed; final selection deferred to M2. **Status:** Open under CR-003. Authorisation is covered by ADR-005, but the sign-in mechanism has not been chosen.
2. Formal confirmation of the initial controlled category list options. **Status:** Open under CR-008.
3. Decision on FR-006 (Attachments): file types, size limits, and storage policy. **Status:** Remains deferred at *Could* priority. CR-010 does not bring it back into scope.
4. Definition of 'overdue' logic: Target response time fields required. SLA automation is deferred; agree on a simple target mechanism in the Engineering Decision Log (affects FR-016). **Status:** Open under CR-005.
5. Reassignment rules: Establish permissions regarding who can reassign an already-owned request (affects FR-011). **Status:** Open under CR-004.
6. Reporting export scope: Decide whether reporting is view-only or exportable (affects FR-018). **Status:** Open under CR-006.
7. Sponsor access model: Confirm whether the sponsor receives a dedicated view or utilizes management views (affects FR-023). **Status:** Open under CR-007.
8. RBAC role definitions: Finalize specific role permissions in M2 (affects FR-020 and security ACs). **Status:** Open under CR-003, with ADR-005.
9. *(Added after the M2 review.)* Growth figures: none are available, so NFR-010 has no volume target. If the Sponsor supplies figures, a change request will add one.

---

## Architecturally Significant Requirements (ASRs)

Full register, sources and open targets: `Docs/Requirements/ASR_Register.md` (owner M. Malope). After the M2 review moved to microservices (CR-010), CR-011 restated ASR-05 and added ASR-07; the other expectations are unchanged.

| ASR | Driver and measurable expectation | Consequence for the service split | ADR |
| :--- | :--- | :--- | :--- |
| **ASR-01** | Ownership and status correctness: of simultaneous accepts on one request, exactly one succeeds; no status change is lost; invalid transitions are refused. | Assignment, status and the single-owner check stay inside one service with one data store. Other services learn of a change only after it commits. | ADR-001 (rev.), ADR-002 |
| **ASR-02** | Authorisation and privacy: every read and write is authorised on the server; another requester's reference is refused. | Each service checks identity and role for its own data; role rules keep one source of truth. | ADR-005, ADR-001 (rev.) |
| **ASR-03** | Auditability: every controlled action is recorded with actor and timestamp; history cannot be edited. | An action and its audit record succeed or fail together; audit is append-only and a lost message must not lose an entry. | ADR-002 (rev.) |
| **ASR-04** | Oversight views within 3 s at 1,000 requests; reports match the records exactly. | Reporting reads request data through the Requests service and keeps no copy, so NFR-008 stands as written. | ADR-001 (rev.), ADR-002 |
| **ASR-05** *(restated)* | Sustainable cost and complexity: each service on free or low-cost plans with its cost recorded; no more services than needed; one person can run the system locally. | Build only the services needed now, on one stack with shared build and run steps. | ADR-001 (rev.), ADR-003, ADR-007 |
| **ASR-06** | Requester self-service: submit in ≤ 5 steps and ≤ 5 min; a staff status change is visible within 15 s. | Short guided flow with read-only status; the 15 s check covers the whole path across services. | ADR-004, ADR-003 |
| **ASR-07** *(new)* | Growth readiness: the three NFR-010 checks pass; no volume target until growth figures exist. | Services hold no in-memory state, own their schema in the shared instance, and are split only along boundaries the current requirements show. | ADR-001 (rev.), ADR-006, ADR-007 |

Superseded by CR-011: ASR-05 previously favoured "one deployable unit over a distributed design", and ASR-04 held that reporting "does not justify a separate reporting database"; the full earlier wording is kept in the register. Failure isolation, independent deployability and a fixed volume target were considered but not adopted, because no stakeholder evidence or growth figures support them yet.

---
## CI & Automated Verification
**Claim:** every change to `main` is built, tested and checked automatically, and
cannot be merged unless the Quality Gate passes.

| Item | Summary | Evidence |
| :--- | :--- | :--- |
| **Workflow** | GitHub Actions: Build and Test, Static Analysis, Dependency Vulnerabilities, Secrets Scan, then an aggregating Quality Gate | `/.github/workflows/ci.yml` |
| **Gate rule** | A build/test failure, formatting violation, High/Critical dependency advisory or detected secret each blocks merge | `/Docs/CI_Documentation/CI_Quality_Gates.md` |
| **Branch protection** | `main` protected; Quality Gate required; 2 non-author approvals | [PR link showing required checks] |
| **Passing run** | Run on release candidate commit `<sha>` | [Actions run URL] |
| **Gate blocking a change** | A run where the gate failed and was fixed | [failed run URL + fixing PR] |
| **Test and coverage output** | TRX and coverage artifacts per run | [artifact link in the run above] |

**What a green run shows / does not show:** see `/Docs/CI_Documentation/CI_Quality_Gates.md`.

---
## CivicConnect: Requirements Traceability Matrix (RTM)
> **Legend:**
> * **Traceability Target (M4 Chain):** Stakeholder/Source $\rightarrow$ Requirement $\rightarrow$ Acceptance Criteria $\rightarrow$ Design/Architecture $\rightarrow$ Issue/PR $\rightarrow$ Implementation $\rightarrow$ Test $\rightarrow$ Acceptance/Release Evidence. *(Remaining lifecycle columns completed as evidence is produced)*
> * **Source IDs:** ST-1 (Requester), ST-2 (Service Staff), ST-3 (Management), ST-4 (Administrator), ST-5 (Sponsor).
> * **Priority (MoSCoW):** Must, Should, Could, Won't.
> * **Status:** Baseline candidate, Baselined, Candidate / may defer.

| ID | Source | Requirement | Acceptance Criteria | Priority | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **FR-001** | ST-1 | Submit a new service request with the information needed to action it. | AC-001.1; AC-001.2 | Must | Baseline candidate |
| **FR-002** | ST-1, ST-3 | Categorize the request from a controlled category list (not free text). | AC-002.1; AC-002.2 | Must | Baseline candidate |
| **FR-003** | ST-1 | View the current status of a submitted request. | AC-003.1; AC-003.2 | Must | Baseline candidate |
| **FR-004** | ST-1 | View a history/list of own previously submitted requests. | AC-004.1; AC-004.2 | Must | Baseline candidate |
| **FR-005** | ST-1 | Search/filter own request history. | AC-005.1 | Should | Baseline candidate |
| **FR-006** | ST-1, ST-2 | Attach supporting information to a request (e.g. a photo). | AC-006.1 | Could | Candidate may defer |
| **FR-007** | ST-1 | Receive feedback when a request is accepted, rejected, updated or completed. | AC-007.1; AC-007.2 | Must | Baseline candidate |
| **FR-008** | ST-2 | View the service requests relevant to authorized staff (staff queue). | AC-008.1; AC-008.2 | Must | Baseline candidate |
| **FR-009** | ST-2 | Search, filter or sort requests using useful criteria. | AC-009.1; AC-009.2 | Must | Baseline candidate |
| **FR-010** | ST-2 | View the full details of a request. | AC-010.1 | Must | Baseline candidate |
| **FR-011** | ST-2 | Assign or accept responsibility for a request. | AC-011.1; AC-011.2 | Must | Baseline candidate |
| **FR-012** | ST-2 | Update a request's status through controlled transitions. | AC-012.1; AC-012.2 | Must | Baseline candidate |
| **FR-013** | ST-2 | Record actions, comments or resolution information on a request. | AC-013.1 | Must | Baseline candidate |
| **FR-014** | ST-2 | Resolve or close requests where authorized. | AC-014.1; AC-014.2 | Must | Baseline candidate |
| **FR-015** | ST-3 | View useful service-activity information (oversight overview). | AC-015.1 | Must | Baseline candidate |
| **FR-016** | ST-3 | Identify open, overdue, resolved and closed requests. | AC-016.1; AC-016.2 | Must | Baseline candidate |
| **FR-017** | ST-3 | View request information by category/status (or another justified dimension). | AC-017.1 | Must | Baseline candidate |
| **FR-018** | ST-3 | Access enough information to support accountability and service-performance analysis. | AC-018.1; AC-018.2 | Should | Baseline candidate |
| **FR-019** | ST-4 | Manage user accounts. | AC-019.1 | Must | Baseline candidate |
| **FR-020** | ST-4 | Manage roles and permissions (RBAC). | AC-020.1; AC-020.2 | Must | Baseline candidate |
| **FR-021** | ST-4 | Maintain the controlled category list. | AC-021.1 | Must | Baseline candidate |
| **FR-022** | ST-4 | Access an audit trail of key controlled actions. | AC-022.1 | Should | Baseline candidate |
| **FR-023** | ST-5 | Access high-level service-performance and accountability reporting. | AC-023.1 | Should | Baseline candidate |
| **NFR-001** | ST-1 | Usability: first-time requester submits without training in $\le 5$ steps and $\le 5$ min. | Submission completes in $\le 5$ steps and $\le 5$ min, verified in usability testing. | Should | Baseline candidate |
| **NFR-002** | ST-1 | Feedback timeliness: status/feedback visible within $\le 15$ sec of a staff change. | Feedback visible to requester $\le 15$ s after a staff status change. | Should | Baseline candidate |
| **NFR-003** | ST-1 | Privacy: a requester can never view another requester's request. | Requester cannot access another's data (supports AC-004.2). | Must | Baseline candidate |
| **NFR-004** | ST-2, ST-3 | Performance: staff queue and oversight views load quickly under load. | $\le 3$ s load time for 1,000 requests. | Should | Candidate |
| **NFR-005** | ST-2 | Concurrency & integrity: no double-ownership or lost updates. | No duplicate ownership; atomic transitions (supports FR-011/012). | Must | Candidate |
| **NFR-006** | ST-4 | Security / RBAC: every action authorized by role. | Unauthorized access denied (supports FR-008/014/020). | Must | Candidate |
| **NFR-007** | ST-4 | Auditability: key actions recorded and tamper-evident. | Actor + timestamp, not silently altered (supports FR-013/022). | Must | Candidate |
| **NFR-008** | ST-3, ST-5 | Reporting accuracy: reports reconcile with underlying records. | Zero discrepancy between reporting and data layer. | Should | Candidate |
| **NFR-009** | ST-5 | Cost sustainability: run within free/low-cost tiers where practical. | Operates in free/low-cost tiers; cost documented. | Should | Candidate |
| **NFR-010** | ST-5 | Growth readiness: able to expand in future without a redesign (added through CR-011). | Scale-out, data ownership and extension checks all pass (NFR-010). | Must | Candidate (CR-011) |

**Demonstration trace.** FR-011 (AC-011.2) -> ASR-01 -> the Requests service, the only service allowed to assign (ADR-001) -> a conditional update in the requests schema that succeeds only while the request is unassigned, with the history row in the same transaction (ADR-002) -> `POST /api/v1/requests/{id}/assignment` through the API entry point (ADR-006) -> `app/` code: `AssignmentService`, `RequestRepository` and `AssignmentEndpoints` -> `AssignmentServiceTests` (run in CI) and `ConcurrentAcceptTests` (skipped in CI until a test database is provided).

---

## Controlled Changes to the Baseline

Each change is recorded here before the affected section changes, and the original wording is kept. Working copy: `Docs/Risks/RTM/CivicConnect-Change-Register.xlsx`.

| ID | Affects | Change | Status | Owner |
| :--- | :--- | :--- | :--- | :--- |
| **CR-001 to CR-008** | NFR-002, NFR-004 and Open Items 1, 2, 4 to 8 | Clarifications carried from the first M2 pass; each is shown against its open item above, with full wording in the workbook. | Open | M. Malope (CR-003: G. Enright) |
| **CR-009** | FR-007, FR-012 | Add Rejected as a terminal status, reachable only from Received and always with a reason (`Data_Model.md`, section 8). | Proposed | K. Marota proposes, M. Malope records |
| **CR-010** | ADR-001 (superseded, D-005); RTM architecture and data columns; ADR-002, ADR-004 to ADR-007 | Move from the modular monolith to four services (Requests, Access, ReferenceData, Reporting) behind one API entry point, with a schema per service in one PostgreSQL instance, so the system can grow without a redesign (M2 review). Functional scope unchanged. | Approved through the revised ADR-001 (D-005) | K. Marota, M. Malope |
| **CR-011** | NFR-010, ASR-05, ASR-07, C-5 | Add NFR-010 Growth Readiness (three checks, no volume target), restate ASR-05, add ASR-07 and C-5. | Open | M. Malope |
| **EC-001** | FR-021 (AC-021.1) | Editorial: cited AC-002.3, which does not exist; now AC-002.1. No change in meaning. | Applied | M. Malope |

All open CRs need approval before baseline sign-off. One more may follow once ADR-005 is revised: AC-019.1, if a deactivated user's session can stay valid for a time. NFR-008 needs no change, because reporting keeps no copy of the data (ADR-001).

---

## Technical Constraints and Assumptions

* **Achievability:** Technical implementation must fit within team skills and schedule limitations.
* **Layer Separation:** User interface, backend API, and persistence layers must remain distinct.
* **Business Logic Centralization:** Backend layer must handle validation, rules, and security enforcement.
* **Privacy by Design:** System architecture must guarantee requesters cannot view data belonging to other users.

### Initial Architecture Strategy
The initial architecture utilizes a backend API layer situated between the frontend UI and data persistence layer. This provides a central point for validation, business rules, and authorization enforcement while decoupling domain logic from UI components. Backend framework, database choice, API standard, authentication scheme, and deployment details are directional for M1 and will be finalized in M2.

### Engineering Decision Log

| ID | Decision | Reason | Status |
| :--- | :--- | :--- | :--- |
| **D-001** | Separate UI, backend/API, and data persistence layers. | Supports maintainability, clean separation of concerns, and modular testing. | Initial |
| **D-002** | Use a central backend/API layer for application operations. | Provides a consistent location for validation, business rules, and access control. | Initial |
| **D-003** | Defer final backend framework and database selection to M2. | Tech selection requires structured evaluation and proof-of-concept testing. | Open |
| **D-004** | Treat authentication and authorization as M2 decisions. | Specific security architecture depends on stack selection and RBAC refinement. | Open |

### Technical Constraints

| Constraint | Consideration |
| :--- | :--- |
| **Schedule** | Target frameworks must allow rapid development within strict milestone timelines. |
| **Stack Compatibility** | Selected libraries and tools must integrate seamlessly across the application stack. |
| **Learning Curve** | Priority is given to frameworks familiar to the team to minimize execution risk. |

---

## Engineering Decisions and Comparisons

Every decision is compared here against CivicConnect's own drivers, including those researched in Assignment 2 or 3; full records stay in `Docs/Decisions/`. Options score 1 (poor), 2 (acceptable) or 3 (good) per criterion, weighted 1 to 3 by the ASR, requirement or risk the criterion comes from. Still to be added by their owners: ADR-001, ADR-002 and ADR-006 (K. Marota); ADR-003, ADR-005 and ADR-007 (G. Enright).

### ADR-004: Role-Based View Composition (Design Decision 1)

**Status:** Proposed, revision 2 (08/10/2026), to align with the revised ADR-001, ADR-005 and ADR-006. **Owner:** M. Malope. **Research:** A2 Task 1 (Naghdipour, 2023; Silva, 2021). **Full record:** `Docs/Decisions/ADR-004-role-based-view-composition.md`. **Problem:** Four roles use the same request data with different fields and actions, and a requester must never get a status control or see another requester's data (AC-003.2, NFR-003). With services, one screen draws on several services, so there are two questions: where screens get their data (Part 1, new) and how each role's pages are built (Part 2, from revision 1).

**Part 1: where the screens get their data**

| Criterion (weight) | A. Browser calls services | B. Gateway merges | C. One frontend service | D. Frontend per role | E. Micro-frontends |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ASR-02** Privacy (×3) | **1** Every service exposed to the browser | **2** Role shaping in shared infrastructure | **3** Services hidden; controls set server-side | **3** Smallest exposure per role | **2** Many exposed pieces |
| **ASR-06** Consistent screens (×3) | **2** Status logic repeated in browser code | **2** Consistent only if the gateway shapes data alike | **3** One shared status partial | **2** Shared partials copied or packaged per service | **1** Pieces can look and behave differently |
| **ASR-05** Cost for a team of three (×3) | **1** Screens rewritten client-side | **2** Custom merge code in the gateway | **3** One extra deployable reusing existing pages | **1** Three or four deployables, each with its own pipeline, configuration, secrets and hosting slot (NFR-009) | **1** Most build and deployment work |
| **ASR-07** Growth (×2) | **2** Screens change when services split | **2** Every screen change passes the gateway | **2** Runs as copies; splits per role later | **3** Each role scales and releases alone | **3** Each piece scales and releases alone |
| **ASR-04** Oversight speed (×1) | **1** Many browser round trips | **3** One call per screen | **2** Several server-side calls | **3** Tailored per role | **2** Pieces load separately |
| **Total (max 36)** | **17** | **25** | **33** | **27** | **20** |

**Part 2: how each role's pages are built.** Build effort is ×2 here, not ×3, because these options differ only in coding effort inside one service, not in services to build, host and pay for.

| Criterion (weight) | P1. Page per role | P2. Role conditions in markup | P3. Partials, page models, capability object | P4. Per-role strategy |
| :--- | :--- | :--- | :--- | :--- |
| **ASR-06** Status the same for all roles (×3) | **1** Four copies drift | **2** Branches multiply | **3** One partial per status piece | **2** Rendering split across strategies |
| **ASR-02** No role logic in markup (×3) | **2** Loading rules repeated | **1** Role checks spread through views | **3** Server-built capability object; handlers re-check | **2** Logic in strategies |
| **ASR-05** Build effort (×2) | **2** Every change made four times | **3** Fastest | **2** Capability object to build and pass | **1** Extra layer against Razor conventions |
| **Changeability**, from FR-023 and RSK-016 (×2) | **1** Copy everything | **1** New branches affect all roles | **3** New page folder on existing partials | **2** New strategy plus wiring |
| **Testability**, from RTM verification evidence (×1) | **2** Each page tested four times | **1** Every branch per role | **2** Page tests with a seeded user | **3** Strategies tested alone |
| **Total (max 33)** | **17** | **18** | **30** | **21** |

**Decision.** Option C with P3 inside it. One web frontend service serves every role from role-separated page folders over shared partials, and the browser talks only to it. The capability object only decides which controls appear; its values come from the owning service, which re-checks every action. The frontend keeps no in-memory session state, so it can run as several copies. C beats D because D adds three or four deployables now, while C's page folders are where D can be split out later. **Costs and triggers:** one more deployable and network hop per screen, and a frontend defect can affect every role. Split per role (D) only if measurement shows one role holding the others back; simplify P3 if the sponsor view (FR-023) cannot reuse the partials.

---

## Risk Register

| ID | Category | Risk (Cause / Event / Consequence) | Rating (P / I) | Mitigation | Contingency | Owner, Status & Mitigation Evidence |
|---|---|---|---|---|---|---|
| RSK-001 | Technology / Dependency | **Cause:** The team had to select and learn a stack under a fixed schedule.<br>**Event:** The selected stack proves harder to implement or support than expected.<br>**Consequence:** Construction slows and defects increase. | M / M<br>Medium (4) | Keep the selected .NET 10/C# 13/PostgreSQL stack stable, avoid unnecessary framework changes, and use the existing implementation and CI workflow to build team familiarity. | Freeze non-essential features and concentrate the team on the existing stack rather than introducing another technology. | **K. Marota**<br>Mitigating<br>Evidence: ADR-003, application code, test projects, CI workflow |
| RSK-002 | Process / Configuration Management | **Cause:** Three members modify shared controlled artifacts concurrently.<br>**Event:** Documentation, code or baseline evidence diverges between branches or is merged inconsistently.<br>**Consequence:** The project loses a reliable single source of truth and time is spent reconciling changes. | M / M<br>Medium (4) | Continue protected-main development, two-reviewer substantive PRs, small changes and the PR traceability checklist. Keep the current PED as the active baseline and archives clearly separated from it. | Restore the last agreed version and reconcile the affected artifacts through a controlled PR and change record. | **G. Enright**<br>Mitigating<br>Evidence: Git history, PR template, Team Working Agreement |
| RSK-003 | Scope / Change | **Cause:** Nine change requests remain open or proposed while implementation is progressing.<br>**Event:** A late requirement decision changes an already-built schema, service or interface.<br>**Consequence:** Rework, migration changes and late RTM/status changes consume construction time. | M / H<br>High (6) | Triage CR-001 to CR-009. Resolve CR-003 and CR-004 first because they directly affect security and the M2 build slice. Record every accepted change against the affected requirement, ADR and implementation. | Freeze affected functionality at the conservative interpretation and defer dependent work until the change is approved. | **M. Malope**<br>Open<br>Evidence: CR register shows all nine unresolved |
| RSK-004 | Process / Quality | **Cause:** AI is being used for research, documentation and implementation assistance.<br>**Event:** An AI-generated technical claim, design, code fragment or source is accepted without adequate human verification.<br>**Consequence:** Incorrect or unsupported engineering evidence enters the project. | M / H<br>High (6) | Continue maintaining the AI Usage Register. Require human review of AI-assisted code and documentation and verify security, technology and dependency claims before baselining them. | Remove or correct the affected material, record the rejection/change in the AI Register and re-review the associated ADR or implementation. | **G. Enright**<br>Mitigating<br>Evidence: AI Register contains current team entries and verification notes |
| RSK-005 | Security — Secrets / Configuration | **Cause:** Database credentials and future signing keys must be supplied to local/deployed environments.<br>**Event:** A secret is committed to Git or exposed through configuration/CI.<br>**Consequence:** Credentials must be considered compromised and rotated, potentially affecting the deployment. | M / H<br>High (6) | Keep secrets outside source control. Use .NET user secrets locally and environment variables for deployment. Maintain .gitignore coverage for environment files, keys and certificates. Enable GitHub secret scanning and push protection before real deployment credentials are created. | Immediately revoke and rotate the exposed credential, remove it from repository history where necessary, investigate access and record the incident as a realized risk. | **G. Enright**<br>Mitigating<br>Evidence: .gitignore, app configuration, Program.cs, ADR-007 |
| RSK-006 | Technology / Deployment | **Cause:** Development and deployment environments are not yet proven equivalent; the deployment direction assumes a containerized Render deployment while no Dockerfile is currently present in the repository.<br>**Event:** The application works locally but cannot be built or run in the intended deployment environment.<br>**Consequence:** Deployment and demonstration delays occur and environment-specific defects appear late. | M / H<br>High (6) | Keep runtime and package versions pinned/documented. Finalize the Render/Neon deployment path and add the Dockerfile/build configuration required by ADR-007. Verify the same configuration on a clean environment. | Use documented local execution for the demonstration and escalate to a paid/alternative managed deployment only if the selected path cannot be made reliable in time. | **G. Enright**<br>Mitigating<br>Evidence: ADR-007, global.json, project files and configuration documentation |
| RSK-007 | Implementation | **Cause:** The project initially had little construction evidence and a large gap between decisions and code.<br>**Event:** The first build slice remains incomplete or implementation is delivered in a compressed final push.<br>**Consequence:** The project fails to demonstrate progressive construction and weakens requirement-to-code traceability. | M / H<br>High (6) | Continue the existing Build Slice 1: request flow, status/history, staff queue/detail, assignment, status transition, notes and feedback. Keep RTM implementation/test evidence updated alongside code and retain progressive commits/PRs. | Reduce construction to the single M2 demonstration trace and the minimum supporting path, explicitly marking deferred requirements as Planned. | **K. Marota**<br>Mitigating<br>Evidence: real domain/application/web code, migrations, tests and 14 RTM rows marked In Development |
| RSK-008 | Technology / Dependency | **Cause:** ADR-003 was historically undecided and dependent documents still require formal approval.<br>**Event:** The recorded technology decision remains incomplete or is changed after dependent implementation has begun.<br>**Consequence:** Existing code, schema, CI and deployment assumptions require rework. | M / M<br>Medium (4) | Treat PostgreSQL, .NET 10/C# 13, ASP.NET Core Razor Pages and the Web API direction as the current selected stack. Complete the ADR status/approval and reconcile all RTM and documentation references. | If a technology change becomes unavoidable, freeze affected implementation and record the replacement decision and migration impact before continuing. | **K. Marota**<br>Mitigating<br>Evidence: ADR-003 records the actual selected stack and versions |
| RSK-009 | Data / Persistence | **Cause:** Controlled actions update business state and history/audit records together.<br>**Event:** A failure causes the state change to commit without the corresponding history/feedback record.<br>**Consequence:** Auditability and accountability records become inconsistent. | L / H<br>Medium (3) | Use a database transaction around the business operation. The current implementation performs assignment/status changes and history writes through a shared unit of work. Add/retain integration tests covering rollback and same-transaction history. | Reconcile business records against history, repair inconsistencies through a controlled administrative process and record the repair in audit history. | **K. Marota**<br>Mitigating<br>Evidence: UnitOfWork, AssignmentService, StatusTransitionService, ADR-002 and integration-test source |
| RSK-010 | Data / Concurrency | **Cause:** Multiple staff members may attempt to accept the same unassigned request concurrently.<br>**Event:** Two writes are allowed to claim the same request or the conflict is not correctly surfaced.<br>**Consequence:** Ownership integrity is violated and accountability becomes ambiguous. | M / H<br>High (6) | Use the conditional database update already implemented: assignment only succeeds when the request is still unassigned. Check affected-row count and return a conflict. Retain the real-PostgreSQL concurrent-accept integration test. | If concurrency testing exposes a defect, disable reassignment/acceptance temporarily and revise the persistence condition before continuing. | **K. Marota**<br>Mitigating<br>Evidence: conditional update in RequestRepository, AssignmentService, ConcurrentAcceptTests.cs |
| RSK-011 | Security — Authorization / Access Control | **Cause:** Authorization depends on both role and resource ownership/scope.<br>**Event:** An endpoint or query applies role checks without enforcing the required ownership/team/category scope, or an endpoint bypasses the central policy.<br>**Consequence:** A requester or staff member may access another user's or an unauthorized staff request. | M / H<br>High (6) | Keep one server-side authorization policy, deny by default, and apply ownership/scope filtering at query level. Add negative tests for requester privacy, staff scope and protected actions. Complete the team/category/assignment scope currently identified as incomplete. | Disable the affected operation, inspect access records, correct the policy/query and record any confirmed exposure as a realized security risk. | **G. Enright**<br>Mitigating<br>Evidence: AuthorizationPolicy, ADR-005, current RTM security trace; scope limitations remain |
| RSK-012 | Security — Authentication | **Cause:** The real authentication mechanism remains open under CR-003.<br>**Event:** Development identity handling is accidentally treated as production authentication, or real credential/session handling is delayed until late construction.<br>**Consequence:** Access-control guarantees cannot be trusted for real users and deployment must not proceed with the development identity mechanism. | M / H<br>High (6) | Resolve CR-003 and complete ADR-005's authentication portion before non-development use. Keep the current development identity explicitly Development-only and fail closed outside Development. | Continue using seeded Development users only for local construction and prevent non-development startup until real authentication is configured. | **G. Enright**<br>Open<br>Evidence: development-only CurrentUser implementation and fail-closed startup; real authentication absent |
| RSK-013 | Data / Audit | **Cause:** Application code exposes append-only history semantics, but database-level permissions do not yet enforce insert/select-only access for the application role.<br>**Event:** A future code path or direct database access modifies/deletes history.<br>**Consequence:** Audit evidence can be altered or removed. | L / H<br>Medium (3) | Keep no-update/no-delete application paths and write history in the same transaction as controlled actions. Add database role permissions and automated checks before production use. | Reconcile audit rows against controlled state, restore from a trusted backup where required and document any repair. | **K. Marota**<br>Mitigating<br>Evidence: append-only repository/application design; DB grant hardening remains outstanding |
| RSK-014 | Cost / Deployment | **Cause:** The intended deployment uses free/low-cost Render and Neon tiers with resource, inactivity and service-limit constraints.<br>**Event:** A free-tier limit is reached or provider conditions change.<br>**Consequence:** Demonstration availability or cost sustainability is affected. | M / M<br>Medium (4) | Record provider limits and expected usage in ADR-007. Keep the application portable and avoid provider-specific dependencies. Verify actual deployment resource use before demonstration. | Use local infrastructure for demonstration or move to the documented paid tier if a free-tier limit blocks the project. | **G. Enright**<br>Mitigating<br>Evidence: ADR-007 records Render/Neon direction and cost assumptions |
| RSK-015 | Data / Availability | **Cause:** Durable state depends on a single managed PostgreSQL instance and the chosen free tier does not provide the same backup capability as a paid production service.<br>**Event:** Database outage, corruption or data loss occurs.<br>**Consequence:** Requests, history and accountability records become temporarily or permanently unavailable. | L / H<br>Medium (3) | Record the accepted free-tier backup limitation. Take a manual database branch before risky schema/data changes and document restore procedures. Revisit managed backups if the project continues beyond coursework. | Restore from the latest available copy/branch, communicate the recoverable data-loss window and reconstruct missing records where necessary. | **K. Marota with G. Enright**<br>Open<br>Evidence: ADR-007 explicitly records the backup gap; no restore rehearsal evidenced |
| RSK-016 | Design | **Cause:** Design patterns and abstraction can be introduced beyond what the actual first build requires.<br>**Event:** Abstractions become unused, overly complex or inconsistent with the selected architecture.<br>**Consequence:** Construction slows and the engineering defense becomes harder to justify. | M / M<br>Medium (4) | Apply patterns only where the current implementation demonstrates a concrete problem. Keep ADR-004 and ADR-005 tied to actual view composition and authorization responsibilities, and remove unnecessary abstraction during review. | Simplify an over-engineered component and supersede the affected ADR/design decision through controlled change. | **K. Marota**<br>Mitigating<br>Evidence: ADR-004/005 and corresponding application components |
| RSK-017 | Traceability | **Cause:** PED, RTM, ADRs, README, risk evidence and implementation are separate artifacts and can evolve at different speeds.<br>**Event:** Documentation describes an interface, implementation or decision that no longer matches the actual code.<br>**Consequence:** The M2/M3 defense trace becomes unreliable. | M / M<br>Medium (4) | Update RTM implementation/test evidence with code changes. Reconcile PED v2.1, ADRs, README and risk register before baseline sign-off. Treat the current implementation as evidence rather than assuming planned architecture is implemented. | Perform a full reconciliation pass and record each discrepancy as a controlled documentation change before assessment. | **M. Malope with all**<br>Mitigating<br>Evidence: current PED/RTM/ADR/application set; known discrepancies remain |
| RSK-018 | Scope / Change | **Cause:** CR-001 to CR-009 remain unresolved while affected requirements are already represented in implementation/design documentation.<br>**Event:** A change is approved after dependent code/schema work has progressed.<br>**Consequence:** Rework and late baseline changes occur. | M / M<br>Medium (4) | Resolve change requests in dependency order. Mark blocked requirements explicitly and do not silently implement rejected/proposed behavior. Update the RTM and ADRs in the same controlled change. | Freeze the affected feature at the current conservative interpretation and defer the remainder to M3. | **M. Malope**<br>Open<br>Evidence: all nine CRs remain Open/Proposed |
| RSK-019 | Technology / Dependency | **Cause:** Package versions are now specified, but dependency monitoring, lock/restore reproducibility and vulnerability alerts are not fully evidenced.<br>**Event:** A dependency becomes incompatible, vulnerable, unsupported or incorrectly documented.<br>**Consequence:** Build failure, security exposure or unsupported technology enters the project. | M / M<br>Medium (4) | Keep package versions pinned, verify versions/licenses against authoritative sources, add dependency monitoring and record the evidence in ADR-003. | Pin/replace the affected package and document the impact through ADR-003 and the RTM. | **G. Enright**<br>Mitigating<br>Evidence: project files pin major dependency versions; monitoring not yet evidenced |
| RSK-020 | Process / Quality / CI | **Cause:** A CI workflow now exists, but it targets the app/ directory without a solution/project at that directory root, and the integration tests require CIVICCONNECT_TEST_DB while the workflow provides no PostgreSQL test database.<br>**Event:** CI fails to restore/build/test consistently or does not execute the intended integration evidence.<br>**Consequence:** Pull requests may appear controlled without actually providing a reliable automated quality gate. | M / H<br>High (6) | Make the CI workflow explicitly target the correct solution/project, provision or otherwise configure the required test database, and make successful build/unit/integration checks visible and required for substantive PRs. | Temporarily restrict required CI checks to the verified test set while the database-backed integration job is fixed; do not treat an unexecuted integration test as evidence. | **G. Enright**<br>Open<br>Evidence: .github/workflows/ci.yml exists; current app/ structure and integration-test fixture expose configuration gaps |
| RSK-021 | Security — Input / Data Handling | **Cause:** The system accepts requester-supplied text and query parameters across API and UI paths, while validation responsibilities are split between implementations.<br>**Event:** Unvalidated, overlong, malformed or unexpected input reaches persistence or business logic.<br>**Consequence:** Data integrity problems, avoidable application errors, or an input-driven security defect could occur. | M / H<br>High (6) | Centralize input validation at the application boundary. Enforce required fields, maximum lengths, controlled enum/category values, bounded paging and a whitelist for sort parameters. Keep database constraints as a second line of defense and add negative tests for malformed input. | Reject the affected request with a controlled validation response, log the failure without storing sensitive input, and patch the relevant boundary before re-enabling the feature. | **K. Marota**<br>Mitigating<br>Evidence: backend Validation service, DTOs, domain required-field checks and database constraints; coverage is not yet uniform across both implementation trees |
| RSK-022 | Architecture / Integration | **Cause:** The repository currently contains two overlapping implementation trees: Backend/ with a Web API implementation and app/ with a separate .NET application/Razor Pages implementation and its own domain/application/infrastructure layers.<br>**Event:** The two implementations diverge in rules, interfaces, validation, authorization or persistence behavior, or the team is unclear which one is authoritative.<br>**Consequence:** Duplicate work, inconsistent security behavior, broken integration and weak architecture-to-code traceability. | M / H<br>High (6) | Declare the canonical implementation path, reconcile the RTM/API/architecture documentation against it, and either integrate or archive the competing implementation. CI must build and test the canonical tree. Security controls must not be considered complete until the canonical path is unambiguous. | Freeze the non-canonical implementation and use it only as reference/archival material. If integration is required, create a dedicated controlled migration/integration task rather than merging both trees opportunistically. | **K. Marota with all**<br>Open<br>Evidence: both Backend/ and app/ contain substantial overlapping implementation code |

---
## Baseline Sign-Off

| Field | Details |
| :--- | :--- |
| **Project** | CivicConnect |
| **Baseline Type** | Milestone 3 (M3) |
| **Version** | v3.0 |
| **Date** | 14/10/2026 |
| **Scope Reviewed** | Pending |
| **Requirements / Traceability Checked** | Pending |
| **Risk Review Completed** | Pending |
| **Repository / Governance Controls Checked** | Pending |
| **Outcome** | **Pending** |

---

## References

* Gong, Y. & Yan, J. & Soliman, X., 2020. *Towards a comprehensive understanding of digital transformation in government: Analysis of flexibility and enterprise architecture*. Government Information Quarterly, 37(3), p. 101487.
* Mergel, I., Edelmann, N. & Haug, N., 2019. *Defining digital transformation: Results from expert interviews*. Government Information Quarterly, 36(4), p. 1101385.