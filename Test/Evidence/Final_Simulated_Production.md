# Final Simulated Production Check (release v1.0.0)

**Purpose:** evidence for the final go-live decision (PED v4.0, Final Deployment & Go-Live Decision). It shows the release running in a production-like environment, separate from development, before any hosted deployment exists.  
**Date:** 18/10/2026, 18:57 SAST  
**Build:** `SignIn-Module`, the content tagged as `v1.0.0` after merge  
**Result:** 13 of 13 checks passed

## Environment
| Setting | Value |
| :--- | :--- |
| Environment | `Production` (`ASPNETCORE_ENVIRONMENT`), launch profile disabled |
| Address | `https://localhost:7001`, HTTPS only (development certificate) |
| Database | A new, empty PostgreSQL 17 database (`civicconnect_prodsim`), separate from the development and test databases |
| Secrets | Connection string and first-administrator password passed as environment variables only; nothing written to the repository, and the passwords were random and never displayed |
| Startup | `Database__MigrateOnStartup=true` and `Bootstrap__AdminEmail/Name/Password` (see `Docs/Requirements/CR-003_Sign_In_Implementation.md`, section 4) |

## Checks
| # | Check | Result |
| :--- | :--- | :--- |
| 1 | The site root sends an anonymous visitor to the sign-in page | Pass (302 to `/Account/SignIn`) |
| 2 | A private page is not served without signing in | Pass (302) |
| 3 | The API refuses an anonymous call | Pass (401) |
| 4 | The development stand-in (`?user=`) is ignored | Pass (302) |
| 5 | A wrong password is refused | Pass |
| 6 | The bootstrapped administrator signs in and must change the temporary password | Pass |
| 7 | The sign-in cookie is Secure, HttpOnly and SameSite=Lax | Pass |
| 8 | Other pages wait until the temporary password is changed | Pass (302 to `/Account/ChangePassword`) |
| 9 | After the change, the administrator reaches Users and roles | Pass |
| 10 | No development users exist in the production database | Pass |
| 11 | Startup created the schema (6 migrations), the 7 controlled categories, one administrator and no sample data | Pass |
| 12 | The stored password hash does not contain the password | Pass |
| 13 | The application log records the startup step | Pass |

The full automated suite passed on the same build: 287 of 287 (89 domain, 92 application, 106 integration).

## What this does and does not show
**Shows:** the release starts and runs with production settings, secrets stay outside the repository, real sign-in protects every page and the API, and a fresh database is prepared without development data.  
**Does not show:** behaviour on a hosted server (no container or hosting yet, TD-04), free-tier limits or hash cost on that server (RSK-014), backup and restore (RSK-015), or load beyond PT-01.
