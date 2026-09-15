# Assignment 2
> Group I  
> 2026  
> Participating Members:  
> * Gerald Enright 577830  
> * Keletso Marota 601632  
> * Mogau Malope 600192  
---
## Task 1
### Design Problem 

### Alternatives For Each problem

### Recommendation feeding M2

---
## Task 2
### Persistence and Data-Integrity Decisions

Using PostgreSQL, the team's selected database, this part investigates how CivicConnect should maintain data accuracy when a step fails or two persons act simultaneously.

### 2.1 Chosen operation: assigning a request

When a staff member accepts a request, five things are saved together: the owner (FR-011), the status change from Received to Assigned (FR-012), a history entry (FR-013), an audit record (NFR-007) and a message for the requester (FR-007). Three things can go wrong:

- **Partial save:** the status changes but the history entry is lost (NFR-007).
- **Two staff at once:** both assign the same request and the last save wins (NFR-005).
- **Rules bypassed:** a script saves an invalid change, e.g. Received → Closed (FR-012).

### 2.2 Research into correctness mechanisms

### Transactions

The software must determine which of the multiple changes made during a transaction belong inside it. To ensure that nothing is half-saved and that any failure causes ROLLBACK, the status/owner check and all five saves should share a single, brief PostgreSQL transaction. The user's decision-making time and queue loading remain outside of the transaction. CivicConnect has a single database and backend, therefore one local transaction is sufficient (D-001, D-002).

### Where rules are checked

**Who checks what**

| Rule | UI | Backend | PostgreSQL |
|---|---|---|---|
| Only staff may assign | Hide button | **Main check** | – |
| Status change is valid | Show valid actions | **Main check** | CHECK on status values |
| Request not already owned | Show owner | Clear message | Conditional UPDATE |
| History cannot be changed | – | No edit feature | INSERT/SELECT rights only |

It is dangerous to have just one layer check the rules. Bypassing the user interface is possible, incorrect data from scripts cannot be prevented by the backend alone, and rules stored just in the database are difficult to test and may result in ambiguous errors. As a result, each layer has a specific function: PostgreSQL safeguards the stored data and prevents incorrect entries, the backend verifies the business rules, and the user interface helps users submit accurate information.

**Concurrency**

Under PostgreSQL's default Read Committed level, two staff can both read a request as unassigned and both save, so one update is silently lost (PostgreSQL Global Development Group, 2026a).

**Ways to prevent a lost update**

| Option | How it works | Trade-off |
|---|---|---|
Conditional UPDATE | e.g. Update WHERE assigned_staff_id IS NULL; 0 rows changed means someone was first | Simple, no locks. Rule repeated in SQL
Version number | Save only if the row's version is unchanged | Works for any edit. User must retry
SELECT … FOR UPDATE | Lock the row before checking | No clash; others wait; deadlock risk
Serializable | PostgreSQL cancels one clashing transaction | No manual check; backend must retry


**Caching**

Cache needed to determine an assignment should never be stored because it may speed up the staff queue (NFR-004) but may display outdated data (Microsoft, 2025). M2 doesn't require a cache; instead, indexes should be tried first, and caching should only be reviewed if NFR-004 is not satisfied. As long as the cache is emptied each time an administrator makes changes, the category list is the safest later option because it rarely changes.

### 2.3 Alternative approaches compared

**A:** the backend checks rules and saves each change separately. 

**B:** a PostgreSQL stored procedure does everything. 

**C:** layered: backend rules run in one transaction, with PostgreSQL constraints and a conditional UPDATE as safety nets.

| Criterion | A | B | C |
|---|---|---|---|
| **All-or-nothing saves** | Weak | Strong | Strong |
| **Two staff at once** | Weak | Strong | Strong |
| **Protection from scripts** | Weak | Strong | Good |
| **Easy to read and test** | Strong | Weak | Strong |
| **Clear messages for users** | Medium | Weak | Strong |
| **Effort for the team** | Low | High | Medium |

A is the quickest to build but fails NFR-005 and NFR-007. B is strong, but it hides business rules in SQL that the team would find hard to review and test. C takes a little more effort, but each layer has a clear job, so **C is the best fit for CivicConnect.**

### 2.4 Recommendation for the project

The team recommends the following (final decision in M2):

| ID | Recommendation | Feeds into |
|---|---|---|
| **R2-1** | One PostgreSQL transaction for all five saves, run by the backend | New ADR; new risk: partial save; M3 rollback test |
| **R2-2** | Conditional UPDATE to claim a request; version column for other edits | M2 data model; new risk: double ownership; M3 concurrency test |
| **R2-3** | Backend owns role and status rules; PostgreSQL constraints; insert-only history | M2 data model; RTM links for FR-011–FR-013, NFR-005, NFR-007 |
| **R2-4** | No caching in M2; indexes first | NFR-004 performance check |

**Limitations:** this assumes only a few staff act on the same request at once; if clashes turn out to be common, row locking may be a better choice. The design also depends on the reassignment rules (AC-011.2), which are still an open M1 item and would change the UPDATE condition.

---
## Task 3

### APIs and Integration Decisions

### 3.1 Integration Requirement

The existing architecture separates the user interface, backend/API and persistence layers, as established by D-001 and D-002. An integration mechanism is therefore required between the Staff Operations Panel and the Requester Portal when a service request changes state.

The specific case considered is the transition of a request's status by Service Staff, as defined by FR-012. NFR-002 requires the updated status to reach the Requester Portal within 15 seconds. At the same time, NFR-003 requires that information belonging to another requester not be exposed, while NFR-005 and FR-011 require controls to prevent two staff members from owning the same request.

The Staff Operations Panel acts as the producer by submitting changes through the backend, while the Requester Portal acts as the consumer by retrieving the updated request state. Without a defined integration approach, individual screens could implement different endpoints and validation rules. This could result in stale request statuses, which would conflict with NFR-002, as well as duplicated business logic across the user interfaces. Centralising these rules through the backend therefore supports the purpose of D-002 and provides a more consistent approach to request processing.

### 3.2 Comparison of Integration Mechanisms

Since both clients are browser-based, HTTP will form the basis of communication. The main decision is therefore how the Requester Portal should become aware of a change that it did not initiate.

| Criteria                          | REST with Client Polling                                                                                                   | REST with Push (WebSocket/SSE)                                                                          |
| --------------------------------- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Coupling                          | Low. The client periodically requests the latest state.                                                                    | Slightly higher because a persistent connection or subscription is required.                            |
| NFR-002                           | Can be satisfied with a polling interval of approximately 10 seconds or less, although this generates additional requests. | Can be satisfied with near-instant updates.                                                             |
| Failure behaviour                 | Degrades gradually. A missed poll results in a temporarily stale view until the next request.                              | A dropped connection requires reconnection or fallback logic, otherwise the client may become stale.    |
| Testability                       | Simple request/response behaviour is straightforward to test.                                                              | More complex because connection and lifecycle behaviour must also be tested.                            |
| Team unfamiliarity risk (RSK-001) | Low because REST is widely understood and uses established patterns.                                                       | Higher because real-time communication introduces additional infrastructure and development complexity. |

Where REST is used, the API should follow basic resource and method conventions. Resource-oriented nouns should be used for URIs, HTTP verbs should represent operations, and requests should remain stateless by carrying the information required for processing rather than depending on server-side session state. A service becomes more RESTful as it makes greater use of appropriate URIs, HTTP methods and hypermedia rather than relying on a single tunnelled endpoint.

Full HATEOAS maturity is not required for an internal API of this size. The additional discoverability and self-description provided by higher Richardson Maturity Model levels would provide limited value while the system has only a small number of functional requirements and no external or third-party consumers. Implementing these levels at this stage would therefore add complexity without a proportionate benefit (Devopedia, 2020; RESTful API, n.d.).

### 3.3 Recommended Approach

REST should be used for the reading and writing of request data because it is simple, testable and aligned with the team's existing level of experience. Short-interval client polling should be used to allow the Requester Portal to detect changes rather than introducing a WebSocket layer at this stage.

Polling is considered proportionate because it can satisfy NFR-002 without requiring additional real-time infrastructure. Its failure behaviour is also relatively straightforward: a failed or missed request results in a delayed refresh rather than a persistent connection becoming unavailable.

Push-based communication through WebSockets or Server-Sent Events can be considered as a future improvement if system requirements or team capacity justify it. It is not required as a day-one implementation.

This decision should extend D-002 in the Engineering Decision Log by explicitly recording the client synchronisation mechanism. A short Architecture Decision Record (ADR) should also document the decision. The approach should be reflected in the Requirements Traceability Matrix (RTM) Design/Architecture column against FR-003, FR-007 and NFR-002.

---
## Task 4

### Collaborative Engineering and CI Controls

### 4.1 Source Control Management and Branching

Git provides the mechanism for tracking changes to the project files, while Source Control Management (SCM) is the broader process used to maintain consistency between code, configuration and project versions. This is particularly relevant to RSK-006, where configuration differences between development, staging and production environments could result in inconsistent behaviour.

The project already uses protected main-branch development with a two-reviewer approval model. The branching strategy should therefore keep changes small and reviews manageable.

A trunk-based approach is suitable for the three-person team because it encourages short-lived branches and frequent integration into the main branch. This reduces the likelihood of long-running branches becoming out of sync and helps limit merge conflicts.

Gitflow was considered as an alternative, but its use of multiple long-lived branches would introduce additional process overhead for a small project. A simpler trunk-based approach is therefore more appropriate for CivicConnect (Atlassian, n.d.; AWS, n.d.).

The two-reviewer requirement also creates a potential risk for a three-person team because reviewers could approve changes without sufficient scrutiny simply to keep the work moving. This can be reduced by keeping pull requests small and focused on one feature, requirement or project artefact at a time.

### 4.2 Continuous Integration and Quality Gates

Git and branch protection help control how changes are merged, but they do not confirm that the application still builds or functions correctly. Continuous Integration (CI) addresses this by automatically building and testing changes whenever they are integrated.

For CivicConnect, CI should run when a pull request targets the main branch. The build should use a clean checkout, a repeatable command and locked dependencies so that it does not depend on an individual developer's local environment.

Due to RSK-001, the initial CI quality gates should focus on the checks that provide the most value without introducing unnecessary complexity. The application should successfully compile or build, and unit tests should pass before a pull request can be merged. These should be configured as required and blocking checks.

Linting and static analysis can initially be configured as warning or non-blocking checks. Once the team is comfortable with the tooling and the rules are producing useful results, they can be promoted to required checks.

CI also provides a control for RSK-005. Secrets required by GitHub Actions should be stored using encrypted repository or environment secrets rather than being committed to configuration files. Dependency monitoring and secret scanning should also be enabled where available. CI results should remain visible on the pull request so reviewers can see whether the automated checks passed.

Automation should handle repeatable mechanical checks, while reviewers remain responsible for judging design decisions, requirement coverage and overall correctness.

### 4.3 Recommended Approach

CivicConnect should use a lightweight GitHub Actions CI workflow that runs for pull requests targeting the main branch. The initial required checks should be the application build and unit tests.

Linting and static analysis should initially remain non-blocking and can be made mandatory once the team has established suitable rules. Dependabot and secret scanning should also be enabled to help identify dependency and credential risks.

The approach should be reflected in the project's branch protection rules, the PED CI and quality section, and the RTM Issue/PR and Test columns during M2 to M4.

This provides a lightweight engineering process that combines protected main-branch development, peer review and automated checks without introducing unnecessary infrastructure or process overhead.

---
## Task 5
| A2 finding | Evidence/alternative considered | Recommendation | Project decision it should inform | Expected PED/ADR/RTM/application evidence |
| :--- | :--- | :--- | :--- | :--- |
| Design problem 1 | 
| Design problem 2 | 
| Persistence/Data | Assigning a request saves five things (owner, status, history, audit, requester message). A partial save or two staff assigning at once breaks NFR-005 and NFR-007. | Approaches compared: A (backend only), B (PostgreSQL stored procedure), C (layered). Concurrency options: conditional UPDATE, version number, SELECT … FOR UPDATE, Serializable. Caching versus stale data also considered. | Approach C: one PostgreSQL transaction run by the backend; conditional UPDATE to claim a request; version column for other edits; PostgreSQL constraints; insert-only history; no caching in M2. | D-003 (PostgreSQL); new persistence ADR; M2 data model; Risk Register | ADR and data model in PED v2.0; new risks (partial save, double ownership); RTM links for FR-011–FR-013, NFR-005, NFR-007; M3 rollback and concurrency tests
| Integration/API | 
| SCM/CI | 
---

## References
Microsoft (2025) Cache-Aside pattern — Azure Architecture Center. Available at: https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside (Accessed: 12 September 2026).

PostgreSQL Global Development Group (2026a) PostgreSQL 18 documentation: Transaction isolation. Available at: https://www.postgresql.org/docs/current/transaction-iso.html (Accessed: 12 September 2026).

Atlassian (n.d.) *Trunk-based Development*. Available at: https://www.atlassian.com/continuous-delivery/continuous-integration/trunk-based-development (Accessed: 12 September 2026).

AWS (n.d.) *Advantages and disadvantages of the Trunk strategy*. Available at: https://docs.aws.amazon.com/prescriptive-guidance/latest/choosing-git-branch-approach/advantages-and-disadvantages-of-the-trunk-strategy.html (Accessed: 12 September 2026).

Devopedia (2020) *Richardson Maturity Model*. Available at: https://devopedia.org/richardson-maturity-model (Accessed: 12 September 2026).

Fowler, M. (2006) *Continuous Integration*. Available at: https://martinfowler.com/articles/continuousIntegration.html (Accessed: 12 September 2026).

RESTful API (n.d.) *Richardson Maturity Model*. Available at: https://restfulapi.net/richardson-maturity-model/ (Accessed: 12 September 2026).

