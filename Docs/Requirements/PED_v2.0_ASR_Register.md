## Architecturally Significant Requirements (ASRs) and Quality Drivers

**Owner:** Mogau Malope (Requirements Lead & Frontend / UX Engineer)
**Status:** Proposed for the M2 baseline
**Referenced by:** RTM v2.0 (ASR column), ADR-001 to ADR-007, Risk Register v2.0

A requirement is treated as architecturally significant only where changing it would change an architecture, persistence, technology or design decision, not merely the code inside a component. Each driver below is supported by evidence already in the baseline, either a stakeholder profile, a stakeholder conflict or a risk, and each is stated as something that can be measured or tested. Quality attributes that do not meet that test are listed in the exclusions table, so that the architecture is driven by project evidence rather than by a list of every attribute taught.

### ASR register

| ASR | Quality driver | Source and evidence | Measurable expectation |
| :--- | :--- | :--- | :--- |
| **ASR-01** | Ownership and status correctness while staff act at the same time | NFR-005; FR-011 (AC-011.2); FR-012 (AC-012.1, AC-012.2); ST-2; conflict C-1; RSK-009, RSK-010 | Of any number of simultaneous accept attempts on one unassigned request, exactly one succeeds and the rest are refused with a clear message. No status change is lost. Invalid transitions such as Received to Closed are refused. |
| **ASR-02** | Authorisation and requester privacy | NFR-003; NFR-006; FR-008 (AC-008.2); FR-014 (AC-014.2); FR-020; ST-4; D-004 closed by CR-003; RSK-011, RSK-012 | Every read and write of a request is authorised on the server against the caller's role and ownership. A requester who asks for another requester's reference is refused rather than shown data. Staff without close permission cannot close a request. |
| **ASR-03** | Auditability and tamper evidence | NFR-007; FR-013 (AC-013.1); FR-022 (AC-022.1); ST-3, ST-4; RSK-013 | Every assignment, status change, note, role change and category change is recorded with actor and timestamp. History cannot be edited or deleted through the application. Corrections are new entries. |
| **ASR-04** | Oversight responsiveness and reporting that reconciles | NFR-004; NFR-008; FR-009; FR-015 to FR-018; ST-2, ST-3; RSK-014 | The staff queue and the oversight views return within 3 seconds at a seeded volume of 1,000 requests, measured on the deployed environment. Displayed and exported figures match the underlying records exactly. |
| **ASR-05** | Cost and complexity that the team can sustain | NFR-009; ST-5; conflict C-4; RSK-003, RSK-014, RSK-016; M1 constraints on schedule, compatibility and learning curve | The solution runs within free or low cost hosting and database plans, with expected monthly cost written down. The team can build, run and deploy it inside the remaining schedule using skills it already has. |
| **ASR-06** | Requester self service and timely feedback | NFR-001; NFR-002; FR-001; FR-003 (AC-003.2); FR-007 (AC-007.1, AC-007.2); ST-1; conflict C-1 | A first time requester submits a valid request in 5 steps or fewer and inside 5 minutes without training. A status change made by staff is visible to the requester within 15 seconds, in the application, with a reason given on rejection. |

### How each driver shaped the M2 decisions

| ASR | Consequence for architecture, data, technology and design | Decision record |
| :--- | :--- | :--- |
| **ASR-01** | Assignment and transition rules sit behind the backend boundary, not in the interface. The store must support a transaction and a conditional update that fails safely when another actor already owns the request. The lifecycle needs an enforced set of valid transitions rather than a free text status. | ADR-001, ADR-002 |
| **ASR-02** | Every request scoped query is filtered by the caller's identity and role on the server, so the interface can never be the enforcement point. Requires one authorisation decision point and a role and permission data model. | ADR-005, ADR-001 |
| **ASR-03** | The data model needs an append only history table beside the request record, holding actor, action, old value, new value and time. Rules out destructive updates and row deletion for corrections. | ADR-002 |
| **ASR-04** | Oversight figures are derived from the same tables as the request records, so reports cannot drift from the data. Requires indexing and pagination on the queue and oversight queries. It does not justify a separate reporting database. | ADR-001, ADR-006 |
| **ASR-05** | Favours one deployable unit over a distributed design, one managed database, and a stack the team already knows, on free plans. Also supports keeping email and text notification out of scope. | ADR-001, ADR-003, ADR-007 |
| **ASR-06** | The requester journey is a short guided flow with validation messages for each field and a read only status display. The 15 second expectation is met by polling rather than push infrastructure, which is a deliberate simplification. | ADR-004, ADR-003 |


### Targets still to confirm

| Item | Affects | Needed for | Route |
| :--- | :--- | :--- | :--- |
| Feedback latency value, recorded as Z seconds in the M1 RTM | NFR-002 | ASR-06 verification | CR-001 |
| Oversight performance target and data volume | NFR-004 | ASR-04 verification | CR-002 |
| Authentication mechanism | NFR-006, FR-020, D-004 | ASR-02 | CR-003, closed through ADR-005 |
| Reassignment rules for an owned request | FR-011 (AC-011.2) | ASR-01 | CR-004 |
| Definition of overdue, needing a target response field | FR-016 (AC-016.2) | ASR-04 | CR-005 |
| Reporting export in scope or deferred | FR-018 (AC-018.2) | ASR-04 | CR-006 |

Each ASR is carried into the RTM through the ASR column against the requirement IDs listed above, and each decision record names the driver that caused it. The trace chosen for the M2 demonstration is FR-011 with ASR-01, running from requirement through architecture responsibility, persistence decision, interface decision, implementation and initial verification.
