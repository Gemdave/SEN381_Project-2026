# ADR-007 One deployable unit with a managed database on a low cost plan

**Status:** Proposed, awaiting team approval
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



## Positions recorded

* **State.** 
* **Secrets.** 
* **Networking.** 
* **Backups.** 
* **Availability.** 
* **Cost.** 
* **Performance.** 

## What it costs



## Left for later, and what would trigger it



## Verification



## Evidence

**RTM:** NFR-004, NFR-009, and every implementation row depends on this.
**Depends on:** ADR-001, ADR-003.
