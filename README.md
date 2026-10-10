# CivicConnect
**Community Service Request Management Platform**
> **Module:** Software Engineering 381 (SEN381)  
> **NQF Level:** 8  
> **Academic Year:** 2026  
> **Institution:** Belgium Campus ITversity  
> **Project:** Integrated Team Software Engineering Project

---
## 1. Project Overview
CivicConnect is a community service request management platform designed to replace fragmented request-management processes such as email, telephone calls, WhatsApp messages and disconnected spreadsheets with a single, traceable system.  
The platform is intended to provide one authoritative record for the complete lifecycle of a service request:  
1. A requester submits a service request.
2. The request is categorized and recorded.
3. Service staff receive and accept requests.
4. Requests progress through controlled status transitions.
5. Staff record actions and notes.
6. Request history provides an auditable record of what happened.
7. Requesters receive visibility into the progress and outcome of their requests.
8. Management and administrators can eventually use the collected data for oversight, reporting and governance.

The project is being developed progressively according to the SEN381 requirements, architecture, risk, quality and governance baselines.

---
## 2. Current Project Status
### Overall status
**Milestone 2 / Build phase — in development**  
The repository contains meaningful working construction, automated tests and CI infrastructure. However, CivicConnect is **not yet a production-ready application**.  
The current implementation primarily demonstrates the staff-side request workflow and the engineering controls around assignment, authorization, status transitions and request history.

### Currently implemented in the active `app/` solution  
- Layered .NET 10 application architecture.
- Domain model for service requests.
- Controlled request categories.
- Requester, service staff, management and administrator roles in the domain model.
- Staff request queue.
- Staff request detail page.
- Request acceptance/assignment.
- Server-side authorization checks.
- Capability-based UI composition.
- Controlled request status transitions.
- Append-only request history.
- Transactional assignment and status updates.
- Database-level conditional updates for concurrency protection.
- Development-only seeded users.
- PostgreSQL persistence through Entity Framework Core/Npgsql.
- Unit tests for application behavior.
- Domain tests for status-transition rules.
- PostgreSQL integration testing for concurrent request acceptance.
- GitHub Actions CI.
- Static analysis and formatting checks.
- Dependency vulnerability scanning.
- Repository secret scanning.
- A combined CI quality gate.

### Not yet complete
The following areas remain incomplete or are represented primarily by requirements/design documentation:  
- Production authentication.
- Complete requester-facing UI.
- Request submission UI/API in the active `app/` solution.
- Complete requester request history and feedback workflow.
- Full staff workflow including all planned notes/status operations.
- Management/oversight functionality.
- Administrator functionality.
- Sponsor/reporting functionality.
- Attachments.
- Full production RBAC.
- Production deployment.
- Final database migration/bootstrap setup for the active `app/` solution.
- Complete alignment between the API documentation and the currently implemented `app/` endpoints.

The project documentation deliberately records several of these as future, deferred or change-controlled work rather than silently treating them as complete.

---
# 3. Core Product Goals
CivicConnect is being developed around three central operational questions:  
> **What has been requested?**  
> **Who is accountable for acting on it?**  
> **Has it actually been resolved?**

The system therefore prioritizes:  
- Traceability
- Accountability
- Controlled workflows
- Role-based access
- Data integrity
- Auditability
- Clear request status
- Sustainable deployment
- Automated quality controls

---
# 4. Stakeholders
| Stakeholder | Role in CivicConnect |
|---|---|
| **Requester** | Submits service requests and tracks their progress and outcome. |
| **Service Staff** | Receives, accepts, works on and resolves requests. |
| **Management** | Oversees service performance and accountability. |
| **Administrator** | Maintains users, permissions and reference data. |
| **Sponsor** | Owns/funds the platform and is concerned with value, sustainability and cost. |

---
# 5. Functional Scope
The requirements baseline defines functionality across several user groups.

## Requester
Planned requester functionality includes:  
- Submit a service request.
- Select a category from a controlled list.
- View the current status of a request.
- View previously submitted requests.
- Search/filter request history.
- Receive in-application feedback when requests change state.
- Eventually attach supporting information such as photographs.

## Service Staff
Planned staff functionality includes:  
- View an authorized request queue.
- Search, filter and sort requests.
- View request details.
- Accept/claim requests.
- Transition requests through the controlled lifecycle.
- Add action notes.
- Close completed requests.
- Maintain accountability through request history.

## Management
Planned management functionality includes:  
- Monitor open requests.
- Monitor overdue requests.
- View service-performance information.
- Analyze requests by relevant dimensions.
- Audit request lifecycle accountability.

## Administrator
Planned administrator functionality includes:  
- Manage users.
- Manage roles and permissions.
- Maintain request categories.
- Retire reference-data values without destroying historical information.

## Sponsor
The sponsor's primary concern is that the system provides accountability and visibility without creating unnecessary operational or financial overhead.

---
# 6. Request Lifecycle
The active domain model defines the following controlled request lifecycle:  
```
Received
   |
   v
Assigned
   |
   v
InProgress
   |
   v
Resolved
   |
   v
Closed
```

The domain deliberately prevents arbitrary status changes.  
For example:  
```
Received -> Closed
Received -> Resolved
Assigned -> Closed
```

are not valid transitions.  
The status-transition rules are centralized in the domain layer so that the lifecycle cannot be changed accidentally by individual UI pages.  
The project documentation also discusses a proposed `Rejected` state. This is currently subject to the relevant change request and should not be treated as part of the completed active implementation.

---
# 7. Architecture
The active application uses a layered architecture:  
```
┌─────────────────────────────────────┐
│          CivicConnect.Web           │
│      Razor Pages / HTTP / UI        │
└──────────────────┬──────────────────┘
                   │
                   v
┌─────────────────────────────────────┐
│      CivicConnect.Application       │
│   Use cases / services / policies   │
└──────────────────┬──────────────────┘
                   │
                   v
┌─────────────────────────────────────┐
│         CivicConnect.Domain         │
│ Entities / rules / lifecycle model │
└─────────────────────────────────────┘
                   ^
                   │
┌─────────────────────────────────────┐
│     CivicConnect.Infrastructure     │
│ EF Core / PostgreSQL / persistence │
│ security / seed / transactions     │
└─────────────────────────────────────┘
```

### Domain
Contains business concepts and rules:  
- `Request`
- `RequestStatus`
- `StatusTransition`
- `RequestHistoryEntry`
- `Category`
- `AppUser`
- User roles

The domain layer does not depend on the database or web layer.

### Application
Contains application-level operations and abstractions:  
- Assignment
- Status transitions
- Authorization policies
- Repository interfaces
- Unit-of-work abstraction
- Clock abstraction

### Infrastructure
Provides implementations for:  
- PostgreSQL persistence
- Entity Framework Core
- Repositories
- Transactions
- Authorization policy
- Development seed data
- System clock

### Web
Provides:  
- ASP.NET Core Razor Pages
- Staff queue
- Staff request detail page
- Assignment endpoint
- UI capability composition
- Development identity
- Static assets

---
# 8. Technology Stack
The active application is based on:  
| Component | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| Web framework | ASP.NET Core |
| UI | Razor Pages |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL |
| PostgreSQL provider | Npgsql / EF Core provider |
| Testing | xUnit |
| CI | GitHub Actions |
| Static analysis | Roslyn / `dotnet format` |
| Dependency scanning | `dotnet list package --vulnerable` |
| Secret scanning | Gitleaks |
| Intended application hosting | Render |
| Intended database hosting | Neon PostgreSQL |

The repository pins the .NET SDK through:  
```
app/global.json
```

with SDK version `10.0.100`.

---
# 9. Repository Structure
The repository is organized into three major areas.  
```
SEN381_Project-2026/
│
├── app/
│   ├── src/
│   │   ├── CivicConnect.Domain/
│   │   ├── CivicConnect.Application/
│   │   ├── CivicConnect.Infrastructure/
│   │   └── CivicConnect.Web/
│   │
│   ├── tests/
│   │   ├── CivicConnect.Domain.Tests/
│   │   ├── CivicConnect.Application.Tests/
│   │   └── CivicConnect.IntegrationTests/
│   │
│   ├── db/
│   │   └── seed/
│   │
│   ├── CivicConnect.slnx
│   └── global.json
│
├── Backend/
│   ├── src/
│   │   ├── CivicConnect.Core/
│   │   ├── CivicConnect.Data/
│   │   └── CivicConnect.Web/
│   │
│   ├── tests/
│   ├── db/
│   └── README_backend.md
│
├── Docs/
│   ├── AI_Register/
│   ├── API/
│   ├── App Structure/
│   ├── Architecture/
│   ├── Assignments/
│   ├── CI_Documentation/
│   ├── Data/
│   ├── Decisions/
│   ├── Diagrams/
│   ├── Governance/
│   ├── PED/
│   ├── Requirements/
│   └── Risks/
│
├── .github/
│   ├── workflows/
│   │   └── ci.yml
│   └── pull_request_template.md
│
└── README.md
```

---
# 10. Important Note About `app/` and `Backend/`
There are currently **two backend codebases in the repository**.

## `app/`
`app/` is the newer active implementation and should be treated as the primary application going forward.  
It follows the newer layered architecture:  
```
Domain
Application
Infrastructure
Web
```

The current build slice concentrates on the staff request workflow and its associated engineering controls.

## `Backend/`
`Backend/` contains an earlier implementation of the CivicConnect backend.  
It contains a more extensive API-oriented build slice, including:  
- Request submission
- Categories
- Request history
- Feedback
- Staff queue
- Assignment
- Status transitions
- Notes
- API controllers
- SQL migrations
- API contract implementation

It also has its own tests and backend README.  
This implementation should currently be regarded as **legacy/reference construction rather than a second production application**.  
Future development should avoid adding new functionality independently to both implementations. The team should either:  
1. continue with `app/` and archive/remove the older backend when appropriate, or
2. formally designate `Backend/` as the active implementation and reconcile the repository accordingly.

The project documentation should always make clear which implementation is being assessed.

---
# 11. Current Staff Workflow
The active `app/` solution currently demonstrates the following flow:  
```
Staff Queue
    |
    v
Open Request
    |
    v
Check Authorization
    |
    v
Accept Request
    |
    +----> Success
    |
    +----> Conflict if another staff member accepted it
    |
    v
Request becomes Assigned
    |
    v
History entry written
```

The assignment endpoint is:  
```
POST /api/v1/requests/{id}/assignment
```

A successful assignment returns `200`.  
If another staff member has already claimed the request, the application returns `409 Conflict`.

---
# 12. Concurrency and Data Integrity
One of the important engineering decisions in CivicConnect is that request assignment must be safe when multiple staff members attempt to claim the same request.  
The application does not rely solely on:  
```
1. Check whether request is unassigned.
2. Assign request.
```

because two users could pass the check simultaneously.  
Instead, the database write itself contains the precondition:  
```
UPDATE request
WHERE request is unassigned
  AND request is Received
```

The affected-row count determines whether the operation succeeded.  
Therefore:  
```
Staff A ──────┐
              ├──> Database conditional update
Staff B ──────┘

             ┌───────────────┐
             │ One succeeds  │
             │ One conflicts │
             └───────────────┘
```

The integration test `ConcurrentAcceptTests` specifically exercises this behavior.
Successful state changes and their history records are also performed within the same transaction.

---
# 13. Authorization and Security
Authorization is implemented as a server-side policy rather than relying on UI controls.  
The active authorization model includes:  
| Action | Current rule |
|---|---|
| View | Depends on role and request ownership |
| Assign | Service staff |
| Transition | Assigned service staff |
| Add note | Assigned service staff |
| Close | Assigned service staff |

The UI uses capabilities to decide which controls to display, but this is not considered the security boundary.  
The application checks authorization again when an action is performed.  
This follows the principle:  
> **Hiding a button is a usability feature, not an access-control mechanism.**

---
# 14. Development Authentication
Real authentication is **not yet implemented**.  
During development, the application uses a development-only identity mechanism.  
The current implementation reads a user identifier from the request and falls back to a seeded staff user when necessary.  
The application deliberately refuses to run outside Development because production authentication has not yet been implemented.  
This is an explicit temporary control associated with the open authentication change request.  
**Do not deploy the current development identity mechanism to a real environment.**

---
# 15. Database
CivicConnect uses PostgreSQL for persistent state.  
The active Entity Framework model contains:  
- `requests`
- `request_history`
- `categories`
- `users`

Important database constraints include:  
- Unique request references.
- Indexed requester IDs.
- Indexed staff queue fields.
- Controlled status values.
- Append-only request history at the application level.
- Unique category names.

Development seed data creates:  
- Categories:
  - Fault
  - Equipment
  - Security
  - IT
  - Maintenance
  - Lost Property
  - Other
- Development requester.
- Two development staff users.
- Five sample requests.

There is also a performance seed script capable of generating 1,000 requests for NFR-004 performance measurement.

---
# 16. Testing
The active `app/` solution contains three test projects.

## Domain tests
```
app/tests/CivicConnect.Domain.Tests
```

These verify core domain rules such as valid and invalid status transitions.  

## Application tests
```
app/tests/CivicConnect.Application.Tests
```

These test application services including:  
- Successful assignment.
- Conflict handling.
- Missing requests.
- Authorization checks.
- History creation.

## Integration tests
```
app/tests/CivicConnect.IntegrationTests
```

These verify database-dependent behavior, particularly concurrent request acceptance against PostgreSQL.  
Integration tests are skipped when no test database is configured.  
Set:  
```
CIVICCONNECT_TEST_DB
```

to a disposable PostgreSQL database to run them.

---
# 17. Continuous Integration  
GitHub Actions is configured in:  
```
.github/workflows/ci.yml
```
The CI pipeline currently contains five major quality controls.

### Build and Test
Runs:  
```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

### Static Analysis
Runs:  
```bash
dotnet build -p:EnableNETAnalyzers=true
dotnet format --verify-no-changes
```

### Dependency Vulnerability Check
Checks direct and transitive NuGet dependencies for known vulnerabilities.  
High and Critical vulnerabilities fail the job.  

### Secret Scanning
Gitleaks scans the repository history for accidentally committed secrets.

### Quality Gate
The final quality gate requires all four preceding checks to succeed.  
```
                 ┌── Build & Test ────────┐
                 │                         │
                 ├── Static Analysis ─────┤
Pull Request ────┤                         ├──> Quality Gate
                 ├── Dependency Scan ─────┤
                 │                         │
                 └── Secrets Scan ────────┘
```

---
# 18. Running the Active Application
## Requirements
Install:  
- .NET 10 SDK
- PostgreSQL 17.x
- Git

Verify the SDK:  
```bash
dotnet --version
```

The repository expects .NET SDK `10.0.100` or a compatible feature version.

---
## Database Configuration
The application requires a PostgreSQL connection string.  
For local development, prefer .NET user secrets:  
```bash
dotnet user-secrets set \
  "ConnectionStrings:CivicConnect" \
  "Host=localhost;Database=civicconnect_dev;Username=postgres;Password=<password>" \
  --project app/src/CivicConnect.Web
```

Alternatively, the connection string can be supplied through the environment:  
```
ConnectionStrings__CivicConnect
```

Do not commit database credentials.

---
## Running the Application
From the repository root:  
```bash
cd app
dotnet run --project src/CivicConnect.Web
```

### Current database-bootstrap caveat
The active web application calls Entity Framework Core migrations during Development startup.  
The current `app/` tree does not yet contain a committed EF migration set.  
Therefore, **database migration/bootstrap should be completed before treating the above command as a guaranteed clean-machine setup procedure**.  
This is an outstanding implementation task rather than something the README should conceal.

---
# 19. Running Tests
From `app/`:  
```bash
dotnet test
```

This runs the domain and application tests and the integration-test project.  
To enable the PostgreSQL integration tests, provide a disposable test database:  

### Linux/macOS
```bash
export CIVICCONNECT_TEST_DB="Host=localhost;Database=civicconnect_test;Username=postgres;Password=<password>"
dotnet test
```

### Windows PowerShell
```powershell
$env:CIVICCONNECT_TEST_DB="Host=localhost;Database=civicconnect_test;Username=postgres;Password=<password>"
dotnet test
```
The integration test fixture recreates its test database, so **never point it at a database containing real or important data**.

---
# 20. API
The intended CivicConnect API is documented under:  
```
Docs/API/
```

The machine-readable specification is:  
```
Docs/API/openapi.yaml
```

The proposed API uses:  
```
/api/v1
```
and includes planned endpoints for:  
- Categories
- Request submission
- Request history
- Request details
- Staff queue
- Assignment
- Status transitions
- Notes
- Feedback
- Health checks

### Important API documentation note
The API contract describes the intended Build Slice 1 interface and is more extensive than the currently implemented endpoint surface in the active `app/` project.  
For example, the active `app/` implementation currently exposes the assignment endpoint:  
```
POST /api/v1/requests/{id}/assignment
```

while several endpoints documented in `Docs/API/API_Contract_v1.md` have not yet been implemented in `app/`.  
The API documentation should therefore be treated as the **engineering contract/baseline**, not as proof that every documented endpoint is currently available.

---
# 21. Configuration and Secrets
The project follows the principle that secrets must never be committed to source control.  
Configuration is intended to be supplied through:  
- .NET user secrets during local development.
- Environment variables in deployment.
- Hosting-provider secret/configuration facilities.

The primary database configuration key is:  
```
ConnectionStrings__CivicConnect
```

The repository should never contain:  
- Database passwords.
- API keys.
- Signing keys.
- Production connection strings.
- Certificates/private keys.

---
# 22. Deployment Direction
The documented deployment direction is:  
```
                  ┌──────────────────┐
                  │     GitHub       │
                  │   Source + CI    │
                  └────────┬─────────┘
                           │
                           v
                  ┌──────────────────┐
                  │     Render       │
                  │ ASP.NET Core App │
                  └────────┬─────────┘
                           │ TLS
                           v
                  ┌──────────────────┐
                  │      Neon        │
                  │   PostgreSQL     │
                  └──────────────────┘
```

The project currently intends to use:  
- **Render Free** for application hosting.
- **Neon Free** for PostgreSQL.
- **GitHub Actions** for CI.
The target cost is therefore approximately:  
```
$0/month
```

at coursework scale.  
However, the deployment plan is currently a **direction rather than completed production deployment evidence**.  
The project documentation identifies the absence of a deployment Dockerfile and deployment verification as remaining work.

---
# 23. Performance
The project includes a database seed script for generating 1,000 requests:  
```
app/db/seed/seed_requests.sql
```

This exists to support performance testing against realistic request volume.  
The project targets responsive staff queue and oversight views under the expected coursework demonstration workload rather than production-scale traffic.  

---
# 24. Engineering Documentation
The `Docs/` directory is an important part of the project and should be read alongside the source code.

### Requirements
```
Docs/Requirements/ASR_Register.md
Docs/PED/PED_v3.md
```

Contains:  
- Stakeholders.
- Functional requirements.
- Non-functional requirements.
- Acceptance criteria.
- Assumptions.
- Constraints.
- Requirements traceability.

### Architecture
```
Docs/Architecture/
Docs/Decisions/
Docs/Diagrams/
```

Contains the architectural baseline and ADRs.  
Important decisions include:  
- ADR-001 — Architecture style.
- ADR-002 — Persistence and data integrity.
- ADR-003 — Technology stack.
- ADR-004 — Role-based view composition.
- ADR-005 — RBAC authorization policy.
- ADR-006 — API interface contract.
- ADR-007 — Deployment direction.

### Risk Management
```
Docs/Risks/
```  

Contains the current risk register and RTM-related risk evidence.

### CI and Quality
```
Docs/CI_Documentation/
```

Documents the CI quality gates and their engineering rationale.

### Responsible AI
```
Docs/AI_Register/
```

Records AI-assisted research, documentation and implementation activity together with human verification.

### Governance
```
Docs/Governance/
```

Contains the team working agreement and project governance material.

### Assignments
```
Docs/Assignments/
```

Contains SEN381 assignment deliverables and supporting evidence.

---
# 25. Requirements Traceability
The project uses a Requirements Traceability Matrix to connect:  
```
Stakeholder Need
       ↓
Requirement
       ↓
Acceptance Criterion
       ↓
Architecture / ADR
       ↓
Implementation
       ↓
Test
       ↓
Evidence
```

This is important because a feature is not considered complete merely because code exists.  
The implementation must be traceable to an approved requirement or engineering decision and supported by appropriate verification evidence.

---
# 26. Engineering Principles
The project follows several recurring engineering principles.

### Server-side authorization
Never trust the UI to enforce permissions.

### Database-enforced concurrency
Critical ownership changes must be safe under concurrent writes.

### Transactional state changes
A state change and its corresponding history record should succeed or fail together.

### Append-only history
Historical accountability records are not edited or silently deleted.

### Controlled state transitions
The request lifecycle is defined centrally rather than allowing arbitrary status changes.

### Secrets outside source control
Credentials belong in environment/user-secret configuration.

### Progressive verification
Code should be supported by automated tests and CI rather than relying solely on manual demonstration.

### Traceability
Requirements, risks, decisions, implementation and tests should remain connected.

---
# 27. Known Limitations
The following limitations should be considered when evaluating the current repository.  
1. **Authentication is incomplete.**  
   Development-only identity is used while the final authentication decision remains open.
2. **The active UI is incomplete.**  
   The current `app/` UI focuses on staff queue and request details rather than the complete requester/management experience.
3. **The active API is incomplete.**  
   The documented API contract contains endpoints that have not yet been implemented in `app/`.
4. **The database migration story needs completion.**  
   The active application expects EF migrations, but migration artifacts are not currently present in the active project.
5. **Management and administration functionality is incomplete.**
6. **Attachments are not implemented.**
7. **Production deployment has not yet been demonstrated.**
8. **The repository contains both a legacy `Backend/` implementation and the newer `app/` implementation.**  
   These should eventually be consolidated or explicitly separated.
9. **Some project documentation describes intended or proposed functionality rather than current implementation.**  
   Requirements and API documentation should not be interpreted as evidence that a feature is already complete.
10. **The project has not been executed in this review environment.**  
    The uploaded project was inspected statically; the review environment does not have the .NET SDK installed, so a fresh `dotnet build`/`dotnet test` verification was not possible here.

---
# 28. Project Team
| Team Member | Primary Responsibility |
|---|---|
| **Mogau Malope** | Requirements, stakeholder alignment, frontend/UX and quality assurance |
| **Keletso Marota** | Systems architecture, backend/API, persistence and staff workflows |
| **Gerald Enright** | DevOps, security, CI/CD, quality, risk management and operational engineering |

The team working agreement and repository governance documents provide the authoritative detail regarding responsibilities and collaboration.

---
# 29. Development Workflow
The project uses GitHub-based collaborative development.  
Changes should:  
1. Be associated with a requirement, risk, decision or engineering task.
2. Be developed on an appropriate branch.
3. Include relevant documentation updates.
4. Avoid committing secrets.
5. Include appropriate tests.
6. Pass the CI quality gate.
7. Receive the required review/approval according to the team working agreement.

The repository pull-request template reinforces these requirements.

---
# 30. Responsible AI
AI tools may be used for research, documentation, implementation assistance and engineering analysis.  
AI-assisted contributions are subject to human verification.  
The project maintains an AI Usage Register: `Docs/AI_Register/AI_Register.md`  
AI-generated material is not considered authoritative merely because an AI system produced it.  
Human review remains responsible for:  
- Technical correctness.
- Security decisions.
- Requirement interpretation.
- Source verification.
- Architecture decisions.
- Final implementation decisions.

---
# 31. Useful Documentation Map
| Need | Start here |
|---|---|
| Understand requirements | `Docs/PED/PED_v3.md` |
| Understand architecture | `Docs/Architecture/Backend_Architecture.md` |
| Understand architecture decisions | `Docs/Decisions/` |
| Understand API | `Docs/API/API_Contract_v1.md` |
| Machine-readable API | `Docs/API/openapi.yaml` |
| Understand database | `Docs/Data/Data_Model.md` |
| Understand CI | `Docs/CI_Documentation/CI_Quality_Gates.md` |
| Understand risks | `Docs/Risks/Risk_Register_V2.1.md` |
| Understand AI usage | `Docs/AI_Register/AI_Register.md` |
| Understand deployment | `Docs/Decisions/ADR-007-deployment-direction.md` |
| Understand team process | `Docs/Governance/Team_Working_Agreement.md` |
| Active application | `app/` |
| Older backend implementation | `Backend/` |

---
# 32. Project Direction
The intended direction for the project is to continue building on the newer `app/` architecture and progressively close the gap between the engineering baseline and the implemented product.  
The logical progression is:  
```
Current
  │
  ├── Staff queue
  ├── Request detail
  ├── Assignment
  ├── Authorization
  ├── Controlled lifecycle
  ├── History
  └── Automated verification
        │
        v
Next
  │
  ├── Complete status/note workflows
  ├── Requester experience
  ├── Authentication
  ├── API completion
  ├── Database migrations
  └── Expanded RBAC
        │
        v
Later
  │
  ├── Management reporting
  ├── Administration
  ├── Attachments
  ├── Production deployment
  └── Operational hardening
```

The repository's requirements, RTM, ADRs and risk register remain the authoritative sources for deciding what is required, deferred or formally changed.

---