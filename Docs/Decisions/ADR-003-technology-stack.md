# ADR-003 Technology stack
>**Status:** Incomplete. The team has to record its actual choice before this becomes Proposed.  
**Owner:** DevOps, Security and Quality Lead  
**Replaces:** D-003  
**Quality drivers:** ASR-05 first, then ASR-01, ASR-02, ASR-04  
**Risks touched:** RSK-001, RSK-008, RSK-019, RSK-004  

## Why it is urgent

Eighteen of the thirty two RTM rows still read that the technology is pending this record. Nothing can be built without it, and ADR-001, ADR-002 and ADR-007 all assume things about the runtime. RSK-001 and RSK-008 both trace here, and both stay open until this closes.

## What constrains the choice

* **ASR-05 and NFR-009.** It has to run on free or low cost hosting, with the expected running cost written down.
* **RSK-001.** Learning an unfamiliar stack on a fixed schedule is the highest exposure on the register, so existing team skill counts as a criterion.
* **ADR-002.** The store must support transactions and a conditional update whose affected row count is readable.
* **ADR-005.** The backend needs a request pipeline where one authorization check can run before every handler.
* **M1 constraints.** Schedule, compatibility across the stack, and learning curve.

## Settled already: the database

**PostgreSQL:** It gives the transactional behavior ADR-002 depends on, it is available on free and low cost managed plans for NFR-009, it costs nothing to license, and the team has agreed it.

### Rejected  
**MySQL** and **MariaDB**, equal to the task but with no advantage that beats familiarity.  
**SQLite:** because one writer at a time suits a system where staff write concurrently very badly.  
**A document store:** because the domain is plainly relational and the guarantees ASR-01 and ASR-03 ask for are exactly what a relational engine provides.

## Application Stack
### Options 
**A. C#** — Same language end-to-end for backend and browser UI. A single policy-based authorization pipeline. Supports PostgreSQL with readable affected row counts. No cost  
**B. Java** — Needs a second UI toolchain alongside it. A single policy-based authorization pipeline. Supports PostgreSQL with readable affected row counts. No cost  
**C. Node.js** — Requires a separate frontend framework. No built in authorization policy framework. Solid PostgreSQL support but requires more manual wiring. No cost  

### Chosen
Frontend: ASP.NET Core Razor Pages (server-rendered, same runtime and language as the backend), with plain JavaScript fetch for the short-interval status polling recommended for the Requester Portal.
Backend: C# 13 / .NET 10.0 (LTS), ASP.NET Core Web API.
Build tool: the dotnet CLI / MSBuild
Test framework: xUnit.net, with Testcontainers for .NET for integration tests that run against a real PostgreSQL instance.

### Rejected
**Java (B):** Needs a second UI toolchain alongside the backend language.  
**Node.js (C):** Doesn't have a built in authorization policy framework and requires more manual wiring with PostgreSQL.

## Versions and licenses
| Component | Product | Version | License | Evidence |
|---|---|---|---|---|
| Database | PostgreSQL | 17.x | PostgreSQL License (permissive) | postgresql.org release notes; Neon version-support docs, accessed 29 Sept 2026 |
| Database host | Neon (Free plan) | — | Proprietary SaaS, $0 at Free tier | neon.com/docs, "Postgres compatibility", updated 2026-02-02, accessed 29 Sept 2026 |
| Runtime | .NET | 10.0 (LTS) | MIT License | dotnet.microsoft.com/platform/support/policy, updated 8 Sept 2026, accessed 29 Sept 2026; supported until 14 Nov 2028 |
| Web framework | ASP.NET Core | 10.0 | MIT License | ships with the .NET 10 SDK |
| ORM | Entity Framework Core | 10.x | MIT License | ships with the .NET 10 SDK |
| PostgreSQL provider | Npgsql.EntityFrameworkCore.PostgreSQL | current release compatible with EF Core 10 | PostgreSQL License | NuGet package page, to be pinned at implementation start |
| Test framework | xUnit.net | current release compatible with .NET 10 | Apache License 2.0 | xunit.net |
| Integration test tooling | Testcontainers for .NET | current release | MIT License | dotnet.testcontainers.org |
| App hosting | Render (Free tier, Docker) | — | Proprietary SaaS, $0 at Free tier | render.com/articles/platforms-with-a-real-free-tier-for-developers-in-2026, accessed 29 Sept 2026 |
| CI | GitHub Actions | — | Free minutes included with the existing GitHub repository plan | github.com pricing |

**RTM:** FR-001, FR-002, FR-003, FR-004, FR-005, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-015, FR-016, FR-017, FR-018, FR-019, FR-020, FR-021, FR-022, FR-023, NFR-001, NFR-002, NFR-003, NFR-004, NFR-005, NFR-006, NFR-007, NFR-008, NFR-009  
**Depends on:** ADR-001.  
**Blocks:** ADR-007 and all implementation evidence.  
