# CivicConnect M3 Test Plan and Verification Evidence Record
**Course:** SEN381 | **Milestone:** 3 | **Version:** 1  
> **Status of this document.** Expected results below are derived from the PED acceptance criteria and risk register *before* any execution. The Actual, Status, Evidence and Interpretation fields in section 8 are intentionally blank and must be filled from real runs. Items marked **[confirm]** depend on the implementation and must be checked against the code before execution.

---
## 1. Purpose and scope
This plan defines the focused verification suite for the CivicConnect M3 release candidate. It protects the behavior that matters most: request submission, controlled status transitions, ownership integrity, role-based access and requester privacy. The goal is evidence of engineering value and traceability, not a high test count.  
**In scope (core M3 slice):** FR-001, FR-002, FR-004, FR-007, FR-008, FR-011, FR-012, FR-014, FR-020; NFR-003, NFR-004, NFR-005, NFR-006.  
**Out of scope for M3 testing:** FR-006 attachments (Could), FR-018/FR-023 reporting depth, FR-022 audit trail UI, real authentication mechanism (CR-003, development identity is used), production-scale load, accessibility and browser-compatibility testing.

---
## 2. Risk-based strategy
Tests were chosen by risk and priority, not by technique. Each high-rated risk has at least one test aimed at the failure it describes.  
| Risk | What could fail | Tests aimed at it |
| :--- | :--- | :--- |
| RSK-010 (H) | Two staff claim the same request | UT-03, IT-02 |
| RSK-011 (H) | Role check without ownership/scope check; another user's data exposed | UT-04, BB-03, BB-05, IT-03 |
| RSK-021 (H) | Malformed or overlong input reaches persistence | BB-01, BB-02, UT-02 |
| RSK-009 (M) | State change committed without history record | IT-04 (stretch) |
| RSK-012 (H) | Development identity treated as real authentication | Not tested; recorded as a residual risk |
| RSK-022 (H) | Two implementation trees diverge | Tests target the authoritative tree only (see section 9) |

---
## 3. Coverage against the M3 minimum
| M3 requirement | Minimum | This plan | Cases |
| :--- | :--- | :--- | :--- |
| Unit/component | 5 | 5 | UT-01 to UT-05 |
| Black-box functional | 5 | 5 | BB-01 to BB-05 |
| API/integration | 3 | 3 (+1 stretch) | IT-01 to IT-03 (IT-04) |
| E2E/system | 2 | 2 | E2E-01, E2E-02 |
| Negative/failure/unauthorized | 3 | 8 | UT-01, UT-03, UT-04, BB-03, BB-04, BB-05, IT-02, IT-03 |
| High-priority requirement/risk | 5 | 13 | All except UT-02 and UT-05 (supporting) |
| Black-box techniques | 2 | 4 | Equivalence partitioning, boundary value analysis, decision table, state transition |
| Performance | 1 | 1 | PT-01 |

Total: 15 distinct functional/automated cases plus one performance scenario. No case is counted twice toward the minimum.

---
## 4. Test environment and data
| Item | Plan |
| :--- | :--- |
| Unit tests | Run in CI (Build and Test job), no external dependencies |
| API/integration | Run in CI against a PostgreSQL service container; connection string supplied through `CIVICCONNECT_TEST_DB` |
| E2E | Run against the staging environment (Render + Neon) or locally; screenshots and run output stored as evidence |
| Performance | Run against staging with a seeded dataset, outside the CI gate |
| Naming for traceability | Each automated test carries its case ID, e.g. `[Trait("TestCase","UT-01")]`, so CI results map back to this plan |

### Seed data (controlled, recreated before each run):
| Actor | Details |
| :--- | :--- |
| Requester A, Requester B | Each owns 2 requests |
| Staff 1 | Scoped to category "Fault"; has close permission |
| Staff 2 | Scoped to category "Fault"; no close permission |
| Staff 3 | Scoped to category "IT" only |
| Manager, Administrator | One account each |
| Requests | At least 3 in "Fault" and 2 in "IT" across the accounts above |

---
## 5. Test case catalogue
Technique key: **EP** equivalence partitioning, **BVA** boundary value analysis, **DT** decision table, **ST** state transition.

### 5.1 Unit/component
| ID | Test basis | Why selected | Level / technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| UT-01 | FR-012; AC-012.1, AC-012.2 | Lifecycle integrity underpins accountability; invalid transitions corrupt records | Unit / ST | Every (from, to) pair across Received, Assigned, In Progress, Resolved, Closed | Only forward adjacent steps are allowed; Received to Closed, backward and same-state moves are rejected with a message **[confirm backward/same-state reading]** |
| UT-02 | FR-002; AC-002.1, AC-002.2 | Category must be controlled, not free text | Unit / EP | Valid category from the list; a string not in the list; empty; null | Valid accepted; unknown, empty and null rejected, category flagged |
| UT-03 | FR-011; AC-011.1, AC-011.2; NFR-005; RSK-010 | Prevents silent double ownership | Unit / business rule | (a) Unassigned request, Staff 1 accepts; (b) request owned by Staff 1, Staff 2 attempts to accept | (a) Owner and timestamp recorded, status Assigned; (b) blocked or controlled reassignment, owner stays Staff 1 |
| UT-04 | FR-014; AC-014.2; FR-020; AC-020.2; NFR-006; AC-003.2 | Permission rules are the core RBAC guarantee | Unit / DT | Role and action combinations: requester closes; staff without close permission closes; staff with close permission closes; non-admin changes a role; admin changes a role | Denied, denied, allowed, denied, allowed respectively; denied cases leave state unchanged |
| UT-05 | FR-001; AC-001.1 | Every request must be uniquely identifiable and start in the right state | Unit / business rule | Two valid complete requests created in sequence | Each has a distinct non-empty reference ID, status Received, creation timestamp and the creating requester as owner |

### 5.2 Black-box functional
| ID | Test basis | Why selected | Level / technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| BB-01 | FR-001; AC-001.2; RSK-021 | Incomplete requests must never be stored | Functional / EP | Partitions: all fields valid; each required field missing one at a time (title, description, category, location **[confirm list]**); all missing | Valid case saved; every other case not saved, request count unchanged, each missing field flagged |
| BB-02 | FR-001; RSK-021 | Limits are where length bugs and unhandled errors hide | Functional / BVA | Title and description at 0, 1, limit-1, limit and limit+1 characters (limit **[confirm from validation rule]**) | Within limit accepted; over limit rejected with a clear message; no server error response in any case |
| BB-03 | FR-008; AC-008.1, AC-008.2; RSK-011 | Staff must only see requests they are authorized for | Functional / EP | Staff 1 (scope Fault) opens queue; then requests an IT request directly by its ID | Queue lists only Fault requests; direct access to the IT request is denied and returns no request data |
| BB-04 | FR-012; AC-012.1, AC-012.2 | Verifies the transition rule as a user experiences it | Functional / ST | Staff 1 on a Received request: attempt Received to Closed; then advance one step at a time to Closed | Invalid jump blocked with a message and status unchanged; each valid step accepted and recorded with actor and timestamp |
| BB-05 | FR-004; AC-004.2; NFR-003; RSK-011 | Requester privacy is a Must and a High risk | Functional / negative | Requester A opens own history; then requests Requester B's request by ID | History shows only A's two requests; B's request is denied or not found and no B data is returned |

### 5.3 API/integration
| ID | Test basis | Why selected | Level / technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| IT-01 | FR-001; AC-001.1 | Confirms API and database cooperate on the core write path | API integration | Authenticated requester submits a valid request through the create endpoint **[confirm endpoint]** | Success response containing a reference ID; the row exists in PostgreSQL with status Received and the correct requester |
| IT-02 | FR-011; NFR-005; RSK-010 | The concurrency rule only means something against the real database | API integration / negative | Two staff accept the same unassigned request at the same moment | Exactly one succeeds; the other receives a conflict/denied response; the database shows a single owner |
| IT-03 | FR-014; AC-014.2; NFR-006; RSK-011 | Access control must hold at the API boundary, not only in the UI | API integration / negative | (a) No credentials; (b) requester credentials; (c) staff without close permission; each calls the close/status endpoint | Each denied with the status code defined by the API contract **[confirm 401/403]**; request status unchanged in the database |
| IT-04 (stretch) | FR-012; FR-013; RSK-009 | State change and history record must commit together | API integration / failure | Force the history write to fail during a status change (fault injection or constraint violation) | The status change is rolled back; no state without history. Drop this case if fault injection is not practical |

### 5.4 E2E/system
| ID | Test basis | Why selected | Level / technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| E2E-01 | FR-001; FR-003; FR-004; AC-001.1, AC-003.1, AC-004.1 | Core requester journey across UI, API and database | E2E | Requester signs in, submits a valid request, opens history | Confirmation with reference ID shown; request appears in own history with status Received in plain language |
| E2E-02 | FR-011; FR-012; FR-014; FR-007; AC-007.1, AC-011.1, AC-012.1, AC-014.1 | Critical lifecycle across both roles | E2E | Requester submits; Staff 1 accepts, moves to In Progress, then Resolved; requester revisits | Each status reflected for the requester; resolution recorded; a feedback item with timestamp appears for the requester |

---
## 6. Performance scenario
| Field | Detail |
| :--- | :--- |
| ID | PT-01 |
| Basis | NFR-004: staff queue loads within 3 seconds for 1,000 requests |
| Why selected | The queue is the most-used, data-heavy read; the NFR gives an explicit target |
| Setup | Seed 1,000 requests; Staff 1 authenticated; staging environment; tool: k6 or equivalent |
| Workload | 3 warm-up requests; then 20 sequential requests; then 5 concurrent virtual users for 30 seconds |
| Measures | Median, 95th percentile and maximum response time; error rate |
| Expected result | 95th percentile at or below 3 seconds; zero errors |
| Interpretation limits | Free-tier hosting, one database, no network variation, synthetic data; shows the target is met at this scale, not at municipal scale. Record environment details and any cold-start effect with the result |

---
## 7. How CI relates to this plan
| Cases | Run by | Gate effect |
| :--- | :--- | :--- |
| UT-01 to UT-05 | Build and Test job | A failure fails the Quality Gate |
| IT-01 to IT-03 (IT-04) | Build and Test job with PostgreSQL service | A failure fails the Quality Gate |
| BB-01 to BB-05 | Automated at API/component level where practical; otherwise executed against staging and recorded manually | Automated ones gate; manual ones recorded as evidence |
| E2E-01, E2E-02 | Staging, via scripted run or documented manual run with screenshots | Not part of the gate; results recorded in  | PT-01 | Staging, on demand | Not part of the gate; results recorded in |

>A test that is skipped because its database or environment is missing is **not** evidence and must be reported as blocked, not passed.

---
## 8. Execution evidence log (to be completed after real runs)
| ID | Run date | Commit / RC tag | Actual result | Status (Pass / Fail / Blocked) | Evidence link | Interpretation and limitation |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| UT-01 | | | | | | |
| UT-02 | | | | | | |
| UT-03 | | | | | | |
| UT-04 | | | | | | |
| UT-05 | | | | | | |
| BB-01 | | | | | | |
| BB-02 | | | | | | |
| BB-03 | | | | | | |
| BB-04 | | | | | | |
| BB-05 | | | | | | |
| IT-01 | | | | | | |
| IT-02 | | | | | | |
| IT-03 | | | | | | |
| IT-04 | | | | | | |
| E2E-01 | | | | | | |
| E2E-02 | | | | | | |
| PT-01 | | | | | | |

Every failure must be entered in the Defect Register with severity, status, fix and regression evidence. A corrected defect keeps its test in the suite as a regression test.

---
## 9. RTM extension
Add these columns to the existing RTM rows for the requirements below; do not rebuild the RTM.  
| Requirement | Acceptance criteria | Test cases |  Defect / status |
| :--- | :--- | :--- | :--- |
| FR-001 | AC-001.1, AC-001.2 | UT-05, BB-01, BB-02, IT-01, E2E-01 |  |
| FR-002 | AC-002.1, AC-002.2 | UT-02 |  |
| FR-003 / FR-004 | AC-003.1, AC-004.1, AC-004.2 | BB-05, E2E-01 |  |
| FR-007 | AC-007.1 | E2E-02 |  |
| FR-008 | AC-008.1, AC-008.2 | BB-03 |  |
| FR-011 | AC-011.1, AC-011.2 | UT-03, IT-02, E2E-02 |  |
| FR-012 | AC-012.1, AC-012.2 | UT-01, BB-04, E2E-02 |  |
| FR-014 | AC-014.1, AC-014.2 | UT-04, IT-03, E2E-02 |  |
| FR-020 | AC-020.2 | UT-04 |  |
| NFR-003 | n/a | BB-05 |  |
| NFR-004 | n/a | PT-01 |  |
| NFR-005 | n/a | UT-03, IT-02 |  |
| NFR-006 | n/a | UT-04, IT-03 |  |

---
## 10. What this plan does not prove
- It does not show the absence of defects; it shows the selected behaviors work under the chosen data.
- Coverage percentage, if reported, shows which lines ran, not that the assertions are right.
- Development identity is used for access control; passing RBAC tests says nothing about real authentication (RSK-012).
- Staging performance on free-tier hosting does not predict municipal-scale load.
- Out-of-scope requirements (section 1) have no verification evidence and must not be described as verified.

---
## 11. Open items to confirm before execution
1. Which implementation tree is authoritative (`Backend/` or `app/`, RSK-022); all tests target that one.
2. The exact required-field list and field length limits for BB-01 and BB-02.
3. API endpoints and the 401/403 convention for IT-01 and IT-03.
4. Whether backward and same-state transitions are invalid (UT-01); if not, update the AC wording or log a change record.
5. Whether managers may close or change status; the ACs do not say, so no case covers it.
6. Whether E2E will be scripted (for example Playwright) or documented manual runs.
7. That the CI workflow provisions PostgreSQL and passes `CIVICCONNECT_TEST_DB` (open from RSK-020).