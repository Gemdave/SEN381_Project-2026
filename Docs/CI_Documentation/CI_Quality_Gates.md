# CI Workflow and Quality Gates
**Workflow file:** `.github/workflows/ci.yml` | **Version:** 2.0 | **Owner:** G. Enright | **Relates to:** RSK-005, RSK-019, RSK-020, RSK-023

## 1. Pipeline summary
| Job | Purpose | Blocking? | Evidence produced |
|---|---|---|---|
| Build and Test | Restore, Release build, run every test project against a PostgreSQL service container, count passed, failed and skipped tests | Yes | `test-results` artifact (one TRX file per test project, plus coverage XML once `coverlet.collector` is installed) and a Passed/Failed/Skipped table in the run summary |
| Static Analysis | Roslyn analyzers (reported) and `dotnet format --verify-no-changes` style check | Format check blocks; analyzer warnings are reported only | `analyzer-output` artifact, run summary |
| Dependency Vulnerabilities | `dotnet list package --vulnerable --include-transitive` | Blocks on High/Critical; Low/Moderate reported | Advisory report in run summary |
| Secrets Scan | gitleaks across the full git history | Yes | Job log |
| Quality Gate | Aggregates the four jobs above | Yes - the single required check | Result table plus commit SHA in run summary |

Triggers: pull requests to `main`, pushes to `main`, tags matching `v*` (release candidates, for example `v3.1`) and manual dispatch. A newer run on the same branch cancels an older one, so the latest commit is the one judged.

## 2. Gate rule
A change can progress only when **Quality Gate** is green, meaning all four checks passed. Each of these independently fails the gate and blocks the merge:  
- a build error or a failing test;
- **any skipped test** (see section 4);
- a formatting violation against `.editorconfig`;
- a High or Critical dependency advisory;
- a detected secret.

## 3. Branch protection settings (GitHub, Settings -> Branches -> `main`)
- Require a pull request before merging, with **2 approvals** from people other than the author.
- Require status check **Quality Gate** to pass, and require the branch to be up to date.
- Dismiss stale approvals when new commits are pushed.
- Block direct pushes to `main`, including for administrators where possible.

These settings are configured in GitHub, not in the workflow file, so the evidence is a screenshot of the rule or a pull request showing the required check.

## 4. Test database in CI
The integration, black-box and end-to-end tests need a real PostgreSQL database because they test database behavior (concurrent ownership, insert-only history, constraints, rollback).  
| Item | Setup |
|---|---|
| Database | A `postgres:17` service container started for the Build and Test job only |
| Connection | `CIVICCONNECT_TEST_DB` is set at job level to `Host=localhost;Port=5432;Database=civicconnect_test;Username=postgres` |
| Authentication | Trust authentication, so **no password exists anywhere in the repository** (RSK-005). The container is disposable, lives only for the job and is reachable only from the runner |
| Lifecycle | The test fixture drops and recreates `civicconnect_test` from the real migrations at the start of the run |
| Never reuse | This setup must never be used for a real or shared database |

**Skipped tests fail the build.** The integration tests skip themselves when `CIVICCONNECT_TEST_DB` is missing, which would leave a green run that proves nothing. The "Verify no tests were skipped" step counts `NotExecuted` results in the TRX files and fails the job if there are any. A skipped test is reported as blocked, never as passed.

For local runs, see `app/db/TEST_DATABASE.md`.

## 5. What a green run does and does not show
**Shows:** the code compiles, the automated tests that exist all ran and passed (none skipped), style rules hold, no known High/Critical NuGet advisory is present, and no secret pattern is in the history.

**Does not show:**
- that the tests cover the right behavior; a coverage percentage, when available, shows which lines ran and not that the assertions are correct;
- that the application works in the staging environment, because CI runs on a runner and not on the deployed release candidate;
- the absence of vulnerabilities in our own code, because the scan checks dependencies and not logic flaws such as broken access control;
- real authentication, because the tests use the development identity stand-in (RSK-012);
- performance under load; the CI timing check is only a regression guard, and the performance exercise is run separately on staging.

## 6. Release candidate identification
Release candidates are tagged `vX.Y`. The tag push triggers the same pipeline, and the run summary records the commit SHA, which ties the green run to the exact baseline submitted. The same tag and commit are recorded in the PED submission block and in the staging deployment (`/version`).

## 7. Run evidence
| Item | Value |
|---|---|
| Release candidate tag | v3.0 |
| Commit | 6a85684 |
| Actions run | https://github.com/Gemdave/SEN381_Project-2026/actions/runs/38705719178 |
| Date | 14/10/2026 |
| Build and Test | pass; 44 of 44 executed cases passed, 0 failed, 0 skipped |
| Static Analysis | Pass |
| Dependency Vulnerabilities | Pass |
| Secrets Scan | Pass |
| Quality Gate | Pass |
| Test results artifact | Test\CI-Artifacts\test-results.trx |
| Coverage report | not produced |
| Gate blocking a change | none |

## 8. Known limitations

- Analyzer warnings are reported but not blocking; tightening to warnings-as-errors is a candidate Technical Debt Register item.
- The secrets scan uses gitleaks' default rules with no custom allowlist; any finding must be read and resolved, not silenced.
- gitleaks is free for repositories owned by a personal account; a repository moved to an organization would need a license key.
- The Quality Gate cannot enforce review rules; those are the branch protection settings in section 3.