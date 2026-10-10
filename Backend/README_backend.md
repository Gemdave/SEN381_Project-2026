# CivicConnect backend (Milestone 2, Build Slice 1)

A .NET 10 web API for the CivicConnect service-request platform. This first slice covers submitting requests, the requester's history and feedback, the staff queue, accepting a request, controlled status changes and action notes.

**Status:** In development. It compiles and its unit tests pass for the rules layer. Sign-in is a development-only placeholder (see below), so nothing here is ready for real users.

## What is where

| Folder | What it holds |
| :-- | :-- |
| `src/CivicConnect.Core` | Business rules and services. No database or web code. |
| `src/CivicConnect.Data` | PostgreSQL access (Npgsql), transactions, SQL migrations |
| `src/CivicConnect.Web` | API controllers, error handling, startup |
| `tests/CivicConnect.Tests` | Unit tests (in-memory fake database) and real-database tests |
| `db/dev_seed.sql` | Sample users for local work only |
| `Docs/` | Architecture, data model, API contract (`openapi.yaml`), PED sections |

Decisions behind this: ADR-001 (architecture), ADR-002 (data integrity), ADR-003 (stack), ADR-006 (API). Requirement links are in the RTM v2.0.

## Versions

.NET 10 LTS (C# 13), ASP.NET Core, PostgreSQL 17.x, Npgsql 10.0.3, xUnit 2.9.3.

## Run it locally

1. Install the .NET 10 SDK and PostgreSQL 17, and create an empty database, e.g. `civicconnect_dev`.
2. Give the app its connection string through the environment. Do not put it in a file that gets committed.

   ```bash
   export ConnectionStrings__CivicConnect="Host=localhost;Database=civicconnect_dev;Username=postgres;Password=<yours>"
   ```
3. Start it. In Development the tables are created automatically.

   ```bash
   dotnet run --project src/CivicConnect.Web
   ```
4. Add the sample users once: `psql "<same connection>" -f db/dev_seed.sql`
5. Call the API with a sample user (Development only):

   ```bash
   curl -H "X-Dev-User: requester1@civicconnect.test" http://localhost:5080/api/v1/categories
   ```

## Run the tests

```bash
dotnet test
```

The real-database tests are skipped unless you point them at a throwaway database:

```bash
export CIVICCONNECT_TEST_DB="Host=localhost;Database=civicconnect_test;Username=postgres;Password=<yours>"
dotnet test
```

Use a scratch database, because history rows cannot be deleted by design.

## Configuration

| Setting | Purpose |
| :-- | :-- |
| `ConnectionStrings__CivicConnect` | Database connection (required, from the environment) |
| `Database__RunMigrations` | `true` applies pending migrations at startup. On in Development, off otherwise. |

No secrets are committed. `.gitignore` should also cover `appsettings.*.local.json` and `.env`.

## Known limitations

* Sign-in is the `X-Dev-User` header, and only in Development. Real authentication follows ADR-005 / CR-003.
* No manager, administrator or sponsor endpoints yet.
* Staff see unassigned requests and their own; team/category restrictions are not modelled.
* The category list is a starting set awaiting confirmation (CR-008).
* The `Rejected` status depends on proposed CR-009.
* Data-layer and web code has not yet been run against a live PostgreSQL by the author of this slice. Run the real-database tests first.

## Suggested branches and PRs

`feature/fr-001-submit`, `feature/fr-011-assign-accept`, `feature/api-contract`, `docs/ped-v2-backend`. Each needs the two independent approvals set in the working agreement.
