# CI Workflow and Quality Gates
## 1. Pipeline summary
| Job | Purpose | Blocking? | Evidence produced |
|---|---|---|---|
| Build and Test | Restore, Release build, run all automated tests | Yes | `test-results` artifact (TRX + coverage XML) |
| Static Analysis | Roslyn analyzers (reported) and `dotnet format` style check | Format check blocks; analyzer warnings reported | `analyzer-output` artifact, run summary |
| Dependency Vulnerabilities | `dotnet list package --vulnerable --include-transitive` | Blocks on High/Critical; Low/Moderate reported | Advisory report in run summary |
| Secrets Scan | gitleaks across full git history | Yes | Job log |
| Quality Gate | Aggregates the four jobs above | Yes - the single required check | Result table plus commit SHA in run summary |

Triggers: pull requests to `main`, pushes to `main`, tags matching `v*` (release candidates) and manual dispatch.

## 2. Gate rule
A change can progress only when **Quality Gate** is green, meaning all four checks passed. A build error, a failing test, a formatting violation, a High/Critical dependency advisory or a detected secret each independently fails the gate and blocks the merge.

## 3. Branch protection settings (GitHub, Settings -> Branches -> `main`)
- Require a pull request before merging, with **2 approvals**.
- Require status check **Quality Gate** to pass; require the branch to be up to date.
- Dismiss stale approvals on new commits.
- Block direct pushes to `main`, including for admins where possible.

## 4. What a green run does and does not show
**Shows:** the code compiles, the automated tests that exist pass, style rules hold, no known High/Critical NuGet advisory is present, and no secret pattern is in the history.

**Does not show:**
- that the tests cover the right behavior (coverage percentage is not correctness);
- that the application works in the staging environment (CI runs on a runner, not the deployed release candidate);
- the absence of vulnerabilities in our own code (the scan checks dependencies, not logic flaws such as broken access control);
- performance under load (covered by the separate performance exercise).

## 5. Release candidate identification
Release candidates are tagged `vX.Y`. The tag push triggers the same pipeline, and the run summary records the commit SHA, which ties the green run to the exact baseline submitted.
