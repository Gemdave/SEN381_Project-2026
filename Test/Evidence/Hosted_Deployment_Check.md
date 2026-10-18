# Hosted Deployment Check (v1.0.0)

**Site:** https://sen381-project-group-i-2026.onrender.com/ | **Host:** Render Free web service built from the repository's `Dockerfile` (PR #39), database on Neon Free | **Environment:** `Production` | **Run:** 18/10/2026, about 21:35 SAST | **Method:** `curl` from a team machine in South Africa, without signing in

| # | Check | Result | Pass |
| :--- | :--- | :--- | :--- |
| 1 | The site serves HTTPS with a valid certificate | Certificate verified | Pass |
| 2 | Plain HTTP is redirected to HTTPS | 301 to the `https://` address | Pass |
| 3 | HSTS is sent | `strict-transport-security: max-age=2592000` | Pass |
| 4 | The site root sends an anonymous visitor to sign-in | 302 to `/Account/SignIn` | Pass |
| 5 | Private pages need sign-in: `/Requests`, `/Admin/Users`, `/Oversight`, `/Account/ChangePassword` | 302 to sign-in for each | Pass |
| 6 | The API refuses anonymous calls: `GET /api/v1/queue`, `POST /api/v1/requests` | 401 for both | Pass |
| 7 | The development stand-in is ignored: `/?user=...` and `/Home?user=...` | 302 to sign-in; the sign-in page lists no development users | Pass |
| 8 | No server files or tools are exposed: `/appsettings.json`, `/appsettings.Production.json`, `/.env`, `/swagger` and an unknown path | 302 to sign-in for each; nothing served | Pass |
| 9 | The sign-in form carries an anti-forgery token | Hidden `__RequestVerificationToken` present | Pass |
| 10 | Static files load | `/css/site.css` returns 200 | Pass |

**Response times** (site already awake, 20 sequential requests per path, network time included):

| Path | Median | p95 | Max |
| :--- | :--- | :--- | :--- |
| `/Account/SignIn` (page) | 0.590 s | 0.701 s | 0.774 s |
| `/api/v1/queue` (401 path) | 0.657 s | 0.983 s | 0.993 s |

**Result:** 10 of 10 checks pass, and both paths answer well inside the 3 s target of NFR-004.

**Release checks the same evening:** 288 of 288 automated tests pass with 0 skipped; `dotnet format` reports no changes; no vulnerable packages; CI is green on `main` for PR #38 and PR #39.

**Cost (NFR-009):** $0 per month. The team confirmed both plans are Free: Render Free for the web service and Neon Free for the database (ADR-007).

**Does not show:** signed-in journeys or their timing on the host (no account was used for this check), behaviour on a cold start (the free service sleeps after 15 minutes idle and takes about a minute to wake), backup and restore (RSK-015), or resistance to attack (no penetration test).
