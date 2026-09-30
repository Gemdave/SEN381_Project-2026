# CivicConnect Architecture Decision Records

One file for each significant decision. Every record states the problem before it names a solution, lists the options that were genuinely considered, records what was chosen, and states the cost that choice brings.

## Index

* **ADR-001** [Layered modular monolith](ADR-001-architecture-style.md). Architecture. Keletso Marota. Proposed.
* **ADR-002** [Conditional update and append only history](ADR-002-persistence-and-data-integrity.md). Data and persistence. Keletso Marota. Proposed.
* **ADR-003** [Technology stack](ADR-003-technology-stack.md). Technology. Gerald Enright. **Incomplete.**
* **ADR-004** [Role based view composition](ADR-004-role-based-view-composition.md). **Design pattern 1.** Mogau Malope. Proposed.
* **ADR-005** [Central authorisation policy](ADR-005-rbac-authorisation-policy.md). **Design pattern 2.** Gerald Enright. Proposed.
* **ADR-006** [REST interface contract](ADR-006-api-interface-contract.md). Interface and integration. Keletso Marota. Proposed.
* **ADR-007** [Deployment direction](ADR-007-deployment-direction.md). Deployment. Gerald Enright. Proposed.

The two design pattern decisions the milestone 2 asks for are ADR-004 and ADR-005.

## Status values

* **Proposed.** Drafted and ready for review. Not binding.
* **Accepted.** Approved by the team and part of the baseline.
* **Superseded by ADR-00X.** Replaced. The old record stays where it is, with the reason for the change.
* **Deprecated.** No longer applies and nothing replaced it.

## Superseded M1 decisions

* **D-001** separate interface, backend and persistence layers. Carried forward and made concrete by ADR-001.
* **D-002** central backend for application operations. Carried forward by ADR-001 and ADR-006.
* **D-003** defer framework and database selection. Superseded by ADR-002 for the database and ADR-003 for the stack.
* **D-004** defer authentication and authorisation. Superseded by ADR-005, through change request CR-003.
