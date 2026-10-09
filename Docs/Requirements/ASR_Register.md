## Architecturally Significant Requirements (ASRs) and Quality Drivers

**Owner:** Mogau Malope (Requirements Lead & Frontend / UX Engineer)
**Status:** Proposed for the M2 baseline; updated after the M2 design review (CR-011)
**Referenced by:** PED v3.0 (ASR section), RTM (ASR column), ADR-001 to ADR-007, Risk Register

A requirement is treated as architecturally significant only where changing it would change an architecture, persistence, technology or design decision, not merely the code inside a component. Each driver below is supported by evidence already in the baseline and is stated as something that can be measured or tested.

**After the M2 review.** The architecture moved to microservices (CR-010). CR-011 restated ASR-05 and added ASR-07. The other expectations are unchanged; only their consequences were rewritten for services. Earlier wording is kept under "Superseded wording".

### ASR register

| ASR | Quality driver | Source and evidence | Measurable expectation |
| :--- | :--- | :--- | :--- |
| **ASR-01** | Ownership and status correctness while staff act at the same time | NFR-005; FR-011 (AC-011.2); FR-012 (AC-012.1, AC-012.2); ST-2; conflict C-1; RSK-009, RSK-010 | Of any number of simultaneous accept attempts on one unassigned request, exactly one succeeds and the rest are refused with a clear message. No status change is lost. Invalid transitions such as Received to Closed are refused. |
| **ASR-02** | Authorisation and requester privacy | NFR-003; NFR-006; FR-008 (AC-008.2); FR-014 (AC-014.2); FR-020; ST-4; D-004 closed by CR-003; RSK-011, RSK-012 | Every read and write of a request is authorised on the server against the caller's role and ownership. A requester who asks for another requester's reference is refused rather than shown data. Staff without close permission cannot close a request. |
| **ASR-03** | Auditability and tamper evidence | NFR-007; FR-013 (AC-013.1); FR-022 (AC-022.1); ST-3, ST-4; RSK-013 | Every assignment, status change, note, role change and category change is recorded with actor and timestamp. History cannot be edited or deleted through the application. Corrections are new entries. |
| **ASR-04** | Oversight responsiveness and reporting that reconciles | NFR-004; NFR-008; FR-009; FR-015 to FR-018; ST-2, ST-3; RSK-014 | The staff queue and the oversight views return within 3 seconds at a seeded volume of 1,000 requests, measured on the deployed environment. Displayed and exported figures match the underlying records exactly. |
| **ASR-05** *(restated, CR-011)* | Cost and complexity the team can sustain while staying ready to grow | NFR-009; ST-5; conflicts C-4, C-5; RSK-003, RSK-014, RSK-016; M1 constraints on schedule, compatibility and learning curve | Each service and its data store runs on free or low-cost plans, with its expected monthly cost recorded. There are no more services than the current requirements need. One team member can run the whole system locally from the documented steps. |
| **ASR-06** | Requester self service and timely feedback | NFR-001; NFR-002; FR-001; FR-003 (AC-003.2); FR-007 (AC-007.1, AC-007.2); ST-1; conflict C-1 | A first time requester submits a valid request in 5 steps or fewer and inside 5 minutes without training. A status change made by staff is visible to the requester within 15 seconds, in the application, with a reason given on rejection. |
| **ASR-07** *(new, CR-011)* | Growth readiness | NFR-010; ST-5; conflict C-5; CR-010; M2 design review | The three NFR-010 checks pass: a second copy of a service changes no behaviour; no service touches another service's schema in the shared PostgreSQL instance; a new service needs no schema change elsewhere. No volume target until growth figures exist. |

### How each driver shapes the service split

| ASR | Consequence for architecture, data, technology and design | Decision record |
| :--- | :--- | :--- |
| **ASR-01** | Assignment, status and the single-owner check stay inside one service with one data store, so the conditional update never waits on another service. Other services learn of a change only after it commits. | ADR-001 (rev.), ADR-002 |
| **ASR-02** | Each service checks identity and role for its own data and never relies on a caller having checked. Requester privacy is enforced by the service that owns request data, and role rules keep one source of truth. | ADR-005, ADR-001 (rev.) |
| **ASR-03** | An action and its audit record succeed or fail together. Audit is append-only, and if audit is later gathered centrally, a lost or late message must not lose an entry. | ADR-002 (rev.) |
| **ASR-04** | Queue and oversight reads stay within 3 s. Reporting reads request data through the Requests service and keeps no copy, so NFR-008 stands as written (ADR-001). | ADR-001 (rev.), ADR-002, ADR-006 |
| **ASR-05** | Build only the services needed now, on one stack, with free or managed hosting and shared build and run steps. | ADR-001 (rev.), ADR-003, ADR-007 |
| **ASR-06** | One short guided flow with per-field validation and read-only status. The 15 s check covers the whole path across services; ADR-003 records polling or push. | ADR-004, ADR-003 |
| **ASR-07** | Services hold no in-memory state a second copy would need, own their schema in the shared instance, and are split only along boundaries the current requirements show. | ADR-001 (rev.), ADR-002, ADR-006, ADR-007 |

### Superseded wording (first M2 pass, replaced by CR-011)

* **ASR-05 driver:** "Cost and complexity that the team can sustain".
* **ASR-05 expectation:** "The solution runs within free or low cost hosting and database plans, with expected monthly cost written down. The team can build, run and deploy it inside the remaining schedule using skills it already has."
* **ASR-05 consequence:** "Favours one deployable unit over a distributed design, one managed database, and a stack the team already knows, on free plans. Also supports keeping email and text notification out of scope."
* **ASR-04 consequence:** "Oversight figures are derived from the same tables as the request records, so reports cannot drift from the data. Requires indexing and pagination on the queue and oversight queries. It does not justify a separate reporting database."

### Considered but not treated as ASRs

| Quality attribute | Why not now | What would make it one |
| :--- | :--- | :--- |
| Failure isolation / high availability | No stakeholder has asked for it; NFR-009 limits hosting spend; RSK-015 already records the backup gap. | The Sponsor asks for higher availability, or an outage in one service stops request submission. |
| Independent deployability | A team of three releases together. | Releases in one area start being held back by another. |
| Fixed volume or throughput target | No growth figures exist; an invented figure is not evidence. | The Sponsor supplies figures; a CR adds a target to NFR-010 and ASR-07. |

### Targets still to confirm

| Item | Affects | Needed for | Route |
| :--- | :--- | :--- | :--- |
| Feedback latency value, recorded as Z seconds in the M1 RTM | NFR-002 | ASR-06 verification | CR-001 |
| Oversight performance target and data volume | NFR-004 | ASR-04 verification | CR-002 |
| Authentication mechanism | NFR-006, FR-020, D-004 | ASR-02 | CR-003, closed through ADR-005 |
| Reassignment rules for an owned request | FR-011 (AC-011.2) | ASR-01 | CR-004 |
| Definition of overdue, needing a target response field | FR-016 (AC-016.2) | ASR-04 | CR-005 |
| Reporting export in scope or deferred | FR-018 (AC-018.2) | ASR-04 | CR-006 |
| Allowed reporting lag | NFR-008 | ASR-04 | Closed: not needed, reporting keeps no copy (ADR-001) |
| Growth figures | NFR-010 | ASR-07 | None yet; a CR adds them if the Sponsor supplies them |

Each ASR is carried into the RTM through the ASR column against the requirement IDs listed above, and each decision record names the driver that caused it. The M2 demonstration trace is FR-011 with ASR-01; it is rebuilt against the services once the revised ADR-001 names them.
