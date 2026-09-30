# ADR-001 Layered modular monolith as one deployable unit

> **Status:** Proposed, awaiting team approval
> **Owner:** Systems Architect and Backend Lead
> **Builds on:** D-001, D-002 in the PED decision log
> **Quality drivers:** ASR-01, ASR-02, ASR-03, ASR-04, ASR-05
> **Risks touched:** RSK-001, RSK-015

## Problem

CivicConnect manages service requests and their history for four types of user. The main information is centred around the service request, so the system does not need several separate applications to manage it.

A request must have one owner (NFR-005), actions must be traceable (NFR-007), and a requester must not be able to see another requester's information (NFR-003). These requirements are closely connected and can be handled within the same application.

The team also has three students, a fixed project schedule and a requirement to keep hosting costs low (NFR-009). The team is still gaining experience with the selected technologies (RSK-001). M1 established that the interface, backend and persistence should be separated, but the final application structure was left for M2.

## Options

**A. Layered modular monolith, one deployable unit.** The application uses separate interface, service and persistence layers, with the backend divided into modules. These modules share one relational database. Request ownership and status changes can be handled in the same transaction, which helps enforce the single-owner requirement.

**B. Microservices.** Requests, access and reporting could be separate services. This would add network communication, more deployment work and more hosting requirements. It would also make operations such as assigning a request more complicated because they could involve multiple services.

**C. Browser application connected directly to a hosted database.** This would be quick to start, but it would put too much responsibility for authorisation and business rules on the client. This does not fit the security and access requirements.

**D. Serverless functions.** This could reduce some hosting costs, but the team has limited experience with this approach (RSK-001). It would also introduce extra considerations around database connections and response times.

## Decision

**Option A is selected.**

CivicConnect will use one deployable application connected to one relational database. The application will be divided into four main modules:

* **Requests**
* **Access**
* **ReferenceData**
* **Reporting**

The modules will communicate through defined service interfaces instead of directly accessing another module's internal classes or tables.

Reporting will use the same request data as the rest of the system. This keeps management information based on the actual request records and reduces the chance of reporting data becoming inconsistent (NFR-008).

## What it costs

* The module boundaries depend on the team following the agreed structure during development and code review.
* A change to one part of the application means the whole application needs to be redeployed.
* Scaling is mainly vertical. This is acceptable for the expected project scope, but larger-scale growth remains a future consideration.
* The application and database can become a single point of failure. This risk is recorded under RSK-015 and the deployment approach is covered by ADR-007.

## Evidence

**RTM:** The architecture column links requirements to the relevant modules.
**Requirements:** NFR-003, NFR-004, NFR-005, NFR-007, NFR-008, NFR-009.
**Verification:** The architecture will be reviewed against the ASRs during the M2 architecture review. Layer boundaries will also be checked during development and code review.
**Depends on this record:** ADR-002, ADR-005, ADR-006, ADR-007.
