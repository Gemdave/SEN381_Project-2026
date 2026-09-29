# ADR-006 REST Interface Between the Browser and Backend

> **Status:** Proposed, awaiting team approval
> **Owner:** Systems Architect and Backend Lead
> **Builds on:** D-002 and Task 3 API and integration decisions
> **Quality drivers:** ASR-02, ASR-04, ASR-05, ASR-06
> **Research used:** Assignment 2, Task 3 — APIs and Integration Decisions

## Problem

ADR-001 places the backend between the browser and the database, while ADR-005 makes the backend responsible for access decisions. The system therefore needs a clear interface between the frontend and backend.

The Requester Portal needs to submit and view requests, while the Staff Panel needs to perform actions such as assigning requests and changing their status.

The API also needs to handle validation errors, permission failures, privacy and conflicts in a consistent way.

## Options

**A. REST with JSON over HTTPS.** Resource-based endpoints and standard HTTP methods give the frontend a simple interface. The backend can handle validation and permission checks before any data is changed.

**B. GraphQL.** This could allow the frontend to request different data shapes, but it adds another technology and more setup than the current project requires.

**C. Server-rendered forms.** This would reduce some frontend work, but it does not fit the browser-based design used by the project.

**D. Separate endpoints for every action.** This can work for individual actions, but it would make the API less consistent and harder to maintain as more features are added.

## Decision

**Option A is selected.**

CivicConnect will use a REST API with JSON over HTTPS. API routes will be versioned under `/api/v1`.

The main request operations will include:

* `POST /api/v1/requests` to submit a request.
* `GET /api/v1/requests` to retrieve requests within the caller's allowed scope.
* `GET /api/v1/requests/{id}` to retrieve a specific request.
* `POST /api/v1/requests/{id}/assignment` to assign a request.
* `PATCH /api/v1/requests/{id}/status` to change a request's status.
* `POST /api/v1/requests/{id}/notes` to add a note.
* `GET /api/v1/categories` to retrieve the controlled category list.
* `GET /api/v1/reports/overview` for management reporting within the user's allowed scope.

The backend will validate requests before changing data and will check authorisation before returning protected information or performing protected actions.

The API will use consistent responses:

* `200` for a successful request.
* `201` when a new resource is created.
* `400` when the request is invalid.
* `401` when authentication is required.
* `403` when the user is not allowed to perform the action.
* `404` when the requested resource cannot be found or is outside the caller's allowed scope.
* `409` when the request conflicts with the current state, such as another staff member already assigning the request.
* `422` when the request is understood but fails a business-rule validation.

Validation responses should identify the fields that need correction rather than only returning a general failure.

The API will not return another requester's information. Scope and permission checks will be applied by the backend.

API changes will remain within version 1 when they are backwards compatible. A breaking change or change in meaning will require a new API version and an updated decision record.

## What it costs

* The backend needs to maintain and document the API contract.
* Each endpoint needs its own validation and permission checks.
* Versioning adds some maintenance when the API changes.
* Returning `404` for requests outside the caller's scope can make a missing record harder to diagnose, but it avoids revealing whether another request exists.

## Evidence

**RTM:** FR-001 to FR-005, FR-007 to FR-015, FR-017, FR-021.
**Requirements:** The request submission, validation, assignment, status-change and privacy requirements from Task 3.
**Verification:** Test successful requests, validation failures, permission failures, privacy behaviour, concurrent assignment conflicts and invalid status changes.
**Depends on:** ADR-001, ADR-002, ADR-005. **Feeds:** ADR-004.
