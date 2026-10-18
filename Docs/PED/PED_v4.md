# CivicConnect Requirements & Engineering Baseline (PED) (v4.0 Final)

**Authors:** Gerald Enright (577830) | Keletso Marota (601632) | Mogau Malope (600192)  
**Course Code:** SEN381  
**Date:** 18/10/2026 (v3.0: 05/10/2026, archived in `Docs/PED/_Archive/PED_v3.0.md`)

### Final Submission Information
| Field | Details |
| :--- | :--- |
| **Team** | Virtual Group I |
| **Team members** | Gerald Enright (577830), Keletso Marota (601632), Mogau Malope (600192) |
| **GitHub repository** | https://github.com/Gemdave/SEN381_Project-2026 |
| **Final release** | Tag `v1.0.0` on `main` (merge of `SignIn-Module`); the release candidate was `v1.0.0-rc1` |
| **PED version** | v4.0 Final (this document) |
| **Final RTM** | `Docs/Risks/RTM/CivicConnect-RTM-v4.0.xlsx` |
| **Product access** | Run locally as described in the README. Real sign-in (Production) needs the environment variables in `Docs/Requirements/CR-003_Sign_In_Implementation.md`, section 4; assessor credentials are given in the Moodle submission, not in the repository |
| **Date submitted** | 18/10/2026 |

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
10. [Verification Strategy and Test Plan](#verification-and-test-plan)
11. [Technical Constraints and Assumptions](#technical-constraints-and-assumptions)
    - [Initial Architecture Strategy](#initial-architecture-strategy)
    - [Engineering Decision Log](#engineering-decision-log)
    - [Technical Constraints](#technical-constraints)
12. [Engineering Decisions and Comparisons](#engineering-decisions-and-comparisons)
    - [Decision Register (Final)](#decision-register-final)
13. [M3 Overview](#m3-overview)
14. [M2 Baseline Conformance (Requirements)](#m2-baseline-conformance-requirements)
15. [Controlled Changes (M3)](#controlled-changes-m3)
16. [Release Readiness (Requirements)](#release-readiness-requirements)
17. [Final Product (M4)](#final-product-m4)
18. [M1 Scope to Final Delivery](#m1-scope-to-final-delivery)
19. [Final Deployment and Go-Live Decision](#final-deployment-and-go-live-decision)
20. [Project Reflection](#project-reflection)
21. [Known Limitations and Future Work](#known-limitations-and-future-work)
22. [Risk Register](#risk-register)
23. [Baseline Sign-Off](#baseline-sign-off)
24. [References](#references)

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
* **NFR-010 - Growth Readiness (CR-011):** CivicConnect can later support more users or features without a redesign. No volume target is set, since no growth figures exist. Checks: a second copy of a service changes no behavior; no service touches another service's schema in the shared PostgreSQL instance (ADR-001); a new service needs no schema change elsewhere.

### Open Items to Confirm in Later Milestones
1. Authentication model assumed; final selection deferred to M2. **Status:** Open under CR-003. Authorization is covered by ADR-005, but the sign-in mechanism has not been chosen.
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
| **ASR-02** | Authorization and privacy: every read and write is authorized on the server; another requester's reference is refused. | Each service checks identity and role for its own data; role rules keep one source of truth. | ADR-005, ADR-001 (rev.) |
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
| **Passing run** | Run on the `v1.0.0` merge to `main` | [Actions run URL] |
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

**Verification evidence.** CI builds and tests only the `app/` tree. `StatusTransitionTests` and `AssignmentServiceTests` run in the Quality Gate, while `ConcurrentAcceptTests` is skipped until CI provides a test database. RTM rows that cite `Backend/` tests are marked "Not in CI" and do not count as verified.

**Final (v4.0).** The table above keeps the M1 baseline statuses as history. The final RTM is `Docs/Risks/RTM/CivicConnect-RTM-v4.0.xlsx`: every requirement now traces through implementation, tests and evidence to a final acceptance status (column W) and outcome (column X). All 36 FR acceptance criteria are Met; NFR-001 and NFR-009 are Partially Met and NFR-010 is Not Met (see M1 Scope to Final Delivery). CI now provides a PostgreSQL database, so `ConcurrentAcceptTests` and every other integration test run there, and `Backend/` has been removed (RSK-022).

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

**Final (v4.0):** every change now has a final status in `Docs/Risks/RTM/CivicConnect-Change-Register-v4.0.xlsx` (column I), summarised in Controlled Changes (M3).

---
## Verification and Test Plan
**Claim:** The release candidate is verified by a small, risk-based suite that protects the behavior that is actually built and matters most: controlled status transitions, single ownership under concurrency, role-based access, insert-only history and transactional integrity.  
**Basis for selection:** cases were chosen from the acceptance criteria, ASRs and highest-rated risks (RSK-010 double ownership, RSK-011 access control, RSK-009 partial saves, RSK-021 input handling), not to maximize test count. Expected results come from the PED and ADRs, not from observed output.  
| Level | Cases | Focus |
| :--- | :--- | :--- |
| Unit/component | UT-01 to UT-07 | Transition rules, permission checks before writes, note requirement, ownership conflict |
| Black-box functional (HTTP API) | BB-01 to BB-05 | Invalid transitions, note length boundaries, queue scope and filters, actor and action combinations |
| API/integration (PostgreSQL) | IT-01 to IT-07 | Concurrent accept, insert-only history, database ownership rule, rollback, requester privacy, full lifecycle |
| E2E/system | E2E-01, E2E-02 | Accept to close journey and concurrent accept, through the real application and database |
| Performance | PT-01 | Staff queue load with 1,000 requests (NFR-004, target 3 seconds or less) |

**Coverage of the minimum requirements:** 21 distinct cases (7 unit, 5 black-box, 7 integration, 2 E2E), with 13 negative or unauthorized cases, 15 addressing high-priority requirements or risks, and four black-box techniques (state transition, boundary value analysis, equivalence partitioning, decision table of actor and action). Further tests are kept for regression but are not counted toward the minimum.  
**Scope:** the plan covers the implemented core slice (FR-007 to FR-009, FR-011 to FR-014, ASR-01 to ASR-03, NFR-003 to NFR-005). Request submission and the requester history (FR-001 to FR-004) are not implemented and have no verification evidence.  
**Execution:** all counted cases except the staging checks run in the CI Build and Test job against a PostgreSQL service, and a failure blocks the Quality Gate. A skipped test fails the job, because a skipped test is not evidence. E2E-03 (a UI walkthrough) and PT-01 run against the simulated staging environment and are recorded separately.  
**Results:** all 21 counted cases passed (44 of 44 executed cases, 0 failed, 0 skipped) in the CI run of 14/10/2026; E2E-03 and PT-01 are recorded separately once run on staging.  
**Evidence:** [Test plan and evidence record](../../Test/Docs/Test_Plan_v2.md) | [CI run of 14/10/2026](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178) | [CI test results (TRX)](../../Test/CI-Artifacts/TestResultReports/) | [Simulated production verification](../../Test/Evidence/Final_Simulated_Production.md) and [PT-01 results](../../Test/Evidence/PT-01_Results.md) | [RTM v4.0 test columns](../Risks/RTM/CivicConnect-RTM-v4.0.xlsx)  
**Final (v1.0.0):** the suite has grown to 288 automated tests (89 domain, 92 application, 107 integration), all passing on 18/10/2026. It now covers FR-001 to FR-023 and real sign-in (`SignInTests`, `AppUserSignInTests`, `Pbkdf2PasswordHasherTests`); the counted minimum above is unchanged. The RTM uses the Test Plan v1 case IDs and this section uses Test Plan v2 IDs, so the RTM names tests by class as well.
**Limitations:** the suite does not prove the absence of defects; access-control results rely on the development identity stand-in, so they say nothing about real authentication (RSK-012); E2E-01 and E2E-02 exercise the API, not the browser UI; the CI timing check is only a regression guard, and staging performance on a local machine does not predict municipal-scale load.

---
## Technical Constraints and Assumptions

* **Achievability:** Technical implementation must fit within team skills and schedule limitations.
* **Layer Separation:** User interface, backend API, and persistence layers must remain distinct.
* **Business Logic Centralization:** Backend layer must handle validation, rules, and security enforcement.
* **Privacy by Design:** System architecture must guarantee requesters cannot view data belonging to other users.

### Initial Architecture Strategy
The initial architecture utilizes a backend API layer situated between the frontend UI and data persistence layer. This provides a central point for validation, business rules, and authorization enforcement while decoupling domain logic from UI components. Backend framework, database choice, API standard, authentication scheme, and deployment details are directional for M1 and will be finalized in M2.

### Architecture Update (ADR-001, D-005)
The first M2 version of ADR-001 chose a layered modular monolith. The team has since decided to move to microservices, so ADR-001 was rewritten and the change is recorded as D-005. The backend/API layer from D-001 and D-002 is kept, but it is now delivered as four services behind one API entry point:

| Deployable | Responsibility |
| :--- | :--- |
| **Requests service** | Creating requests, ownership, status changes and the history record for each change. |
| **Access service** | Users, roles, permissions and the RBAC policy (ADR-005). |
| **ReferenceData service** | Categories and other lookup data. |
| **Reporting service** | Management and oversight figures, built from request data. |
| **Web frontend (`CivicConnect.Web`)** | Razor Pages UI, run and deployed separately. It has no database access and no business rules, and gets all data through the API (ADR-006). |

Rules that keep this safe: only the Requests service can assign a request or change its status, so the single-owner rule (NFR-005) stays inside one transaction; services do not read each other's tables; the services share one managed PostgreSQL instance with a separate schema per service to stay within free-tier limits (NFR-009); and every service applies the ADR-005 policy rather than trusting the caller.

Main costs: more hosting units and deployment work (NFR-009, RSK-014), cross-service calls that can fail or be slow, no single transaction across services, and harder testing. The team's limited experience with the stack remains the main risk (RSK-001), so a small two-service proof of concept should come before the full split. ADR-002, ADR-005, ADR-006 and ADR-007 still describe a single application and need matching updates.

**Final (v4.0):** the four-service split was not built. The release is one deployable, accepted as technical debt (CR-014, TD-01, RSK-025), so NFR-010 is not met.

### Engineering Decision Log

| ID | Decision | Reason | Status |
| :--- | :--- | :--- | :--- |
| **D-001** | Separate UI, backend/API, and data persistence layers. | Supports maintainability, clean separation of concerns, and modular testing. | Carried forward into ADR-001 |
| **D-002** | Use a central backend/API layer for application operations. | Provides a consistent location for validation, business rules, and access control. | Carried forward into ADR-001 |
| **D-003** | Defer final backend framework and database selection to M2. | Tech selection requires structured evaluation and proof-of-concept testing. | Superseded by ADR-002 (database) and ADR-003 (stack) |
| **D-004** | Treat authentication and authorization as M2 decisions. | Specific security architecture depends on stack selection and RBAC refinement. | Superseded by ADR-005 (authorisation) and ADR-008 (authentication, CR-003) |
| **D-005** | Build CivicConnect as independently deployable services (Requests, Access, ReferenceData, Reporting) behind one API entry point, with the web frontend deployed separately. Replaces the layered modular monolith. | Services can be built, deployed and changed on their own, and ownership and history stay inside Requests so NFR-005 still holds. Accepted trade-offs: more hosting and deployment work, cross-service failures, harder testing. | Approved, recorded in ADR-001 (PR #23); not built (CR-014) |

### Technical Constraints

| Constraint | Consideration |
| :--- | :--- |
| **Schedule** | Target frameworks must allow rapid development within strict milestone timelines. |
| **Stack Compatibility** | Selected libraries and tools must integrate seamlessly across the application stack. |
| **Learning Curve** | Priority is given to frameworks familiar to the team to minimize execution risk. |

---
## Engineering Decisions and Comparisons

Every decision is compared here against CivicConnect's own drivers, including those researched in Assignment 2 or 3; full records stay in `Docs/Decisions/`. Each option is rated Strong, Fair or Weak per criterion; criteria come from the ASRs, requirements or risks named in them and are listed in priority order (the first three are equally important in Part 1, the first two in Part 2). The final status and observed consequence of every decision is in the Decision Register (Final) at the end of this section.

### ADR-004: Role-Based View Composition (Design Decision 1)

**Status:** Accepted, implemented in v1.0.0 (revision 2, 08/10/2026). **Owner:** M. Malope. **Research:** A2 Task 1 (Naghdipour, 2023; Silva, 2021). **Full record:** `Docs/Decisions/ADR-004-role-based-view-composition.md`. **Problem:** Four roles use the same request data with different fields and actions, and a requester must never get a status control or see another requester's data (AC-003.2, NFR-003). With services, one screen draws on several services, so there are two questions: where screens get their data (Part 1, new) and how each role's pages are built (Part 2, from revision 1).

**Part 1: where the screens get their data**

| Criterion (priority order) | A. Browser calls services | B. Gateway merges | C. One frontend service | D. Frontend per role | E. Micro-frontends |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ASR-02** Privacy | **Weak** Every service exposed to the browser | **Fair** Role shaping in shared infrastructure | **Strong** Services hidden; controls set server-side | **Strong** Smallest exposure per role | **Fair** Many exposed pieces |
| **ASR-06** Consistent screens | **Fair** Status logic repeated in browser code | **Fair** Consistent only if the gateway shapes data alike | **Strong** One shared status partial | **Fair** Shared partials copied or packaged per service | **Weak** Pieces can look and behave differently |
| **ASR-05** Cost for a team of three | **Weak** Screens rewritten client-side | **Fair** Custom merge code in the gateway | **Strong** One extra deployable reusing existing pages | **Weak** Three or four deployables, each with its own pipeline, configuration, secrets and hosting slot (NFR-009) | **Weak** Most build and deployment work |
| **ASR-07** Growth | **Fair** Screens change when services split | **Fair** Every screen change passes the gateway | **Fair** Runs as copies; splits per role later | **Strong** Each role scales and releases alone | **Strong** Each piece scales and releases alone |
| **ASR-04** Oversight speed | **Weak** Many browser round trips | **Strong** One call per screen | **Fair** Several server-side calls | **Strong** Tailored per role | **Fair** Pieces load separately |
| **Overall** | No Strong, 3 Weak | 1 Strong, no Weak | **3 Strong, no Weak** | 3 Strong, Weak on cost | 1 Strong, 2 Weak |

**Part 2: how each role's pages are built.** Build effort ranks lower here than in Part 1, because these options differ only in coding effort inside one service, not in services to build, host and pay for.

| Criterion (priority order) | P1. Page per role | P2. Role conditions in markup | P3. Partials, page models, capability object | P4. Per-role strategy |
| :--- | :--- | :--- | :--- | :--- |
| **ASR-06** Status the same for all roles | **Weak** Four copies drift | **Fair** Branches multiply | **Strong** One partial per status piece | **Fair** Rendering split across strategies |
| **ASR-02** No role logic in markup | **Fair** Loading rules repeated | **Weak** Role checks spread through views | **Strong** Server-built capability object; handlers re-check | **Fair** Logic in strategies |
| **ASR-05** Build effort | **Fair** Every change made four times | **Strong** Fastest | **Fair** Capability object to build and pass | **Weak** Extra layer against Razor conventions |
| **Changeability**, from FR-023 and RSK-016 | **Weak** Copy everything | **Weak** New branches affect all roles | **Strong** New page folder on existing partials | **Fair** New strategy plus wiring |
| **Testability**, from RTM verification evidence | **Fair** Each page tested four times | **Weak** Every branch per role | **Fair** Page tests with a seeded user | **Strong** Strategies tested alone |
| **Overall** | No Strong, 2 Weak | 1 Strong, 3 Weak | **3 Strong, no Weak** | 1 Strong, 1 Weak |

**Decision.** Option C with P3 inside it. One web frontend service serves every role from role-separated page folders over shared partials, and the browser talks only to it. The capability object only decides which controls appear; its values come from the owning service, which re-checks every action. The frontend keeps no in-memory session state, so it can run as several copies. C and D both have three Strong ratings, but D is Weak on cost, one of the three top-priority criteria, because it adds three or four deployables now, while C's page folders are where D can be split out later. **Costs and triggers:** one more deployable and network hop per screen, and a frontend defect can affect every role. Split per role (D) only if measurement shows one role holding the others back; simplify P3 if the sponsor view (FR-023) cannot reuse the partials.

### Decision Register (Final)
Statuses are final for `v1.0.0`; full records are in `Docs/Decisions/`.

| Decision | Choice | Owner | Final status | Consequence observed in the final product |
| :--- | :--- | :--- | :--- | :--- |
| **ADR-001** Architecture style | Four services behind one API entry point (D-005) | K. Marota | Approved, not implemented (CR-014) | The release is one deployable. Building it as one kept the team on schedule, but NFR-010 is not met and the split remains future work (TD-01). |
| **ADR-002** Persistence and integrity | Conditional update with append-only history | K. Marota | Accepted, implemented | Positive: eight simultaneous accepts give exactly one owner; history cannot be edited (database triggers). |
| **ADR-003** Technology stack | C#/.NET 10, Razor Pages, EF Core, PostgreSQL 17, xUnit | G. Enright | Accepted, implemented | Positive: one language end to end; EF migrations gave a repeatable schema for every environment. |
| **ADR-004** View composition | One web frontend; partials, page models and a capability object | M. Malope | Accepted, implemented in the single deployable | Positive: the Sponsor role (CR-007) was added as one new page folder; every role shows only its own actions. |
| **ADR-005** Authorisation policy | One policy for role, ownership and category scope | G. Enright | Accepted, implemented | Positive: the role matrix test covers every role and action; deactivation and role changes apply on the next request. |
| **ADR-006** API contract | REST interface | K. Marota | Accepted, with deviation (CR-015) | The built API uses POST for status changes and GUID ids; the contract document was not updated (TD-03). |
| **ADR-007** Deployment direction | Managed hosting (Render) and managed PostgreSQL (Neon) | G. Enright | Accepted, partly implemented | Verified as simulated production only; no container or hosting yet (TD-04). |
| **ADR-008** Authentication (new) | Cookie sign-in over the users table, PBKDF2 hashes | G. Enright | Accepted, implemented (CR-003) | Real sign-in outside Development; no MFA and administrator-only password reset remain (RSK-012). |
| **D-001, D-002** Layers and central backend | Carried into ADR-001 | Team | Closed | Realised as Domain, Application and Infrastructure layers inside one application. |
| **D-003, D-004** Deferred stack and security decisions | Deferred from M1 | Team | Superseded by ADR-002, ADR-003, ADR-005, ADR-008 | Closed. |
| **D-005** Microservices | Recorded in ADR-001 | K. Marota | Not built (CR-014) | As ADR-001. |

---

## M3 Overview
**Release candidate:** the merged build of `main` and `frontend-milestone-3-development`, with its scope frozen in `Docs/Scope freeze proposal/Scope_Freeze_Proposal_v2.0.md` and the post-freeze work brought in by CR-016.  
**Implemented:** FR-001 to FR-023 and NFR-002 to NFR-008. NFR-001 awaits its walkthrough. **Not delivered:** NFR-009 (nothing deployed) and NFR-010 (needs the service split, CR-014).  
**Status:** 211 of 211 automated tests pass locally (16 Oct 2026), CI on main runs the integration tests against PostgreSQL, 26 of 26 acceptance checks pass, and PT-01 passes (p95 0.064 s). There is no staging yet (B2). Evidence: `Test/Evidence/` and `Docs/Risks/RTM/CivicConnect-RTM-v3.0.xlsx`.

## M2 Baseline Conformance (Requirements)
Most requirement areas match the M2 baseline. CR-003, CR-009, CR-012 and CR-016 control the deviations. Two remain open for Architecture: one deployable instead of separate services (CR-014), and the ADR-006 contract (CR-015). The full table is in `Docs/Requirements/M3_Baseline_Conformance.md`.

## Controlled Changes (M3)
The M2 table in Controlled Changes to the Baseline is kept as history. The M3 decisions are in full in `Docs/Risks/RTM/CivicConnect-Change-Register-v3.0.xlsx`.

| Decision | CRs |
| :--- | :--- |
| Approved and applied | CR-001, CR-002, CR-004, CR-008, CR-011, EC-001 |
| Approved and implemented | CR-005 (overdue targets), CR-006 (CSV export), CR-007 (sponsor summary), CR-009 (Rejected status) |
| Proposed | CR-012 (the owner in scope or an administrator closes), CR-016 (post-freeze work in the release candidate) |
| Withdrawn | CR-013 (superseded by CR-016) |
| Deferred to M4 | CR-003 (sign-in) |
| Raised for Architecture | CR-010 (approved, not built), CR-014 (one deployable), CR-015 (POST, GUID ids) |

**Final (v1.0.0):** all 17 records are closed in Change Register v4.0. CR-003 (real sign-in), CR-012 and CR-016 are approved and implemented; CR-014 and CR-015 are accepted as technical debt (TD-01, TD-03); CR-010 is closed as not implemented; CR-013 stays withdrawn.

## Release Readiness (Requirements)
| Requirements | State |
| :--- | :--- |
| FR-001 to FR-023, NFR-002 to NFR-008 | Delivered and verified (automated; acceptance for the core journey). FR-019 and FR-020 against the development sign-in only |
| NFR-001 | Delivered; walkthrough pending |
| NFR-009, NFR-010 | Not delivered |

**Open blocker:** B2 (staging). **Accepted limitations:** development identity instead of sign-in (RSK-012), and local evidence only. The gate summary and residual risk are in CI & Automated Verification and the Risk Register.

### Release Evidence Summary
**Release candidate:** `v1.0.0-rc1` on `main`. **Classification at M3: CONDITIONALLY READY.** All requirements were built and verified for demonstration; not ready for public use. The final decision for `v1.0.0` is in Final Deployment and Go-Live Decision.

| Evidence | Result | Source |
| :--- | :--- | :--- |
| Scope | FR-001 to FR-023, NFR-002 to NFR-008 (CR-016) | [Scope freeze](../Scope%20freeze%20proposal/Scope_Freeze_Proposal_v2.0.md), [Change register](../Risks/RTM/CivicConnect-Change-Register-v3.0.xlsx) |
| Tests | 211 of 211 pass locally; CI run on the tag to be linked | [RTM v3.0](../Risks/RTM/CivicConnect-RTM-v3.0.xlsx), [CI gates](../CI_Documentation/CI_Quality_Gates.md) |
| Acceptance | 26 of 26 checks pass | [Acceptance checks](../../Test/Evidence/M3_Acceptance_Checks.md) |
| Performance | PT-01 p95 0.064 s against a 3 s target, local only | [PT-01 results](../../Test/Evidence/PT-01_Results.md) |

**Conditions:** real sign-in (CR-003, RSK-012), staging (B2), the NFR-001 walkthrough, and a green CI run on the tag. **Residual risks:** RSK-010, RSK-012, RSK-015 ([Risk Register v4.0](../Risks/Risk_Register_v4.0.md)).

### Defect Register
| ID | Defect | Final status (v1.0.0) |
| :--- | :--- | :--- |
| DEF-001 | Staff details lacked requester and category (AC-010.1) | Fixed; acceptance check AC-010.1 passes |
| OBS-01 | Accept showed on requests already owned | Fixed; covered by `StaffScreensTests` |
| OBS-02 | History showed user IDs instead of names | Fixed; covered by `StaffScreensTests` |
| DEF-002 | The audit_entries insert-only trigger had no direct test (NFR-007) | Fixed: `HistoryInsertOnlyTests` now proves the trigger refuses edits and deletes; NFR-007 is Met |

### Technical Debt Register
| ID | Debt | Final status (v1.0.0) | Consequence if kept |
| :--- | :--- | :--- | :--- |
| TD-01 | One deployable, not separate services (CR-014) | Retained | NFR-010 not met; scaling means scaling the whole application |
| TD-02 | Development sign-in, no real authentication (CR-003) | Repaid: real sign-in (ADR-008) | None |
| TD-03 | API contract differs from the build (CR-015) | Retained | An API client written from `openapi.yaml` would use PATCH and integer ids and fail |
| TD-04 | No deployment, migration step or Dockerfile (ADR-007) | Partly repaid: production startup and migration step built; no container or hosting | Hosted go-live needs a Dockerfile and deployment pipeline |
| TD-05 | Legacy `Backend/` tree still in the repo (RSK-022) | Repaid: removed | None |
| TD-06 | Mobile layout and accessibility review deferred | Retained | Requesters on phones get a desktop layout; accessibility not formally checked |


## Final Product (M4)
**Release:** `v1.0.0` on `main`, the merge of `SignIn-Module`. **Product status:** complete for its approved scope and stable: 288 of 288 automated tests pass and the simulated production check passes 13 of 13 (18/10/2026).

**What changed since the release candidate (`v1.0.0-rc1`):** finalisation, not new scope.
* Real sign-in outside Development (CR-003, ADR-008), which was deferred to M4; it closes TD-02 and mitigates RSK-012.
* The legacy `Backend/` tree removed, leaving one implementation (RSK-022, TD-05).
* A production startup step that prepares a fresh database with the 7 categories and the first administrator, with no development data.
* DEF-002 closed: the audit trail's insert-only trigger now has a direct test, so NFR-007 is Met.
* Records concluded: RTM v4.0, Change Register v4.0, Risk Register v4.0, final ADR statuses and the new ADR-008.

**Critical journeys demonstrated on this release:** a requester submits a request and follows it to closure; staff accept, update, note and resolve or reject it; a manager sees overdue work and exports the performance figures; an administrator manages users, roles and categories behind real sign-in.

## M1 Scope to Final Delivery
**Final status categories:** IMPLEMENTED, PARTIALLY IMPLEMENTED, NOT IMPLEMENTED, CHANGED/REMOVED THROUGH APPROVED CHANGE. Deferred and out-of-scope items are listed separately and are not counted as failures.

| M1 scope item | M1 priority / status | Final status | Acceptance criteria | Evidence | Conclusion |
| :--- | :--- | :--- | :--- | :--- | :--- |
| FR-001 Submit a request | Must / In scope | IMPLEMENTED | AC-001.1, AC-001.2 Met | RTM v4.0; `SubmissionServiceTests`, E2E-01 | Delivered as committed |
| FR-002 Controlled category | Must / In scope | IMPLEMENTED | AC-002.1, AC-002.2 Met | UT-02 | Delivered as committed |
| FR-003 View status (read only) | Must / In scope | IMPLEMENTED | AC-003.1, AC-003.2 Met | E2E-01 | Delivered; resolves conflict C-1 |
| FR-004 Own history | Must / In scope | IMPLEMENTED | AC-004.1, AC-004.2 Met | BB-05, E2E-01 | Delivered as committed |
| FR-005 Search own history | Should / In scope | IMPLEMENTED | AC-005.1 Met | `ManagementScreensTests` | Delivered (CR-016) |
| FR-007 In-app feedback | Must / In scope | IMPLEMENTED | AC-007.1 Met; AC-007.2 Met through CR-009 | E2E-02, `UpdatesPollingTests` | Delivered; Rejected added by approved change |
| Category list as reference data | In scope | IMPLEMENTED | Covered by FR-002, FR-021 | `CategoryServiceTests` | Delivered |
| FR-008 to FR-014 Service staff | Must (baselined at M2) | IMPLEMENTED | All 12 Met (AC-014.2 as reworded by CR-012) | RTM v4.0 | Delivered |
| FR-015 to FR-018 Management | Must and Should | IMPLEMENTED | All 6 Met | `ReportingTests` | Delivered (CR-016; CR-005, CR-006) |
| FR-019 to FR-022 Administrator | Must and Should | IMPLEMENTED | All 5 Met | `UserAdminTests`, `SignInTests` | Delivered (CR-016; sign-in CR-003) |
| FR-023 Sponsor reporting | Should | IMPLEMENTED | AC-023.1 Met | `ReportingTests` | Delivered (CR-007, CR-016) |
| NFR-002 to NFR-006, NFR-008 | Must and Should | IMPLEMENTED | Met | RTM v4.0; PT-01 | Delivered; NFR-004 measured locally only |
| NFR-001 Usability | Should | PARTIALLY IMPLEMENTED | Partially Met | Five-step form built | The timed first-user walkthrough was not run |
| NFR-007 Auditability | Must | IMPLEMENTED | Met | `HistoryInsertOnlyTests` (request history and audit trail) | Delivered; DEF-002 closed |
| NFR-009 Cost | Should | PARTIALLY IMPLEMENTED | Partially Met | ADR-007 | $0 plan documented and runs with no paid service; not proven on a hosted free tier |
| NFR-010 Growth readiness | Must (added by CR-011) | NOT IMPLEMENTED | Not Met | CR-014 | Service split not built; accepted as technical debt (TD-01) |

| Deferred or out of scope at M1 | Final status | Conclusion |
| :--- | :--- | :--- |
| Email and SMS notifications (C-4) | Still deferred | Correctly outside the committed scope; in-app feedback (FR-007) delivered |
| FR-006 Attachments (Could) | IMPLEMENTED through CR-016 (AC-006.1 Met) | Delivered beyond the committed scope |
| Full administrator RBAC | IMPLEMENTED through FR-019, FR-020 | Delivered |
| Authentication implementation (D-004) | IMPLEMENTED in M4 through CR-003 (ADR-008) | Delivered |
| Production deployment | CHANGED: simulated production (ADR-007, TD-04) | See Final Deployment and Go-Live Decision |

**Scope Completion Percentage** (Met = 1, Partially Met = 0.5, Not Met = 0; AC-002.3 excluded, it does not exist, EC-001):
* **Committed M1 scope** (FR-001 to FR-005, FR-007): 11 of 11 acceptance criteria Met = **100%**.
* **Full approved baseline** (36 FR acceptance criteria and 10 NFR targets): 43 Met + 2 Partially Met (1.0) + 1 Not Met = 44 of 46 = **95.7%**.

**Conclusion:** both figures exceed the 80% target. Every Must functional requirement is implemented. The only Must item not met is NFR-010, accepted as technical debt through CR-014. The delivered scope is coherent: every stakeholder group (ST-1 to ST-5) has a working journey.

## Final Deployment and Go-Live Decision
**Final deployment state: simulated production.** **Decision: GO for simulated production; NO-GO for a public hosted production release.**

| Simulated production requirement | Evidence |
| :--- | :--- |
| Production-like environment separate from development | `Production` environment, launch profile disabled, a new database separate from the development and test databases, HTTPS only |
| Release identification | `v1.0.0` |
| Production configuration and controlled secrets | Connection string and first-administrator password passed only as environment variables; nothing in the repository (RSK-005) |
| Deployment verification | 13 of 13 checks: sign-in required for every page and the API, development stand-in ignored, Secure/HttpOnly cookie, forced password change, fresh database with categories and one administrator only. `Test/Evidence/Final_Simulated_Production.md` |

**Why not a public production release yet:** no container image or hosting pipeline exists (TD-04, RSK-006); password-hash cost on a free-tier host is unmeasured (RSK-014); no backup or restore has been rehearsed (RSK-015); sign-in has no MFA or self-service reset (RSK-012). Releasing publicly would put requesters' personal data (NFR-003) on an unverified host with no recovery path. **Before go-live:** add a Dockerfile and deployment pipeline with the migration step, measure sign-in on the host, take and restore a Neon branch backup, and confirm GitHub push protection.

## Project Reflection
* **Problem and value:** the final product replaces informal requests with a tracked lifecycle and gives every stakeholder (requesters, staff, management, administrators, sponsor) a working journey, which addresses the M1 problem statement.
* **Scope:** 100% of the committed M1 scope and 95.7% of the full baseline were delivered. Changes were controlled: 16 CRs plus one editorial correction, each with a final status.
* **Quality:** confidence comes from 288 automated tests on a real PostgreSQL database, a concurrency test, a role matrix, 26 of 26 acceptance checks and a production-configuration check. Still uncertain: hosted performance and recovery, and attack resistance (no penetration test).
* **Risk, defects and debt:** 10 of 25 risks closed, 9 mitigated, 6 accepted; all found defects fixed, including DEF-002; TD-02 and TD-05 repaid.
* **Decision consequences:** ADR-002 (conditional update) protected the most important rule, single ownership, from the start. ADR-004 with ADR-005 made new roles cheap: the Sponsor role and real sign-in were added without changing the policy. ADR-001 (microservices) was approved but never built, which left an architecture deviation (CR-014) and NFR-010 unmet.
* **Execution:** two overlapping code trees (`app/` and `Backend/`) cost rework until the requester screens were ported in M3; deferring sign-in (CR-003) to M4 meant security evidence arrived late. Reviewed PRs, CI with a real database and a controlled change register kept the work traceable. AI use is recorded with human verification in the AI Usage Register.
* **Future:** hosted deployment and backups, MFA and self-service reset, the service split, an updated API contract, mobile layout and an accessibility review.

## Known Limitations and Future Work
| Limitation | Effect | Future work |
| :--- | :--- | :--- |
| One deployable instead of services (CR-014, TD-01) | NFR-010 not met; scaling means scaling the whole application | Split Requests, Access, ReferenceData and Reporting as ADR-001 describes |
| No hosted deployment (TD-04) | Verified as simulated production only | Dockerfile, deployment pipeline, hosted sign-in measurement |
| No backup or restore rehearsal (RSK-015) | Data loss on the host could not yet be recovered | Neon branch backups and a restore rehearsal |
| Sign-in without MFA or self-service reset (RSK-012) | Weaker protection for administrator accounts | MFA, emailed reset links, breached-password check |
| API contract outdated (CR-015, TD-03) | External clients would follow the wrong contract | Update `openapi.yaml` and ADR-006 |
| Usability walkthrough not run (NFR-001) | The five-step target is built but not measured | Timed first-user walkthrough |
| Desktop layout only; no accessibility review (TD-06) | Phone users and assistive technology not verified | Responsive layout and a WCAG review |

---

## Risk Register
**Claim:** project risks are identified, rated, owned and tracked, and the verification and CI work is aimed at the highest-rated risks rather than spread evenly.  
**Final position (v4.0):** the register is concluded for `v1.0.0`: 10 risks closed, 9 mitigated with a stated residual, 6 accepted or carried, each with its reason in `Docs/Risks/Risk_Register_v4.0.md`. Closed include RSK-022 (`Backend/` removed) and RSK-008 (ADR-003 accepted); RSK-012 is mitigated by real sign-in, with no MFA as its residual; RSK-025 is accepted as technical debt (CR-014). The M3 position below is kept as history.
**M3 position (v3.0):** the register continues the version from PED v2.2. All 22 original risks are carried forward, one risk was added for CI (RSK-023), one for ambiguous acceptance criteria (RSK-024) and one for architecture conformance (RSK-025). No risk has been closed, and no rating has changed without evidence.  
| Measure | Count |
| :--- | :--- |
| Total risks | 25 |
| High (6) | 12 |
| Medium (3 to 4) | 13 |
| Open | 8 |
| Mitigating | 17 |
| Mitigated or closed | 0 |

### Risks driving quality work
| Risk | Concern | M3 control and evidence |
| :--- | :--- | :--- |
| RSK-010 (High) | Two staff claim the same request | UT-05, IT-01, E2E-02 passed in CI on 14/10/2026 (one owner under eight simultaneous accepts) |
| RSK-011 (High) | Access control without ownership/scope enforcement | UT-03, UT-06, BB-04, BB-05, IT-05, IT-07 passed; they use the development identity stand-in |
| RSK-021 (High) | Malformed or out-of-range input | BB-01, BB-02, BB-03 passed (invalid status, note length boundaries, note required) |
| RSK-020 (High) | CI not reliably running the intended tests | CI runs all test projects against PostgreSQL and fails on skipped tests; all five jobs passed; Quality Gate still to be enforced as a required check |
| RSK-022 (High) | Two overlapping implementation trees | CI and tests target `app/`; canonical declaration and handling of `Backend/` still to be recorded |
| RSK-025 (High) | Approved architecture and implementation disagree | Outcome of PR #23 and a controlled change record still to be recorded |
| RSK-005, RSK-019 | Leaked secrets and vulnerable dependencies | Secrets Scan and Dependency jobs passed in CI; GitHub secret scanning and Dependabot settings to be confirmed |
| RSK-012 (High) | Development identity is not real authentication | Not verified by the suite; stated as a release limitation |

**Interpretation:** the High-rated risks are the ones with controls or tests in place, but several controls are evidenced only on one CI run and use the development identity. Ratings will be re-assessed on the final release candidate run, and the remaining Open risks feed the residual risk statement in the Release Evidence Summary.
**Evidence:** [Risk Register v4.0](../Risks/Risk_Register_v4.0.md) | [Risk Register v3.0](../Risks/_Archive/Risk_Register_v3.0.md) | [Test plan](../../Test/Docs/Test_Plan_v2.md) | [CI quality gates](../CI_Documentation/CI_Quality_Gates.md) | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178)

---
## Baseline Sign-Off

| Field | Details |
| :--- | :--- |
| **Project** | CivicConnect |
| **Baseline Type** | Milestone 3 (M3) |
| **Version** | v3.0 |
| **Date** | 16/10/2026 |
| **Architecture Decision Recorded** | ADR-001 microservices (D-005) |
| **Scope Reviewed** | Yes |
| **Requirements / Traceability Checked** | Yes |
| **Risk Review Completed** | Yes |
| **Repository / Governance Controls Checked** | Yes |
| **Outcome** | **Accepted** |

**Final baseline (M4)**

| Field | Details |
| :--- | :--- |
| **Baseline Type** | Final project (M4) |
| **Version** | v4.0 Final; release `v1.0.0` |
| **Date** | 18/10/2026 |
| **Scope completion** | 100% of committed M1 scope; 95.7% of the full baseline |
| **Deployment decision** | Simulated production (GO); public hosted production (NO-GO) |
| **Records concluded** | RTM v4.0, Change Register v4.0, Risk Register v4.0, ADR-001 to ADR-008, Defect and Technical Debt registers, AI Usage Register |
| **Outcome** | **Concluded** |

---

## References

* Gong, Y. & Yan, J. & Soliman, X., 2020. *Towards a comprehensive understanding of digital transformation in government: Analysis of flexibility and enterprise architecture*. Government Information Quarterly, 37(3), p. 101487.
* Mergel, I., Edelmann, N. & Haug, N., 2019. *Defining digital transformation: Results from expert interviews*. Government Information Quarterly, 36(4), p. 1101385.