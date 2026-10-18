# CR-003 — Real Sign-In: Implementation Guide

> **Purpose:** closes CR-003 / RSK-012 / TD-02 by replacing the development stand-in with real email-and-password sign-in outside Development.
> **Status:** written and reviewed by reading only. **It has not been compiled or run** (no .NET SDK was available where it was produced). Follow section 2, and treat the first `dotnet build` and `dotnet test` as the real verification.

---

## 1. What it does

| Behaviour | How |
| :--- | :--- |
| Sign in with email + password | `/Account/SignIn`, cookie session `cc.auth` (HttpOnly, Secure, SameSite=Lax, 8 h sliding, not persistent) |
| Everything private by default | ASP.NET fallback policy requires a signed-in user; only sign-in, error and the site root opt out |
| API answers 401/403, pages redirect | cookie events, for paths under `/api` |
| Passwords stored only as salted hashes | PBKDF2-HMAC-SHA256, 600,000 iterations, 16-byte salt, constant-time compare (`v1.iterations.salt.key`, so cost can be raised later) |
| Same refusal for unknown email, wrong password, deactivated account | AC-019.1; a dummy hash is computed for unknown emails so timing does not reveal them |
| Lockout | 5 wrong passwords in a row lock the account for 15 min; a successful sign-in or an administrator reset clears it |
| Deactivation takes effect at once | the cookie is re-checked against the database on every request (`OnValidatePrincipal`) |
| Administrator creates users with a temporary password | Admin → Users; the user **must** change it at first sign-in and can do nothing else until then |
| Administrator resets a forgotten password | clears lockout, ends the user's sessions, audited (the password is never audited) |
| Changing a password ends the user's other sessions | `SecurityStamp` rotates; the current browser is signed in again |
| First administrator on a fresh database | `ProductionBootstrap`, from environment variables, never from the repository |
| Roles unchanged | the cookie carries only the user id; the role is still read from the database per action (ADR-005, AC-020.1) |
| Development unchanged | Development keeps the stand-in, so the existing 211 tests are not touched |

Not included on purpose (listed as limitations in section 5): self-service password reset, MFA, breached-password checking, IP rate limiting, self-registration.

---

## 2. Apply it

1. **Copy the files** from the zip over the repository (paths are repo-relative). Overwrites 13 existing files and adds 19 — the list is in section 3. If a teammate changed any of the 13 since your upload, merge rather than overwrite.
2. **Create the migration** from the `app/` folder (the connection string must be available the way it was for the earlier migrations, e.g. user secrets):
   ```
   dotnet ef migrations add AddSignIn --project src/CivicConnect.Infrastructure --startup-project src/CivicConnect.Web
   ```
   Open the generated `Up` and check it only **adds** to `users`: `Email` (varchar 254, null), `PasswordHash` (varchar 255, null), `MustChangePassword` (bool, default false), `FailedSignIns` (int, default 0), `LockedUntil` (timestamptz, null), `SecurityStamp` (uuid), and a **unique index** on `Email`. Anything that drops or alters other tables means the model snapshot is out of step — stop and ask.
3. **Build and run all tests** (the integration tests need `CIVICCONNECT_TEST_DB`, as in `app/db/TEST_DATABASE.md`):
   ```
   dotnet build
   dotnet test
   ```
   Expected: the old 211 still pass, plus the new ones (section 6). Fix anything red before going further.
4. **Run real sign-in locally** (Development always uses the stand-in, so use Staging). User secrets are **not** read outside Development, so pass the connection string as an environment variable, and use HTTPS because the cookie is Secure:
   ```
   dotnet dev-certs https --trust
   $env:ASPNETCORE_ENVIRONMENT = "Staging"
   $env:ConnectionStrings__CivicConnect = "Host=localhost;Port=5432;Database=civicconnect_local;Username=postgres;Password=<yours>"
   $env:Database__MigrateOnStartup = "true"
   $env:Bootstrap__AdminEmail = "admin@civicconnect.test"
   $env:Bootstrap__AdminName = "Local Administrator"
   $env:Bootstrap__AdminPassword = "a temporary passphrase"
   dotnet run --project src/CivicConnect.Web --urls "https://localhost:7001"
   ```
   Sign in as the bootstrap administrator → you are sent to *Change password* → choose your own → Admin → Users → create a requester with a temporary password → sign in as them in a private window.
5. **Commit** on a branch, open a PR, let CI run (it runs the new tests and the secret scan).

---

## 3. Files

**Changed (13):** `AppUser.cs`, `IUserRepository.cs`, `UserService.cs`, `CivicConnectDbContext.cs`, `UserRepository.cs`, `Program.cs`, `Index.cshtml.cs`, `Shared/_Layout.cshtml`, `Admin/Users.cshtml`, `Admin/Users.cshtml.cs`, `wwwroot/css/site.css`; tests: `Fakes.cs`, `UserServiceTests.cs`.

**New (19, not counting the migration and this guide):**
- Application: `Abstractions/IPasswordHasher.cs`, `Access/PasswordPolicy.cs`, `Access/SignInService.cs`
- Infrastructure: `Security/Pbkdf2PasswordHasher.cs`, `Seed/ProductionBootstrap.cs`
- Web: `Security/CookieCurrentUser.cs`, `Security/SignInSetup.cs`, `Pages/Account/SignIn`, `ChangePassword`, `SignOut` (each `.cshtml` + `.cshtml.cs`), `Pages/Error.cshtml` + `.cs`
- Tests: `Application.Tests/SignInTests.cs`, `Domain.Tests/AppUserSignInTests.cs`, `IntegrationTests/Pbkdf2PasswordHasherTests.cs`, `IntegrationTests/SignInTests.cs`
- Migration: generated by you in step 2

(`Error.cshtml` is included because `Program.cs` already pointed unhandled errors at `/Error` outside Development but no such page existed.)

---

## 4. Configuration reference (Render / any non-Development host)

| Setting | Value | Notes |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` (or `Staging`) | anything but `Development` |
| `ConnectionStrings__CivicConnect` | Neon connection string (`sslmode=require`) | host env var only (RSK-005) |
| `Database__MigrateOnStartup` | `true` for the **first** deploy, then `false` | take a Neon branch first (ADR-007 backup position) |
| `Bootstrap__AdminEmail` / `__AdminName` / `__AdminPassword` | first administrator | password ≥ 12 characters; **remove `AdminPassword` after the first sign-in** |
| `Authentication__Pbkdf2Iterations` | optional, default 600000, floor 100000 | see the cost warning below |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` | Render terminates TLS in front of the container |

**Cost warning:** ADR-007 records Render Free as 0.1 vCPU. A 600,000-iteration PBKDF2 sign-in may take a few seconds there. Measure it on the real host. If it is unacceptable, lower the setting (not below 100,000) and **record that as an accepted trade-off** in the risk register rather than quietly weakening it. Hashes carry their own cost, so changing it later is safe.

---

## 5. Design decisions and honest limitations

- **Why not ASP.NET Core Identity or an external provider?** Identity brings its own user schema that would duplicate `users`; an external identity provider needs a tenant and redirect setup the sponsor has not asked for; email-based flows (magic link, self-service reset) need an email service the cost ceiling (NFR-009) does not cover. Draft ADR-008 below records this.
- **No self-service password reset.** An administrator resets it. Fine for a coursework-scale deployment; a real service needs emailed reset links.
- **Lockout can be abused** to lock a known account out for 15 minutes. Accepted at this scale; the administrator reset clears it.
- **No MFA, no breached-password check, no IP rate limiting.** The password rule is length-based (≥ 12) plus a short deny list (NIST SP 800-63B style).
- **CSRF:** Razor Pages forms carry antiforgery tokens. The JSON API relies on `SameSite=Lax` (cookies are not sent on cross-site POSTs) — acceptable here, but add antiforgery to the API if it is ever opened to other origins.
- **Temporary passwords** should be shared in person or by phone, never by email. The create/reset screens say so.
- **Messages:** a locked account says so, which tells an attacker the email exists. Accepted to avoid leaving a real user confused.
- **Development still uses the stand-in.** That is deliberate (the test suite depends on it), and it refuses to run anywhere else. If the team wants real sign-in exercised locally by default, that is a follow-up change.
- **Seeded data:** the development seed is never run outside Development. A deployed database gets only the 7 controlled categories and one administrator.

---

## 6. Tests added (evidence for the RTM)

| Where | Evidence for |
| :--- | :--- |
| `Domain.Tests/AppUserSignInTests` | email validation/normalisation; credentials; lockout rule; security stamp rotation |
| `Application.Tests/SignInTests` | password policy; sign-in success/refusal (same refusal for unknown, wrong, deactivated); lockout and its expiry; must-change flag; change-password rules and session ending; admin create/reset; only admins may |
| `IntegrationTests/Pbkdf2PasswordHasherTests` | round trip; salted; never contains the password; older cost still verifies; malformed hashes never verify or throw |
| `IntegrationTests/SignInFlowTests` (non-Development host, needs the test DB) | everything private until sign-in (pages 302, API 401); stand-in unreachable (`?user=` ignored); no user enumeration; cookie flags; safe return URL; sign-out; lockout through the UI; **deactivated user loses the session at once (AC-019.1)**; forced password change; change ends other sessions; admin create/reset; non-admin refused; bootstrap gives categories + one administrator and **no development data** |

---

## 7. Records to update

**CR-003 (change register):** *Approved and implemented.* Cookie sign-in with PBKDF2 password hashes replaces the development stand-in outside Development; roles unchanged (ADR-005). Evidence: section 6 tests, CI run on the final commit `<sha>`.

**RSK-012 (risk register):** mitigation implemented; residual risk now = no MFA, no self-service reset, lockout abuse, free-tier hash cost. Re-rate after the first green CI run — do not mark closed before that.

**TD-02:** repaid. **TD-04:** unchanged until the Dockerfile and deployment exist.

**RTM:** add the section 6 tests against FR-019 (AC-019.1) and FR-020 (AC-020.1), and record CR-003 as the change reference.

**Draft ADR-008 — Authentication mechanism** (*edit before use; status Proposed until the team approves*):
- *Problem:* CR-003 left sign-in open; the application refuses to start outside Development without it.
- *Options:* A cookie authentication over the existing `users` table with PBKDF2 hashes · B ASP.NET Core Identity · C external identity provider · D emailed magic links.
- *Decision:* A. Reuses the existing user model and policy; no new schema family; no external dependency or email service within the $0 ceiling (NFR-009).
- *Consequences:* administrator-mediated reset, no MFA, a password-hash cost that must be measured on the free host.
- *Revisit when:* the sponsor names an identity provider, an email service becomes available, or the system holds data that justifies MFA.

**AI Usage Register (yours to complete):** record that this sign-in design and code were generated with Claude, what you reviewed and changed, what was verified by running (nothing until you build and test), and who accepted it. Fill in the real facts.

---

## 8. Likely defence questions

- *Why is the role not in the cookie?* So a role change or deactivation applies on the next action (AC-020.1, AC-019.1); the cookie proves identity only.
- *What happens to a user deactivated mid-session?* The cookie is rejected on their next request (test: "loses the session at once").
- *How do you know passwords are not stored?* Hash format `v1.cost.salt.key`; tests assert the stored value never contains the password; audit entries never contain it.
- *What would you add next?* Emailed reset, MFA, breached-password check, API antiforgery/rate limiting, then retire the stand-in from local development.
- *What does this not prove?* No penetration test was done; the tests exercise the intended behaviour, not an attacker.
