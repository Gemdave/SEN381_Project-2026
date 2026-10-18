# CivicConnect M3 Test Plan and Verification Evidence Record

**Course:** SEN381 | **Milestone:** 3 | **Version:** 2.0  
**Owner:** G. Enright (Dev-Ops & Security) | **Reviewers:** K. Marota, M. Malope  
**Linked artifacts:** PED v3.0 (CI and verification sections), RTM, `.github/workflows/ci.yml`, `Docs/CI_Documentation/CI_Quality_Gates.md`, Risk Register v3.0

> **Status.** This test plan selects, from the tests that exist, the cases that have a traceable engineering basis and maps each to a requirement, acceptance criterion, ASR or risk. Expected results are taken from the PED and ADRs, not from observed output. The Actual, Status, Evidence and Interpretation fields in section 9 are blank until real runs are recorded. Items marked **[confirm]** need checking against the code or PED.

> **Final (v1.0.0, 18/10/2026).** This is the M3 record, kept as written with its evidence links filled in. Since M3 the suite grew to 288 automated tests, all passing; FR-001 to FR-023 are built and tested; real sign-in replaced the development identity outside Development (CR-003); E2E-03 and PT-01 have results in section 9; and section 13 answers each open item.

---
## 1. Purpose and scope
This plan defines the focused verification suite for the M3 release candidate. It protects the behavior that is actually built and that matters most: controlled status transitions, single ownership under concurrency, role-based access, insert-only history and transactional integrity.  

**In scope (implemented core slice):** FR-007, FR-008, FR-009, FR-011, FR-012, FR-013, FR-014; ASR-01, ASR-02, ASR-03; NFR-003 (view of own request only), NFR-004, NFR-005.  

**Not verified in M3 (not implemented in `app/`):** FR-001 to FR-004 (request submission, categories, requester history), FR-006 attachments, management and administrator functions, reporting, and real authentication (CR-003). These have no verification evidence and must not be described as verified. *Final: all are now built and tested (section 13).*

---
## 2. Risk-based strategy
| Risk / ASR | What could fail | Tests aimed at it |
| :--- | :--- | :--- |
| RSK-010, ASR-01 (High) | Two staff claim the same request | UT-05, IT-01, E2E-02 |
| RSK-011, ASR-02 (High) | Access control gaps; another user's data or actions exposed | UT-03, UT-06, BB-04, BB-05, IT-05, IT-07 |
| RSK-009 (Medium) | A change committed without its history record | IT-04, UT-07 |
| ASR-03 | History edited or deleted after the fact | IT-02 |
| NFR-005 | Request owner inconsistent with status | IT-03, IT-01 |
| RSK-021 (High) | Malformed or out-of-range input reaches persistence | BB-01, BB-02 |
| RSK-012 (High) | Development identity treated as real authentication | Not tested at M3; final: `SignInTests` and `AppUserSignInTests` (CR-003) |
| RSK-022 (High) | Two implementation trees diverge | Tests and CI target `app/` only, declared canonical |

---
## 3. Coverage
| Requirement | Minimum | This plan | Cases |
| :--- | :--- | :--- | :--- |
| Unit/component | 5 | 7 | UT-01 to UT-07 |
| Black-box functional | 5 | 5 | BB-01 to BB-05 |
| API/integration | 3 | 7 | IT-01 to IT-07 |
| E2E/system | 2 | 2 | E2E-01, E2E-02 |
| Negative/failure/unauthorized | 3 | 13 | UT-01, UT-02, UT-03, UT-05, UT-06, BB-01, BB-02, BB-05, IT-02, IT-03, IT-05, IT-07, E2E-02 |
| High-priority requirement/risk | 5 | 15 | UT-03, UT-05, UT-06, BB-04, BB-05, IT-01 to IT-05, IT-07, E2E-01, E2E-02, BB-02, UT-04 |
| Black-box techniques | 2 | 4 | State transition (UT-01, BB-01), boundary value analysis (BB-02), equivalence partitioning (BB-03, BB-04), decision table of actor and action (BB-05) |
| Performance | 1 | 1 | PT-01 |

Legitimate overlap is used: for example UT-05 is a unit, negative and high-priority case, and E2E-02 is an E2E, negative and high-priority case. The count of distinct cases is **21** (7 unit, 5 black-box, 7 integration, 2 E2E). Unit-level and API-level theories expand into more executed rows; the counts above are distinct cases, not executed rows.

---
## 4. Test environment and data
| Item | Plan |
| :--- | :--- |
| Unit tests | Run in CI (Build and Test job); no external dependencies |
| API/integration/E2E | Run in CI against a PostgreSQL 17 service container (`CIVICCONNECT_TEST_DB`); locally against the `app/db` docker-compose database (see `app/db/TEST_DATABASE.md`) |
| Database lifecycle | The fixture deletes and recreates the test database from the real migrations at the start of each run; it must only ever point at a local database whose name ends in `_test` |
| Test data | Created per test by `TestData` (requester, staff, other staff, administrator, request, category) so tests do not depend on shared mutable data |
| Identity | Development stand-in selected with the `user` query value (CR-003); RBAC results do not cover real authentication. Final: real sign-in has its own tests (`SignInTests`) |
| Staging-only checks | E2E-03 (UI walkthrough) and PT-01 run against the staging deployment, outside the CI gate. Final: there was no separate staging; PT-01 ran locally and the live site was checked after go-live (`Test/Evidence/Hosted_Deployment_Check.md`) |
| Traceability | Each counted test carries its case ID, for example `[Trait("TestCase", "UT-05")]`, so CI results map back to this plan |

---
## 5. Test case catalogue
Technique key: **EP** equivalence partitioning, **BVA** boundary value analysis, **ST** state transition, **DT** decision table. Test names are `Class.Method` in `app/tests/`.

### 5.1 Unit/component
| ID | Test | Basis | Why selected | Technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| UT-01 | StatusTransitionTests (both theories) | FR-012; AC-012.1, AC-012.2 | The lifecycle rule underpins accountability | ST | Four allowed pairs (Received to Assigned to In Progress to Resolved to Closed); four refused pairs (Received to Closed, Received to Resolved, Closed to Assigned, Assigned to Received) | Allowed pairs permitted; every other pair refused |
| UT-02 | StatusTransitionServiceTests.An_invalid_transition_is_refused_and_nothing_is_recorded | FR-012; AC-012.2 | A refused move must leave no trace | Negative | Request in Received; move to Closed | Outcome NotAllowed; no history entry and no requester message |
| UT-03 | StatusTransitionServiceTests.Closing_needs_the_close_permission | FR-014; AC-014.2; ASR-02; RSK-011 | Permission must be checked before any write | Negative | Request in Resolved; policy denies Close | Outcome Forbidden; no history entry |
| UT-04 | StatusTransitionServiceTests.Resolving_needs_a_note | FR-014; AC-014.1 | A resolution must be explained | EP (note blank vs present) | Request in In Progress; move to Resolved with a whitespace-only note | Outcome NoteRequired; no history entry |
| UT-05 | AssignmentServiceTests.Zero_affected_rows_with_an_owner_is_a_conflict_and_writes_no_history | FR-011; AC-011.2; NFR-005; RSK-010 | A lost race must not create a second owner or false history | Negative | Update affects 0 rows; request already owned | Outcome AlreadyOwned with the existing owner; no history written |
| UT-06 | AssignmentServiceTests.The_policy_is_checked_before_anything_is_written | ASR-02; RSK-011 | Authorization must precede persistence | Negative | Policy denies Assign | Outcome Forbidden; nothing written |
| UT-07 | StatusTransitionServiceTests.A_valid_move_records_the_change_the_note_and_a_message_for_the_requester | FR-012; FR-013; FR-007; AC-012.1; RSK-009 | A valid move must record both the change and the explanation | State transition | Request in In Progress; move to Resolved with a note | Outcome Changed; history shows StatusChanged then NoteAdded; one message for the requester containing the note |

### 5.2 Black-box functional (through the HTTP API)
| ID | Test | Basis | Why selected | Technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| BB-01 | ApiTests.From_assigned_the_wrong_next_status_is_refused | FR-012; AC-012.2; RSK-021 | Verifies the transition rule as a client sees it, including malformed input | ST and EP | Assigned request; targets Received, Assigned, Closed and the invalid text "Monkey" | Received, Assigned and Closed return Conflict; "Monkey" returns BadRequest |
| BB-02 | ApiTests.A_note_must_be_between_1_and_2000_characters | FR-013; AC-013.1; RSK-021 | Limits are where length bugs hide | BVA | Note lengths 0, 1, 2000 and 2001 | 0 and 2001 return BadRequest; 1 and 2000 return OK |
| BB-03 | ApiTests.Resolving_without_a_note_is_refused_and_with_a_note_is_accepted | FR-014; AC-014.1 | Same business rule checked end to end | EP | Accept, move to In Progress, then resolve without and with a note | Without a note: BadRequest with code `note_required`; with a note: OK |
| BB-04 | ApiTests.The_queue_api_scopes_filters_and_refuses_non_staff | FR-008; FR-009; ASR-02; RSK-011 | Staff must only see unassigned and own requests | EP | Requests mine, theirs and open; filters `q` and `mine=true`; a requester calls the queue | Mine and open visible, theirs absent; `mine=true` returns only mine; requester gets Forbidden |
| BB-05 | ApiTests.Only_the_owner_can_change_status_and_requesters_cannot | ASR-02; NFR-006; RSK-011 | Role and ownership combinations decide the outcome | DT (actor x action) | Owned request; other staff changes status; requester changes status; requester accepts | All three return Forbidden |

### 5.3 API/integration (real PostgreSQL)
| ID | Test | Basis | Why selected | Technique | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| IT-01 | ConcurrentAcceptTests.Exactly_one_of_eight_simultaneous_accepts_succeeds (TC-011.2) | FR-011; AC-011.2; ASR-01; NFR-005; RSK-010 | The one-owner rule only means something under real concurrency | Negative / concurrency | Eight staff, each with its own database context, accept one request at once | Exactly one Assigned and seven AlreadyOwned; one owner stored; one history row |
| IT-02 | HistoryInsertOnlyTests.History_rows_cannot_be_changed_or_deleted | ASR-03 | History must survive even direct database access | Negative | Existing history row; raw UPDATE and DELETE | Both statements are refused; the note is unchanged |
| IT-03 | HistoryInsertOnlyTests.The_database_refuses_an_assigned_request_with_no_owner | NFR-005 | The database, not only the code, protects ownership | Negative | Raw update to Assigned with no owner | The database rejects the update |
| IT-04 | TransactionTests.A_failure_after_the_ownership_change_rolls_everything_back | RSK-009 | A half-saved change corrupts accountability | Failure injection | Ownership change and history insert, then a forced exception | Request still Received and unowned; no history rows |
| IT-05 | AuthorizationTests.A_requester_can_view_only_their_own_request | ASR-02; NFR-003; RSK-011 | Requester privacy is a Must | Negative | Requester and an unrelated requester; View policy | Owner may view; stranger may not |
| IT-06 | LifecycleTests.A_request_goes_from_received_to_closed_with_history_and_messages | FR-011; FR-012; FR-013; FR-007 | Core workflow across services and database | State transition | Accept, In Progress, Resolved (with note), Closed | Status Closed; five history rows; four requester messages |
| IT-07 | LifecycleTests.Only_the_owner_or_an_administrator_can_close | FR-014; AC-014.2; ASR-02 | Closing is the most consequential status change | DT (actor) | Resolved request; other staff then an administrator close it | Other staff Forbidden; administrator Changed |

### 5.4 E2E/system
| ID | Test | Basis | Why selected | Input / precondition | Expected result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| E2E-01 | ApiTests.E2E_a_request_is_accepted_worked_resolved_and_closed_through_the_api | FR-011; FR-012; FR-013; FR-014; FR-007 | Critical journey through the real application host and database | Accept, In Progress, Resolved with note, wrong staff closes, administrator closes | Correct status codes throughout; request Closed; five history rows; four messages |
| E2E-02 | ApiTests.E2E_eight_staff_accepting_at_once_produce_exactly_one_owner | FR-011; ASR-01; NFR-005; RSK-010 | The concurrency rule over HTTP, not just in code | Eight staff accept the same request concurrently | One OK and seven Conflict; one Assigned history row |
| E2E-03 (planned) | Manual walkthrough on staging | FR-008; FR-011; FR-012 | Shows the deployed release candidate works through the UI | Staff queue, request details, accept, change status, add note | Journey completes against the identified release candidate. Not counted toward the minimum until executed and recorded |

E2E-01 and E2E-02 run through the HTTP API of the real application; they do not drive the browser UI. This is a stated limitation.

---
## 6. Supporting tests (Not Counted)
These duplicate counted cases at a different layer or cover smaller rules, so they are not used to meet the minimum:  
NoteServiceTests (three cases), AssignmentServiceTests (success, not found), StatusTransitionServiceTests.Losing_the_race_is_reported_and_nothing_is_recorded, AuthorizationTests.A_requester_cannot_use_staff_actions, LifecycleTests.Skipping_a_step_is_refused_by_the_service, QueueTests.Staff_see_unassigned_and_their_own_requests_but_not_other_peoples.

---
## 7. Performance scenario
| Field | Detail |
| :--- | :--- |
| ID | PT-01 |
| Operation | Staff queue load (`GET /api/v1/queue`) |
| Basis and why | NFR-004: the queue loads within 3 seconds at 1,000 requests; it is the most-used, data-heavy read |
| CI guard (existing) | QueueTests.The_queue_loads_in_under_three_seconds_with_1000_requests: one warm repository call against 1,000 seeded requests. This is a regression guard, not the exercise: it excludes HTTP, serialization and network, and runs on a shared CI runner |
| Exercise | Seed 1,000 requests (`app/db/seed/seed_requests.sql`); against staging, 3 warm-up calls, then 20 sequential calls, then 5 concurrent users for 30 seconds; tool k6 or equivalent **[confirm]** |
| Measures | Median, 95th percentile and maximum response time; error rate |
| Target | 95th percentile at or below 3 seconds with zero errors (NFR-004) |
| Record | Environment, data size, workload, duration, cold-start effect |
| Does not prove | Municipal-scale load, real network variation, production hardware; free-tier hosting and synthetic data only |

---
## 8. How CI relates to this plan
| Cases | Run by | Gate effect |
| :--- | :--- | :--- |
| UT-01 to UT-07 | Build and Test job | A failure fails the Quality Gate |
| BB-01 to BB-05, IT-01 to IT-07, E2E-01, E2E-02 | Build and Test job with the PostgreSQL service | A failure fails the Quality Gate |
| Skipped tests | The "Verify no tests were skipped" step | Any skipped test fails the job, because a skipped test is not evidence |
| E2E-03, PT-01 | Staging, on demand | Not part of the gate; results recorded in section 9 |

---
## 9. Execution evidence log
**Run summary ([CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178), GitHub-hosted runner, 15/10/2026):** 44 of 44 executed cases passed, 0 failed, 0 skipped (Domain 8, Application 12, Integration 24). The integration cases ran against a PostgreSQL service container, so none were skipped. No coverage report was produced because `coverlet.collector` has not been added to the test projects yet.  
| ID | Run date | Commit / RC tag | Actual result | Status (Pass / Fail / Blocked) | Evidence link | Interpretation and limitation |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| UT-01 | 15/10/2026 | `main`, merged M3 build | 8 of 8 rows passed (4 allowed, 4 refused pairs) | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | The domain rule matches the baselined lifecycle for the pairs tested; it does not enumerate every possible pair. |
| UT-02 | 15/10/2026 | `main`, merged M3 build | Passed; outcome NotAllowed, no history or message written | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | A refused move leaves no trace at service level; the database layer is covered by IT cases. |
| UT-03 | 15/10/2026 | `main`, merged M3 build | Passed; outcome Forbidden, no history written | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Permission is checked before any write, using a fake policy; real policy is covered by BB-05 and IT-07. |
| UT-04 | 15/10/2026 | `main`, merged M3 build | Passed; outcome NoteRequired for a whitespace-only note | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Covers blank versus present note only; other invalid note content is covered by BB-02. |
| UT-05 | 15/10/2026 | `main`, merged M3 build | Passed; outcome AlreadyOwned with the existing owner, no history written | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Proves the service reports a lost race correctly; the real race is IT-01 and E2E-02. |
| UT-06 | 15/10/2026 | `main`, merged M3 build | Passed; outcome Forbidden, nothing written | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Authorization precedes persistence in AssignmentService; does not test other services. |
| UT-07 | 15/10/2026 | `main`, merged M3 build | Passed; StatusChanged then NoteAdded recorded, one requester message containing the note | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Uses in-memory fakes; the persisted result is checked in IT-06. |
| BB-01 | 15/10/2026 | `main`, merged M3 build | 4 of 4 rows passed (Received, Assigned, Closed refused with Conflict; "Banana" refused with BadRequest) | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Only the Assigned starting state was exercised; other starting states are covered at unit level (UT-01). |
| BB-02 | 15/10/2026 | `main`, merged M3 build | 4 of 4 rows passed (lengths 0 and 2001 refused; 1 and 2000 accepted) | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Boundaries confirmed for notes only; other input fields are not tested. Source of the 2000 limit still to be cited. |
| BB-03 | 15/10/2026 | `main`, merged M3 build | Passed; BadRequest with code note_required, then OK with a note | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Confirms the rule end to end through the API for the Resolved transition only. |
| BB-04 | 15/10/2026 | `main`, merged M3 build | Passed; own and open requests visible, others hidden, filters worked, requester received Forbidden | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Tests one scope and filter combination with generated data; not every filter or sort order. |
| BB-05 | 15/10/2026 | `main`, merged M3 build | Passed; other staff and requesters received Forbidden on status change and requester on accept | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Actor is chosen with the development identity stand-in, so this does not show real authentication works (RSK-012). |
| IT-01 | 15/10/2026 | `main`, merged M3 build | Passed; one Assigned, seven AlreadyOwned, one owner and one history row stored | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Real PostgreSQL race with eight contexts; one run does not prove the race can never be lost, but the conditional update design supports it. |
| IT-02 | 15/10/2026 | `main`, merged M3 build | Passed; UPDATE and DELETE on history refused, note unchanged | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Shows the database protects history for the connection used; a database superuser could still alter it (RSK-013). |
| IT-03 | 15/10/2026 | `main`, merged M3 build | Passed; database rejected an Assigned request with no owner | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Confirms the database constraint exists in the migrated schema. |
| IT-04 | 15/10/2026 | `main`, merged M3 build | Passed; after a forced failure the request stayed Received and unowned, no history rows | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Covers the assign-and-history path; other multi-step paths are not tested. |
| IT-05 | 15/10/2026 | `main`, merged M3 build | Passed; owner may view, stranger may not | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Tested at policy level, not through a requester-facing page, which is not implemented. |
| IT-06 | 15/10/2026 | `main`, merged M3 build | Passed; status Closed, five history rows, four requester messages | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Single happy-path lifecycle with generated data. |
| IT-07 | 15/10/2026 | `main`, merged M3 build | Passed; other staff Forbidden, administrator Changed | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Matches the implemented rule (owner or administrator); AC-014.2 wording still to be reconciled (open item 1). |
| E2E-01 | 15/10/2026 | `main`, merged M3 build | Passed; correct status codes throughout, request Closed, five history rows, four messages | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | Runs through the real application host and database over HTTP; does not drive the browser UI. |
| E2E-02 | 15/10/2026 | `main`, merged M3 build | Passed; one OK and seven Conflict, one Assigned history row | Pass | [CI run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178); test-results artifact (TRX) | The one-owner rule holds over HTTP in this run; the CI runner is a single machine with one database. |
| E2E-03 | 13/10/2026 | Release candidate | The UI journey (queue, details, accept, status change, note) passed within the 26 acceptance checks | Pass | `Test/Evidence/M3_Acceptance_Checks.md` | Local build with pages driven over HTTP; not on staging and not in a browser |
| PT-01 | 13/10/2026 | Release candidate | p95 0.064 s with 5 users, 0 errors, 1,000 requests | Pass | `Test/Evidence/PT-01_Results.md` | Local Release build, not staging; free-tier cold starts not measured |
 
Supporting tests (not counted) also passed: NoteServiceTests (3), AssignmentServiceTests success and not found, Losing_the_race, A_requester_cannot_use_staff_actions, skipping_a_step, Staff_see_unassigned_and_their_own_requests, and the CI timing guard for 1,000 requests.   
Every failure must be entered in the Defect Register with severity, status, fix and regression evidence. A corrected defect keeps its test in the suite as a regression test.

---
## 10. RTM extension
Add these columns to the existing RTM rows; do not rebuild the RTM.  
| Requirement | Acceptance criteria / basis | Test cases | Execution evidence |
| :--- | :--- | :--- | :--- |
| FR-007 | AC-007.1 | UT-07, IT-06, E2E-01 | Section 9 |
| FR-008 / FR-009 | AC-008.1, AC-009.1 | BB-04, E2E-03 | Section 9 |
| FR-011 | AC-011.1, AC-011.2 | UT-05, IT-01, IT-06, E2E-01, E2E-02 | Section 9 |
| FR-012 | AC-012.1, AC-012.2 | UT-01, UT-02, UT-07, BB-01, IT-06, E2E-01 | Section 9 |
| FR-013 | AC-013.1 | BB-02, UT-07, IT-06 | Section 9 |
| FR-014 | AC-014.1, AC-014.2 | UT-03, UT-04, BB-03, IT-07 | Section 9 |
| ASR-01 | one owner under concurrency | UT-05, IT-01, E2E-02 | Section 9 |
| ASR-02 | authorization | UT-03, UT-06, BB-04, BB-05, IT-05, IT-07 | Section 9 |
| ASR-03 | insert-only history | IT-02 | Section 9 |
| NFR-003 | requester sees only own request | IT-05 | Section 9 |
| NFR-004 | queue within 3 s at 1,000 requests | PT-01 | Section 9 |
| NFR-005 | one owner consistent with status | IT-01, IT-03, E2E-02 | Section 9 |

---
## 11. What this plan does not prove
- It does not show the absence of defects; it shows the selected behaviors work under the chosen data.
- Coverage percentage, if reported, shows which lines ran, not that the assertions are right.
- Development identity is used for access control (the `user` query value); passing RBAC tests says nothing about real authentication (RSK-012). Final: real sign-in is now tested (`SignInTests`).
- E2E-01 and E2E-02 exercise the API, not the browser UI.
- CI performance numbers come from a shared runner and are only a regression guard; the staging exercise is the performance evidence and does not predict municipal-scale load.
- Requester-facing requirements (FR-001 to FR-004) are not implemented, so they have no verification evidence. Final: built and tested at v1.0.0.

---
## 12. Open items to confirm before execution
1. Whether AC-014.2 ("staff without close permission cannot close") matches the implemented rule (owner or administrator may close); if not, record the interpretation as a controlled change.
2. Whether backward and same-state transitions are meant to be invalid (UT-01 treats them as refused).
3. Where the 1 to 2000 character note limit (BB-02) is specified (API contract or PED); cite it in BB-02 and the RTM.
4. Which load tool will be used for PT-01, and the staging URL.
5. Whether the team will add a scripted UI test or record E2E-03 as a documented manual run.
6. That every counted test carries its `[Trait("TestCase", "...")]` ID.

---
## 13. Final status (v1.0.0)
| Item | Final position |
| :--- | :--- |
| Suite | 288 automated tests (89 domain, 92 application, 107 integration), all passing on 18/10/2026; the 21 counted cases above are unchanged |
| Scope verified | FR-001 to FR-023, NFR-001 to NFR-009 and real sign-in (`SignInTests`, `AppUserSignInTests`, `Pbkdf2PasswordHasherTests`); traced in RTM v4.0 |
| CI | Every test project runs against PostgreSQL; the Quality Gate is a required check on `main` with 2 approvals ([PR #38](https://github.com/Gemdave/SEN381_Project-2026/pull/38)); a gate failure was fixed before merge ([failed run](https://github.com/Gemdave/SEN381_Project-2026/actions/runs/39142781118)) |
| Production checks | Simulated production 13 of 13 (`Test/Evidence/Final_Simulated_Production.md`); live site checks (`Test/Evidence/Hosted_Deployment_Check.md`) |
| NFR-001 | Timed first-user walkthrough met the target (recorded in PED v4.0, M1 Scope to Final Delivery) |
| Open item 1 (AC-014.2) | Settled by CR-012: the owner in scope or an administrator closes, as IT-07 tests |
| Open item 2 (backward and same-state moves) | Settled by the implemented behaviour: refused, as UT-01 tests (RSK-024) |
| Open item 3 (note limit) | Specified in `Docs/API/API_Contract_v1.md` (1 to 2000 characters) and enforced by `NoteService.MaxLength` |
| Open item 4 (load tool, staging URL) | A curl script, `Test/Performance/PT-01_queue.sh`, run locally; the live site is https://sen381-project-group-i-2026.onrender.com/ |
| Open item 5 (E2E-03) | Recorded as manual checks within the acceptance checks; no scripted browser test |
| Open item 6 (test case traits) | Partly done: 10 of the 21 counted cases carry a `TestCase` trait; the rest are named by class and method in section 5 |