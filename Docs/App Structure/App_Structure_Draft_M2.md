# CivicConnect application structure, draft for M2

**Purpose:** a starting file structure for the team to build against, scoped to Milestone 2 only.
**Stack:** ASP.NET Core Razor Pages and Web API on .NET 10 LTS (C# 13), PostgreSQL 17.x, per ADR-003.
**Architecture:** one deployable unit with four internal modules, per ADR-001.
**Status:** draft for team review. Nothing here is generated yet.

M2 asks for meaningful development to have begun, not a finished application. The brief does not require a complete app, a mature pipeline, full test coverage or production operations. It does require a repository structure that matches the architecture, working bootstrap configuration on the chosen stack, initial domain code, an initial schema or migration, one working functional path, the design decisions visibly applied, configuration without committed secrets, and initial automated verification. This structure is sized to exactly that.

## Tree

```
CivicConnect.sln
global.json                          pins the .NET SDK so every machine builds the same way
.editorconfig                        shared formatting rules, keeps PR diffs small
src/
  CivicConnect.Domain/               entities and rules, no outward dependencies
    Requests/
      Request.cs                     the aggregate: reference, title, description, category, requester, owner, status
      RequestStatus.cs               controlled vocabulary, not free text (AC-012.1)
      StatusTransition.cs            the allowed moves, the single place they are defined
      RequestHistoryEntry.cs         append only record: actor, action, old value, new value, time
      ActionNote.cs                  staff note, also append only
    Access/
      User.cs, Role.cs, Permission.cs, UserRole.cs
    ReferenceData/
      Category.cs                    controlled list with a retire flag (FR-002, FR-021)
    CivicConnect.Domain.csproj
  CivicConnect.Application/          use cases and service rules, depends on Domain only
    Requests/
      RequestService.cs              submit and read paths (FR-001, FR-003, FR-004)
      AssignmentService.cs           accept or assign, checks the affected row count (FR-011)
      StatusTransitionService.cs     validates the move, writes history in the same transaction (FR-012)
      FeedbackService.cs             requester feedback raised inside that transaction (FR-007)
    Abstractions/
      IRequestRepository.cs
      IHistoryRepository.cs
      IAuthorisationPolicy.cs        the ADR-005 contract, implemented in Infrastructure
    Results/
      AssignmentResult.cs            Assigned or Conflict, so a zero row update can never read as success
    CivicConnect.Application.csproj
  CivicConnect.Infrastructure/       EF Core, PostgreSQL, policy implementation
    Persistence/
      CivicConnectDbContext.cs
      Configurations/                entity configurations, indexes named in the RTM
      Repositories/
        RequestRepository.cs         holds the conditional update from ADR-002
        HistoryRepository.cs         insert and read only, no update or delete path
    Migrations/                      EF Core generated, committed to the repository
    Security/
      AuthorisationPolicy.cs         one decision point, deny by default (ADR-005)
      CapabilityFactory.cs           builds RequestCapabilities for the pages (ADR-004)
    Seed/
      CategorySeed.cs                the controlled category list (pending CR-008)
      DevUserSeed.cs                 development only users, never deployed (RSK-012 contingency)
    CivicConnect.Infrastructure.csproj
  CivicConnect.Web/                  the single deployable: pages, endpoints, wiring
    Program.cs                       service registration, policy registration, routing
    appsettings.json                 non secret settings only, empty connection string placeholder
    appsettings.Development.json     local switches, still no secrets
    Pages/
      Index.cshtml (+ .cshtml.cs)    landing page
      Requests/
        Submit.cshtml (+ .cshtml.cs)     FR-001, FR-002, NFR-001
        Index.cshtml (+ .cshtml.cs)      own history, FR-004, FR-005
        Details.cshtml (+ .cshtml.cs)    own status, read only, FR-003
      Staff/
        Queue.cshtml (+ .cshtml.cs)      FR-008
        Details.cshtml (+ .cshtml.cs)    FR-010 and the handlers for FR-011, FR-012, FR-013
      Shared/
        _Layout.cshtml
        _RequestSummary.cshtml, _RequestDetail.cshtml, _HistoryList.cshtml
        _FilterBar.cshtml, _CategorySelect.cshtml, _EmptyState.cshtml
      Error.cshtml (+ .cshtml.cs)
    ViewComponents/
      StaffQueueViewComponent.cs     queue list with its own query (FR-008, FR-009)
    TagHelpers/
      StatusBadgeTagHelper.cs        status renders in one place only (AC-003.1)
    Endpoints/
      RequestEndpoints.cs            /api/v1/requests, per ADR-006
      AssignmentEndpoints.cs         /api/v1/requests/{id}/assignment
      StatusEndpoints.cs             /api/v1/requests/{id}/status
      NoteEndpoints.cs               /api/v1/requests/{id}/notes
      CategoryEndpoints.cs           /api/v1/categories
    Models/
      RequestCapabilities.cs         what this viewer may do, built on the server (ADR-004)
      RequestSummaryViewModel.cs, RequestDetailViewModel.cs
    wwwroot/
      css/site.css
      js/status-poll.js              the fetch polling ADR-003 chose for NFR-002
    CivicConnect.Web.csproj
tests/
  CivicConnect.Domain.Tests/         transition rules, fast and dependency free
    StatusTransitionTests.cs
  CivicConnect.Application.Tests/    service behaviour with fake repositories
    AssignmentServiceTests.cs, StatusTransitionServiceTests.cs
  CivicConnect.IntegrationTests/     real PostgreSQL, real pages
    TestDatabaseFixture.cs
    ConcurrentAcceptTests.cs         TC-011.2, the M2 demonstration trace test
    RequesterPrivacyTests.cs         TC-N003, negative access test
    PageRenderTests.cs               TC-003.2, no status control on requester pages
db/
  seed/seed_requests.sql             1,000 rows for the NFR-004 measurement
```

## What each project may depend on

| Project | May reference | Must not reference |
| :--- | :--- | :--- |
| Domain | nothing | anything |
| Application | Domain | Infrastructure, Web |
| Infrastructure | Domain, Application | Web |
| Web | Domain, Application, Infrastructure | nothing further |

This is how ADR-001 stops being a diagram and becomes something a reviewer can check. A reference that breaks the table is a review comment, not a preference.

## Build order for Slice 1

Slice 1 is the FR-011 path, which carries the M2 demonstration trace.

1. **Solution and projects**, plus `global.json` and `.editorconfig`. Confirms the stack builds on all three machines.
2. **Domain:** `Request`, `RequestStatus`, `StatusTransition`, `RequestHistoryEntry`, `Category`.
3. **Infrastructure:** DbContext, configurations, first migration, category seed.
4. **Application:** `AssignmentService` with `AssignmentResult`, `StatusTransitionService`.
5. **Infrastructure:** `RequestRepository` with the conditional update, `AuthorisationPolicy`, `CapabilityFactory`.
6. **Web:** endpoints, then `Pages/Staff/Queue` and `Pages/Staff/Details`, then the requester pages.
7. **Tests:** `ConcurrentAcceptTests` first, since it is the evidence the trace needs, then the privacy and page render tests.

## Ownership

| Area | Owner |
| :--- | :--- |
| Domain, Application, Infrastructure, endpoints | K. Marota |
| Pages, partials, view components, tag helper, view models, page and usability tests | M. Malope |
| Program.cs wiring, AuthorisationPolicy, configuration and secrets, deployment config | G. Enright |

Everyone keeps their own RTM rows, risks and AI register entries current in the same pull request as the code.

## Configuration and secrets

No connection string, key or password is ever committed. `appsettings.json` carries an empty placeholder and the real value arrives from outside:

* **Local:** `dotnet user-secrets set "ConnectionStrings:CivicConnect" "..."`, which stores it outside the repository.
* **Deployed:** the `ConnectionStrings__CivicConnect` environment variable in Render, per ADR-007.
* `.gitignore` already covers `.env` files, keys and certificates.
* `DevUserSeed` runs only in the Development environment and is never part of a deployed build. This is the CR-003 contingency, so it must be visibly switched off elsewhere.

## Deliberately absent at M2

Admin pages for FR-019 to FR-022, management oversight beyond a stub, attachments (FR-006, deferred), notification channels beyond in application feedback (conflict C-4), reporting export (pending CR-006), the sponsor view (pending CR-007), caching, a staging environment, and a full CI/CD pipeline. A build and test workflow is optional at M2 and only becomes useful once tests exist, which is RSK-020.

The overdue field for FR-016 stays out until CR-005 is approved, and the reassignment rule in CR-004 must be settled before `AssignmentService` is finished, because it changes the update condition.

## Commands to generate it

```bash
dotnet new sln -n CivicConnect
dotnet new classlib -o src/CivicConnect.Domain
dotnet new classlib -o src/CivicConnect.Application
dotnet new classlib -o src/CivicConnect.Infrastructure
dotnet new webapp -o src/CivicConnect.Web
dotnet new xunit -o tests/CivicConnect.Domain.Tests
dotnet new xunit -o tests/CivicConnect.Application.Tests
dotnet new xunit -o tests/CivicConnect.IntegrationTests
dotnet sln add (ls -r src/*.csproj tests/*.csproj)
```

Then the references, following the dependency table above:

```bash
dotnet add src/CivicConnect.Application reference src/CivicConnect.Domain
dotnet add src/CivicConnect.Infrastructure reference src/CivicConnect.Domain src/CivicConnect.Application
dotnet add src/CivicConnect.Web reference src/CivicConnect.Domain src/CivicConnect.Application src/CivicConnect.Infrastructure
```

Packages and the first migration:

```bash
dotnet add src/CivicConnect.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/CivicConnect.Web package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialRequestSchema --project src/CivicConnect.Infrastructure --startup-project src/CivicConnect.Web
```

Pin the SDK once, so the whole team builds identically:

```bash
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
```

## Definition of done for M2

* The solution builds from a clean clone, and the README says how.
* One migration creates the request, history, category and role tables, and the database can be rebuilt from the repository.
* The FR-011 path works end to end: a staff member accepts a request from a page, ownership and a history row are written in one transaction, and a second attempt is refused.
* `ConcurrentAcceptTests` passes and is the verification evidence in the RTM.
* Requester pages render no status control, and a requester cannot read another requester's request.
* No secret is committed, and the deployed configuration comes from the environment.
* Each requirement touched has its RTM row advanced from Baselined to In Development or Implemented, with the branch, class or test named.
* Issues, branches and reviewed pull requests show progressive work from all three members.
