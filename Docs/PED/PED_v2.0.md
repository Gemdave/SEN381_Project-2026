# CivicConnect Requirements & Engineering Baseline (v1.5)

**Authors:** Gerald Enright (577830) | Keletso Marota (601632) | Mogau Malope (600192)  
**Course Code:** PEDSEN381  
**Date:** 09/09/2026  

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
   - [Engineering Decision Log](#engineering-decision-log)
   - [Technical Constraints](#technical-constraints)
8. [Risk Register](#risk-register)
9. [Baseline Sign-Off](#baseline-sign-off)
10. [References](#references)

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
| **FR-002** | Categorise the request from a controlled category list (not free text). | ST-1, ST-3 | Must | **AC-002.1:** Given the form, when opening the category field, then only controlled categories are selectable (e.g. Fault, Equipment, Security, IT, Maintenance, Lost Property, Other).<br>**AC-002.2:** Given no category selected, when submitted, then submission is blocked (category required); Empty category is flagged. |
| **FR-003** | View the current status of a submitted request. | ST-1 | Must | **AC-003.1:** Given an owned request, when opened, then status shows in plain language (e.g. Received/Assigned/In Progress/Resolved/Closed).<br>**AC-003.2:** Given a requester views a request, when they try to change status, then no such control exists (read-only). |
| **FR-004** | View a history/list of own previously submitted requests. | ST-1 | Must | **AC-004.1:** Given prior requests, when opening history, then only the requester's own requests show with request information (e.g. ref ID, title, category, status, date, description).<br>**AC-004.2:** Given requester’s own profile, when viewing history, then no other requester's request is visible (privacy). |
| **FR-005** | Search/filter own request history. | ST-1 | Should | **AC-005.1:** Given multiple prior requests, when filtering by status, then only the requester's matching prior requests are listed (clear empty-state if none). |
| **FR-006** | Attach supporting information (e.g. a photo). | ST-1, ST-2 | Could | **AC-006.1:** Given an allowed type/size, when attached, then stored and linked; a disallowed type/size is rejected with a message. (File-handling risk may be deferred.) |
| **FR-007** | Receive feedback when a request is accepted, rejected, updated or completed. | ST-1 | Must | **AC-007.1:** Given a status change to Accepted/Rejected/Updated/Completed, when the requester next visits, then an in-app feedback item with timestamp appears.<br>**AC-007.2:** Given a rejection, when feedback is viewed, then a reason is included. |

### 2. Service Staff

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-008** | View the service requests relevant to authorised staff (staff queue). | ST-2 | Must | **AC-008.1:** Given an authenticated staff member, when they open the requests queue, then only requests they are authorised to see are listed (by team/category/assignment).<br>**AC-008.2:** Given a request outside a staff member's authorisation, when the queue loads, then that request is not visible (access control). |
| **FR-009** | Search, filter or sort requests using useful criteria. | ST-2 | Must | **AC-009.1:** Given the staff queue, when the staff member filters (status/category/date) or sorts, then only matching requests within their authorisation are shown in the chosen order.<br>**AC-009.2:** Given a filter with no matches, when applied, then a clear empty-state message is shown. |
| **FR-010** | View the full details of a request. | ST-2 | Must | **AC-010.1:** Given an authorised request, when the staff member opens it, then full details are shown (requester, category, description, status, action history, attachments if any). |
| **FR-011** | Assign or accept responsibility for a request. | ST-2 | Must | **AC-011.1:** Given an unassigned request, when an authorised staff member accepts/assigns it, then ownership is recorded (owner + timestamp) and the request shows as assigned.<br>**AC-011.2:** Given a request already owned by another staff member, when a second staff member tries to take it, then reassignment follows controlled rules (blocked or authorised reassignment), preventing silent double-ownership. |
| **FR-012** | Update a request's status through controlled transitions. | ST-2 | Must | **AC-012.1:** Given a request in a status, when an authorised staff member advances it, then only valid transitions are allowed (Received $\rightarrow$ Assigned $\rightarrow$ In Progress $\rightarrow$ Resolved $\rightarrow$ Closed) and the change is recorded with actor + timestamp.<br>**AC-012.2:** Given an invalid transition (e.g. Received $\rightarrow$ Closed), when attempted, then it is blocked with a message. (Resolves C-1 with FR-003.) |
| **FR-013** | Record actions, comments or resolution information on a request. | ST-2 | Must | **AC-013.1:** Given an owned/authorised request, when a staff member adds an action, comment or resolution note, then it is saved to the request history with author + timestamp and cannot be silently deleted (accountability). |
| **FR-014** | Resolve or close requests where authorised. | ST-2 | Must | **AC-014.1:** Given an authorised staff member and a resolvable request, when they resolve/close it, then status updates to Resolved/Closed, resolution info is recorded, and the requester receives feedback (links to FR-007).<br>**AC-014.2:** Given a staff member without close permission, when they attempt to close, then the action is blocked (RBAC). |

### 3. Management / Oversight

| ID | Requirement | Source | Priority | Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **FR-015** | View useful service-activity information (oversight overview). | ST-3 | Must | **AC-015.1:** Given an authenticated manager, when they open the oversight view, then aggregate activity is shown (e.g. counts by status and category) across their authorised scope. |
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

## CivicConnect: Requirements Traceability Matrix (RTM)

> **Legend:**
> * **Traceability Target (M4 Chain):** Stakeholder/Source $\rightarrow$ Requirement $\rightarrow$ Acceptance Criteria $\rightarrow$ Design/Architecture $\rightarrow$ Issue/PR $\rightarrow$ Implementation $\rightarrow$ Test $\rightarrow$ Acceptance/Release Evidence. *(Remaining lifecycle columns completed as evidence is produced)*
> * **Source IDs:** ST-1 (Requester), ST-2 (Service Staff), ST-3 (Management), ST-4 (Administrator), ST-5 (Sponsor).
> * **Priority (MoSCoW):** Must, Should, Could, Won't.
> * **Status:** Baseline candidate, Baselined, Candidate / may defer.

| ID | Source | Requirement | Acceptance Criteria | Priority | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **FR-001** | ST-1 | Submit a new service request with the information needed to action it. | AC-001.1; AC-001.2 | Must | Baseline candidate |
| **FR-002** | ST-1, ST-3 | Categorise the request from a controlled category list (not free text). | AC-002.1; AC-002.2 | Must | Baseline candidate |
| **FR-003** | ST-1 | View the current status of a submitted request. | AC-003.1; AC-003.2 | Must | Baseline candidate |
| **FR-004** | ST-1 | View a history/list of own previously submitted requests. | AC-004.1; AC-004.2 | Must | Baseline candidate |
| **FR-005** | ST-1 | Search/filter own request history. | AC-005.1 | Should | Baseline candidate |
| **FR-006** | ST-1, ST-2 | Attach supporting information to a request (e.g. a photo). | AC-006.1 | Could | Candidate may defer |
| **FR-007** | ST-1 | Receive feedback when a request is accepted, rejected, updated or completed. | AC-007.1; AC-007.2 | Must | Baseline candidate |
| **FR-008** | ST-2 | View the service requests relevant to authorised staff (staff queue). | AC-008.1; AC-008.2 | Must | Baseline candidate |
| **FR-009** | ST-2 | Search, filter or sort requests using useful criteria. | AC-009.1; AC-009.2 | Must | Baseline candidate |
| **FR-010** | ST-2 | View the full details of a request. | AC-010.1 | Must | Baseline candidate |
| **FR-011** | ST-2 | Assign or accept responsibility for a request. | AC-011.1; AC-011.2 | Must | Baseline candidate |
| **FR-012** | ST-2 | Update a request's status through controlled transitions. | AC-012.1; AC-012.2 | Must | Baseline candidate |
| **FR-013** | ST-2 | Record actions, comments or resolution information on a request. | AC-013.1 | Must | Baseline candidate |
| **FR-014** | ST-2 | Resolve or close requests where authorised. | AC-014.1; AC-014.2 | Must | Baseline candidate |
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
| **NFR-006** | ST-4 | Security / RBAC: every action authorised by role. | Unauthorised access denied (supports FR-008/014/020). | Must | Candidate |
| **NFR-007** | ST-4 | Auditability: key actions recorded and tamper-evident. | Actor + timestamp, not silently altered (supports FR-013/022). | Must | Candidate |
| **NFR-008** | ST-3, ST-5 | Reporting accuracy: reports reconcile with underlying records. | Zero discrepancy between reporting and data layer. | Should | Candidate |
| **NFR-009** | ST-5 | Cost sustainability: run within free/low-cost tiers where practical. | Operates in free/low-cost tiers; cost documented. | Should | Candidate |

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

## Risk Register

| Risk ID | Description & Cause | Probability | Impact | Exposure / Priority | Mitigation Strategy | Owner | Status |
| :--- | :--- | :---: | :---: | :---: | :--- | :--- | :---: |
| **RSK-001** | **Unfamiliarity with technology stack:** Framework/database must be selected under tight schedule constraints. | Medium | High | High | Conduct small technical Proof-of-Concept spikes during M1/M2 before committing. | All team members | [x] Open<br>[ ] Mitigated<br>[ ] Realised |
| **RSK-002** | **Merge Conflicts & Baseline Desynchronisation:** Multiple members editing shared files without coordination. | High | Medium | High | Enforce strict 24-hour PR reviews, modularize document sections, require two peer approvals on GitHub. | All team members | [ ] Open<br>[x] Mitigated<br>[ ] Realised |
| **RSK-003** | **Scope Creep vs. Fixed Schedule:** Attempting complex custom features beyond baseline capabilities. | Medium | High | High | Strictly baseline core requirements in M1; enforce formal Change Request & Impact Analysis processes. | All team members | [x] Open<br>[ ] Mitigated<br>[ ] Realised |
| **RSK-004** | **Unverified AI Code/Content Generation:** AI generates subtle bugs, invalid logic, or wrong architectural assumptions. | Medium | High | High | Mandate independent human verification for every AI output; maintain logs in the AI Usage Register. | All team members | [ ] Open<br>[x] Mitigated<br>[ ] Realised |
| **RSK-005** | **Exposure of Secrets or Credentials:** API keys, database URLs, or passwords committed to GitHub. | Low | High | Medium | Maintain strict `.gitignore`, use environment variables, and run automated pre-commit secret scanners. | All team members | [ ] Open<br>[x] Mitigated<br>[ ] Realised |
| **RSK-006** | **Inconsistent Environment:** Code works locally but fails in Staging/Production due to config drift. | Medium | High | High | Containerize dependencies using Docker or enforce strict environment files early in M2/M3. | All team members | [x] Open<br>[ ] Mitigated<br>[ ] Realised |

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

---

## References

* Gong, Y. & Yan, J. & Soliman, X., 2020. *Towards a comprehensive understanding of digital transformation in government: Analysis of flexibility and enterprise architecture*. Government Information Quarterly, 37(3), p. 101487.
* Mergel, I., Edelmann, N. & Haug, N., 2019. *Defining digital transformation: Results from expert interviews*. Government Information Quarterly, 36(4), p. 1101385.