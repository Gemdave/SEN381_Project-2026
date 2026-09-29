# ADR-007
>**Status:** Proposed, awaiting team approval  
**Owner:** DevOps, Security and Quality Lead  
**Quality drivers:** ASR-05 first, then ASR-01, ASR-04  
**Risks touched:** RSK-005, RSK-006, RSK-014, RSK-015, RSK-020  
**Waits on:** ADR-003, since hosting follows the runtime  

## Problem
The milestone asks teams to anticipate deployment without building it out: state the direction, show the architecture and technology choices are plausible for it, name the configuration, secrets, state and networking consequences, and record what is left for later.

What constrains it is the sponsor's cost ceiling (NFR-009), a team of three with no operations background, and two live risks. RSK-005 covers credentials reaching a public repository, RSK-006 covers code that runs locally and fails elsewhere.

## Options
**A. A managed platform on a free or hobby plan with a managed PostgreSQL instance.** Deploy from the repository, the platform handles certificates and processes, backups come with the database. Least operational work, and it fits the cost ceiling.

**B. A container on a virtual machine the team runs.** Closer parity between environments, which speaks to RSK-006, but it hands the team the operating system, certificates, backups and patching. Work with no marks attached, competing with construction time.

**C. Serverless functions.** Cheap, but start up delays threaten NFR-004, connection limits complicate ADR-002, and it contradicts the single unit in ADR-001.

**D. Nothing deployed.** Permitted, since production deployment is not required, but it leaves every deployment assumption untested and RSK-006 untouched just as M3 starts depending on it.

## Decision
**Option A**, split across two managed free plans rather than one, because ADR-003 already established that the two best free tiers for this stack don't come from the same provider:  
* **Compute:** Render, Free tier — the ASP.NET Core app runs as a Docker container built by CI and deployed from the `main` branch.  
* **Database:** Neon, Free plan — a managed PostgreSQL 17.x instance, reached over a connection string, not co-located with the app.  

## Rejected
* **B. Self-run VM:** A three-person team with no operations background would have to own OS patching, certificate renewal and backup scheduling themselves, work that carries no assessment marks and directly competes with construction time. The closer environment parity it offers doesn't outweigh that cost at this scale.  
* **C. Serverless functions:** Cold-start latency on function invocation threatens the staff-view responsiveness target (NFR-004), and per-invocation connection limits complicate the transactional conditional-update pattern the persistence decision depends on. It also splits the backend into multiple deployable units, which conflicts with the single-application architecture already chosen.  
* **D. Nothing deployed:** Production deployment isn't mandatory for this milestone, but skipping it leaves every assumption about how the application behaves outside a developer's machine completely untested. The next milestone's work depends on a working deployment path already existing, so deferring it further only shifts the risk forward at a worse time to absorb it.  

## Positions recorded
* **State.** The application itself is stateless between requests; all durable state lives in PostgreSQL on Neon. No local files, in-memory caches, or session state are relied on, so a Render restart or a scale-to-one-instance limit (Free tier does not offer more than one instance) loses nothing. Any short-interval polling state (per the ADR-003/A2 Task 3 decision) lives entirely on the client.
* **Secrets.** The database connection string, and any future signing key from ADR-005, are supplied to the running container through Render's environment variable settings, never committed to the repository. `.gitignore` already excludes `.env*` and key/certificate files (RSK-005). GitHub secret scanning and push protection are to be enabled on the repository before the first real credential is created — this remains an open action against RSK-005 and RSK-020.
* **Networking.** Render terminates TLS for the app's public URL and issues the certificate automatically; no certificate management falls to the team. The connection from Render to Neon runs over TLS using Neon's provided connection string (`sslmode=require`). No inbound access to the database is opened beyond what Neon's connection pooling endpoint requires; the database is never exposed with a public port of its own.
* **Backups.** Neon's Free plan does not include a paid backup product, but its branching model gives a practical substitute: a manual branch (a point-in-time copy) can be taken before any schema migration or risky data change, at no cost, and deleted afterward. This is weaker than a scheduled managed backup and is recorded as an accepted gap at this scale (see RSK-015), not as backup coverage.
* **Availability.** Render's free web service spins down after 15 minutes of inactivity and takes about a minute to restart on the next request. This is accepted for a coursework deployment; the team notes it explicitly ahead of any live demonstration and plans to send a warm-up request a few minutes beforehand. Neon's free compute similarly scales to zero when idle and resumes on the next query, with a comparable short delay. Both are judged acceptable because NFR-002's 15-second feedback target applies to an already-active session, not to cold start.
* **Cost.** $0/month at this project's scale (see "What it costs").
* **Performance.** Render Free gives 512 MB RAM and 0.1 vCPU to the app; Neon Free gives 1 GB RAM shared compute to the database. Neither figure is generous, but NFR-004's target is staff queue and oversight views loading quickly under the load a coursework demonstration produces, not production-scale concurrency, so this is judged sufficient. If load testing later shows otherwise, Render's Starter tier is the documented escalation path.

## What it costs
**$0/month**, using:
* Render Free — 750 instance hours/month, 512 MB RAM / 0.1 vCPU, 100 GB outbound bandwidth, 500 build-pipeline minutes.
* Neon Free — persistent PostgreSQL, 3 GiB storage per branch, 1 GB RAM shared compute, no expiry.
* GitHub Actions — build/test minutes included with the repository's existing plan.
**Paid escalation, if a limit is exceeded before the deadline:** Render Starter (USD 7/month) removes the spin-down behavior; Neon's next tier bills USD 0.106 per compute-hour beyond the free allowance. Neither is expected to be needed at this project's traffic level, and switching tiers requires no code change, only a billing decision — this is the concrete contingency for RSK-014.

## Left for later, and what would trigger it
* **A scheduled, provider-managed backup product** — triggered if the project continues past the course (Neon's paid tiers include point-in-time recovery), or if a real data-loss incident occurs on the free tier.
* **A custom domain and stricter CORS/network policy** — not needed while the deployment is a coursework demonstration reached at its Render-provided URL; triggered if the sponsor asks for a durable, branded address.
* **Horizontal scaling or a second instance** — triggered only if load testing under ASR-04 shows the single free-tier instance is insufficient; Option B or a paid Render tier would be revisited at that point, not before.
* **Secret rotation tooling** — a manual rotation process is sufficient at this scale; a managed secrets vault is deferred unless the team's credential count grows enough to make manual tracking unreliable.
* **Containerizing for local development parity** — a Dockerfile is needed for the Render deployment itself; whether the team also runs that same container locally (rather than the .NET SDK directly) is left open and should be decided once the first build slice exists, per RSK-006's mitigation.

## Verification
* A deployed instance of the first build slice, reachable at a Render URL, connecting successfully to the Neon database over TLS, is the concrete evidence this decision was followed — not just documented.
* No secret appears in the repository history: confirmed by GitHub secret scanning being enabled with zero alerts, checked before baseline sign-off (closes the open action under RSK-005/RSK-020).
* A restart of the Render service (triggered manually, or by the free tier's own spin-down) is observed to lose no data and require no manual recovery step, confirming the "State" position above.
* The environment variable list actually required to run the app is documented in the README and matches what Render's dashboard has configured — no undocumented configuration key.

## Evidence
**RTM:** NFR-004, NFR-009, and every implementation row depends on this.  
**Depends on:** ADR-001, ADR-003 (Proposed).  