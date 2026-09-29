# ADR-003 Technology stack

**Status:** Incomplete. The team has to record its actual choice before this becomes Proposed.
**Owner:** DevOps, Security and Quality Lead
**Replaces:** D-003
**Quality drivers:** ASR-05 first, then ASR-01, ASR-02, ASR-04
**Risks touched:** RSK-001, RSK-008, RSK-019, RSK-004

> Replace every placeholder in square brackets before sign off.

## Why it is urgent

Eighteen of the thirty two RTM rows still read that the technology is pending this record. Nothing can be built without it, and ADR-001, ADR-002 and ADR-007 all assume things about the runtime. RSK-001 and RSK-008 both trace here, and both stay open until this closes.

## What constrains the choice

* **ASR-05 and NFR-009.** It has to run on free or low cost hosting, with the expected running cost written down.
* **RSK-001.** Learning an unfamiliar stack on a fixed schedule is the highest exposure on the register, so existing team skill counts as a criterion.
* **ADR-002.** The store must support transactions and a conditional update whose affected row count is readable.
* **ADR-005.** The backend needs a request pipeline where one authorisation check can run before every handler.
* **M1 constraints.** Schedule, compatibility across the stack, and learning curve.

## Settled already: the database

PostgreSQL. It gives the transactional behaviour ADR-002 depends on, it is available on free and low cost managed plans for NFR-009, it costs nothing to licence, and the team has agreed it.

Rejected: MySQL and MariaDB, equal to the task but with no advantage that beats familiarity. SQLite, because one writer at a time suits a system where staff write concurrently very badly. A document store, because the domain is plainly relational and the guarantees ASR-01 and ASR-03 ask for are exactly what a relational engine provides.

Recorded specifics: PostgreSQL version [version], hosted on [provider and plan].

## Still open: the application stack

Each line needs evidence, which means a version, a licence, a link, or the name of the person who has used it.

Criteria, in weight order:

1. Skill already on the team (RSK-001).
2. Can reach a working slice inside the schedule.
3. Free or low cost hosting exists (NFR-009).
4. Supports one authorisation pipeline (ADR-005).
5. First class PostgreSQL support with readable affected row counts (ADR-002).
6. Licence and cost.
7. Dependency health and security advisories.
8. Can run integration tests against a real database.
9. Fits the deployment direction in ADR-007.

**Candidates weighed:** [A], [B], [C].
**Chosen:** frontend [name and version], backend [name and version], build tool [name], test framework [name].
**Rejected and why:** [one line for each, tied to a criterion above].

## Versions and licences



## What it costs

To be written with the decision.

## Evidence

**RTM:** every technology column resolves here.
**Depends on:** ADR-001. **Blocks:** ADR-007 and all implementation evidence.
