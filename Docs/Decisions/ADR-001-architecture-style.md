# ADR-001 Microservices as independently deployable services

> **Status:** Approved
> **Owner:** Systems Architect and Backend Lead
> **Builds on:** D-001, D-002 in the PED decision log
> **Supersedes:** the earlier version of this ADR (layered modular monolith). Recorded in the PED as D-005
> **Quality drivers:** ASR-01, ASR-02, ASR-03, ASR-04, ASR-05
> **Risks touched:** RSK-001, RSK-014, RSK-015

## Problem

CivicConnect manages service requests and their history for four types of user. A request must have one owner (NFR-005), actions must be traceable (NFR-007), and a requester must not be able to see another requester's information (NFR-003).

M1 established that the interface, backend and persistence should be separated, but left the final application structure to M2. The first M2 version of this ADR chose a modular monolith. The team has now decided to move to microservices, so the system is split into services that can be built, deployed and changed on their own.

The constraints have not changed. The team is three students on a fixed schedule, hosting has to stay low-cost (NFR-009), and we are still learning the chosen stack (RSK-001). The decision has to work within those limits, and the cost of splitting the system has to be stated openly.

## Options

| Option | What it is | Strengths | Weaknesses | Outcome |
| :--- | :--- | :--- | :--- | :--- |
| **A. Microservices** | Requests, Access, ReferenceData and Reporting run as separate services behind one API entry point. | Each service can be deployed and changed on its own. Boundaries are enforced by the network, not only by team discipline. Team members can own a service each. Reporting can scale separately from request handling. | Network calls between services. More deployment work and more hosting units. Cross-service actions are harder to keep consistent. | **Selected** |
| **B. Layered modular monolith** | One deployable application with interface, service and persistence layers, split into modules that share one database. | Simplest to build and host. Ownership and status changes happen in one transaction. | One redeploy for any change. Module boundaries rely on code review. Scaling is vertical only. | Previous decision, replaced |
| **C. Browser application straight to a hosted database** | The client talks directly to the database and carries the business rules. | Quick to start. | Authorisation and business rules sit on the client, which does not fit NFR-003 or the RBAC policy. | Rejected |
| **D. Serverless functions** | Each operation is a separate function. | Can reduce hosting cost. | Limited team experience (RSK-001). Database connection handling and response times need extra care. | Rejected |

### How the options compare against the requirements

| Requirement or driver | A. Microservices | B. Modular monolith | C. Direct to database | D. Serverless |
| :--- | :--- | :--- | :--- | :--- |
| One owner per request (NFR-005) | Fair. Kept safe by letting only the Requests service change ownership | Strong. Single transaction | Weak | Fair |
| Requester privacy (NFR-003) | Strong. Access service decides, every service checks | Strong | Weak | Fair |
| Traceable actions (NFR-007) | Fair. History is written by the Requests service in the same transaction as the change | Strong | Weak | Fair |
| Reporting accuracy (NFR-008) | Fair. Reporting reads request data through the Requests service | Strong | Fair | Fair |
| Low hosting cost (NFR-009) | Weak. More services to host, free-tier limits apply to each | Strong | Strong | Fair |
| Team experience (RSK-001) | Weak. New skills needed, offset by smaller codebases per service | Fair | Fair | Weak |
| Independent deployment and scaling | Strong | Weak | Not applicable | Strong |

## Decision

**Option A is selected.**

CivicConnect will be built as four services behind a single API entry point that the browser clients call:

| Service | Responsibility |
| :--- | :--- |
| **Requests** | Creating requests, ownership, status changes, and the history record for each change. |
| **Access** | Users, roles, permissions and the RBAC policy (ADR-005). |
| **ReferenceData** | Categories and other lookup data. |
| **Reporting** | Management and oversight figures, built from request data. |

The rules that keep the system safe:

* **Ownership stays in one place.** Only the Requests service can assign a request or change its status. Assignment and its history entry are written in one database transaction inside that service, so the single-owner rule (NFR-005) does not depend on several services agreeing.
* **Services do not reach into each other's data.** Each service owns its own tables and exposes what others need through its API. This replaces the old rule about module service interfaces.
* **One PostgreSQL instance for now.** To stay inside the free-tier limits (NFR-009), the services share one managed PostgreSQL instance, with a separate schema per service. Moving to a database per service is a later option. ADR-002 needs a note to match.
* **Authorisation is checked in every service.** The entry point passes the caller's identity on, and each service applies the policy from ADR-005 rather than trusting the caller.
* **Reporting stays honest.** Reporting reads request data from the Requests service and does not keep a separate copy that could drift (NFR-008).

## What it costs

* More moving parts. Each service needs its own build, configuration and deployment, which is a real load for three people on a fixed schedule.
* Hosting is counted per service. Free-tier limits and inactivity sleep apply to each one, so cost and start-up delays need rechecking (RSK-014, ADR-007).
* A call between services can fail or be slow, so every cross-service call needs a timeout and a clear error response.
* Actions that touch more than one service cannot use a single transaction. We avoid this by keeping ownership and history inside Requests, but any future feature that crosses services has to be designed with this in mind.
* Failure points increase. Several services plus the shared database can each go down. RSK-015 still applies to the database.
* Testing is harder. Service-to-service behaviour needs its own tests, not only unit tests.
* Team skill is still the main risk (RSK-001). A small proof-of-concept with two services should come before the full split.

## Evidence

**RTM:** The architecture column links requirements to the relevant service.
**Requirements:** NFR-003, NFR-004, NFR-005, NFR-007, NFR-008, NFR-009.
**Verification:** The decision will be reviewed against the ASRs in the M2 architecture review. Service boundaries and the ownership rule will be checked in code review and by a concurrency test on the Requests service.
**Depends on this record:** ADR-002, ADR-005, ADR-006, ADR-007. Each of these needs a short update or superseding note to match the microservices split.
