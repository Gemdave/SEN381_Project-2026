# ADR-001 Layered modular monolith as one deployable unit
>**Status:** Proposed, awaiting team approval  
**Owner:** Systems Architect and Backend Lead  
**Builds on:** D-001, D-002 in the PED decision log  
**Quality drivers:** ASR-01, ASR-02, ASR-03, ASR-04, ASR-05  
**Risks touched:** RSK-001, RSK-015  

## Problem

CivicConnect keeps one controlled record of a service request for four kinds of user. The properties that matter most are concentrated rather than spread out. A request must have exactly one owner (NFR-005), every controlled action must be attributable (NFR-007), and nobody may read another requester's data (NFR-003). All three describe a single small domain.

Pushing the other way is capacity. Three students, a fixed schedule, a sponsor who expects free or low cost hosting (NFR-009), and an open risk that the team does not know its own stack well yet (RSK-001). M1 decided that interface, backend and persistence stay separate, and left the style itself open.

## Options

**A. Layered modular monolith, one deployable unit.** Interface, service and persistence layers inside one application, split internally into modules, over one relational database. Ownership and status changes stay inside a local transaction, so the single owner rule is enforceable with ordinary database behaviour.

**B. Microservices.** Requests, identity and reporting as separate services. Accepting a request would then cross a network boundary, so the atomicity NFR-005 asks for would need distributed transactions or compensation. It also multiplies hosting cost and operational work, and the milestone brief warns that a more distributed design is not automatically a better one.

**C. Browser application talking straight to a hosted database.** Quick to begin, but it drags authorisation towards the client, which is the opposite of what ASR-02 requires, and leaves the audit guarantee depending on client behaviour.

**D. Serverless functions.** Cheap, but nobody on the team has run it (RSK-001), cold starts threaten the NFR-004 response target, and connection limits complicate the relational store that ASR-01 depends on.

## Decision

Option A. One deployable application over one relational database, divided into four modules: Requests, Access, ReferenceData and Reporting. Modules talk to each other through service interfaces, never by reaching into another module's classes or tables.

Reporting reads the same tables the requests live in, so management figures cannot drift away from the records they describe (NFR-008).

## What it costs

* Module boundaries rest on agreement, not on the runtime. Nothing stops one module importing another's internals, so code review has to catch it.
* Any change redeploys everything. Reporting and administration cannot ship on their own.
* Scaling is vertical only. Acceptable at the volume NFR-004 describes, and recorded as a forward consideration rather than a solved problem.
* One application and one database is a genuine single point of failure. RSK-015 carries it and ADR-007 records what is and is not covered.

## Evidence

**RTM:** every row. The architecture column places each requirement in a module.  
**Requirements:** NFR-003, NFR-004, NFR-005, NFR-007, NFR-008, NFR-009.  
**Verification:** reviewed at the architecture walkthrough against ASR-01 to ASR-06. Layer direction is checked in review, and is a candidate for an automated check once a build exists.  
**Depends on this record:** ADR-002, ADR-005, ADR-006, ADR-007.