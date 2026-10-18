# ADR-008 Authentication with cookie sign-in over the existing users table
**Status:** Accepted, implemented in release v1.0.0 (18/10/2026)  
**Owner:** DevOps, Security and Quality Lead  
**Replaces:** D-004 for authentication, through change request CR-003  
**Quality drivers:** ASR-02 first, then ASR-05  
**Risks touched:** RSK-012, RSK-005, RSK-014  
**Builds on:** ADR-005 (the authorisation policy stays as it is; this decision only proves who the user is)

## Problem
CR-003 left sign-in open through M3. The application ran only in Development, on a stand-in that let anyone choose who to be, and it refused to start anywhere else. Without real sign-in, NFR-003 (privacy) and NFR-006 (role-based security) could not hold outside a developer's machine, and no production or simulated production environment was possible (blocker B2).

## Options
**A. Cookie authentication over the existing `users` table, with PBKDF2 password hashes.** Reuses the user model, roles and policy already built. No new schema family, no external service.  
**B. ASP.NET Core Identity.** Mature, but brings its own user tables that would duplicate `users` and the role model in ADR-005.  
**C. An external identity provider.** Strongest security features, but needs a tenant and redirect setup the sponsor has not asked for, and a dependency outside the $0 ceiling (NFR-009).  
**D. Emailed magic links.** No passwords to store, but needs an email service the cost ceiling does not cover.

## Decision
**Option A.** Users sign in with an email address and password. Passwords are stored as salted PBKDF2 hashes (default cost 600,000). The cookie is Secure, HttpOnly and SameSite=Lax and proves identity only: the role is read on every request, so a role change or deactivation applies on the user's next action (AC-019.1, AC-020.1). A new account receives a temporary password and must change it at first sign-in. Repeated failures lock the account for 15 minutes. On a fresh database, the first administrator is created from environment variables, never from the repository. Development keeps the stand-in, which refuses to run anywhere else.

## What it costs
* No self-service password reset: an administrator resets it.
* No multi-factor authentication and no breached-password check; the password rule is length-based (12 or more characters) plus a short deny list.
* Lockout can be abused to lock a known account out for 15 minutes.
* The hash cost has not been measured on a free-tier host (RSK-014) and may slow sign-in there.
* The JSON API relies on SameSite=Lax rather than antiforgery tokens; acceptable while the API serves this site only.

## Revisit when
The sponsor names an identity provider, an email service becomes affordable, or the system holds data that justifies multi-factor authentication.

## Verification
* `AppUserSignInTests`, `SignInTests` (application and integration) and `Pbkdf2PasswordHasherTests`: 288 of 288 tests pass at v1.0.0.
* Simulated production, 13 of 13 checks: `Test/Evidence/Final_Simulated_Production.md`.

## Evidence
**Requirements:** FR-019 (AC-019.1), FR-020 (AC-020.1, AC-020.2), NFR-003, NFR-006.  
**Change:** CR-003. **Implementation guide:** `Docs/Requirements/CR-003_Sign_In_Implementation.md`.
