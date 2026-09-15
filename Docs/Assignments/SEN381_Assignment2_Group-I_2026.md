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
| **Effort for our team** | Low | High | Medium |

A is the quickest to build but fails NFR-005 and NFR-007. B is strong, but it hides business rules in SQL that the team would find hard to review and test. C takes a little more effort, but each layer has a clear job, so **C is the best fit for CivicConnect.**

---
## Task 3


---
## Task 4


---
## Task 5
| A2 finding | Evidence/alternative considered | Recommendation | Project decision it should inform | Expected PED/ADR/RTM/application evidence |
| :--- | :--- | :--- | :--- | :--- |
| Design problem 1 | 
| Design problem 2 | 
| Persistence/Data | 
| Integration/API | 
| SCM/CI | 
---