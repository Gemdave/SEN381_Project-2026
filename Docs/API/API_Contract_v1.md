# CivicConnect API Contract v1 (Build Slice 1)

**Owner:** K. Marota
**Supports:** ADR-006 (REST contract, validation, errors, versioning), ADR-005 (security boundary)
**Machine-readable copy:** `openapi.yaml` in this folder
**Status:** Proposed for the M2 baseline. Matches the code in `CivicConnect.Web/Controllers`.

---

## 1. Style

REST over HTTPS with JSON. GraphQL was not used: there are no outside consumers, the surface is small, and REST is what the team already knows (RSK-001). The requester portal learns about status changes by polling `GET /api/v1/requests/{id}` and `GET /api/v1/feedback` every 10 seconds or less, which keeps us inside NFR-002 (15 seconds) without any push infrastructure.

| Topic | Rule |
| :-- | :-- |
| Base path | `/api/v1` |
| Versioning | Version lives in the path. Adding a field is not a breaking change. Removing or renaming one, or changing its meaning, means `/api/v2`. |
| IDs | Requests use a UUID `id` plus a readable `reference` such as `CC-2026-000123`. |
| Times | ISO 8601 in UTC. |
| Enums | Sent as strings: `Received`, `Assigned`, `InProgress`, `Resolved`, `Closed`, `Rejected`. A `statusText` field carries the plain-language version ("In Progress"). |
| Paging | `page` (from 1, default 1) and `pageSize` (1 to 50, default 20). Lists return `items`, `page`, `pageSize`, `total`. |
| Authentication | Every endpoint needs a signed-in user. Until ADR-005 / CR-003 is implemented, Development builds accept an `X-Dev-User: <email>` header and nothing else is accepted. |
| Authorisation | Checked on the server for every call (NFR-006). The screen hiding a button is never the control. |

## 2. Endpoints

| Method | Path | Permission | Purpose | Requirement |
| :-- | :-- | :-- | :-- | :-- |
| GET | `/api/v1/categories` | signed in | Active categories for the form | FR-002 |
| POST | `/api/v1/requests` | `request.submit` | Submit a request | FR-001 |
| GET | `/api/v1/requests` | `request.view_own` | My requests, filter by `status` | FR-004, FR-005 |
| GET | `/api/v1/requests/{id}` | `request.view_own` or `request.view_detail` | One request. Staff also get history and notes. | FR-003, FR-010 |
| GET | `/api/v1/requests/queue` | `request.queue` | Staff queue with filters and sorting | FR-008, FR-009 |
| POST | `/api/v1/requests/{id}/assignment` | `request.assign` | Accept a request | FR-011 |
| PATCH | `/api/v1/requests/{id}/status` | `request.update_status` (+ `request.close` to close) | Move to the next status | FR-012, FR-014 |
| POST | `/api/v1/requests/{id}/notes` | `request.add_note` | Add a note | FR-013 |
| GET | `/api/v1/feedback` | `request.view_own` | My feedback items | FR-007 |
| POST | `/api/v1/feedback/{id}/read` | `request.view_own` | Mark one as read | FR-007 |
| GET | `/health` | none | Liveness check for the host | ADR-007 |

## 3. Error format

Every error is RFC 9457 `application/problem+json`, the same shape ASP.NET Core produces natively.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more fields need attention.",
  "status": 400,
  "errors": {
    "title": ["Give the request a short title."],
    "categoryId": ["Pick a category from the list."]
  }
}
```

| Status | When | Notes |
| :-- | :-- | :-- |
| 400 | Validation failed or a query parameter is malformed | `errors` names each field |
| 401 | No signed-in user | |
| 403 | Signed in but not allowed | |
| 404 | Not found **or not yours** | A requester asking for someone else's reference gets 404, so existence is not leaked (NFR-003) |
| 409 | State conflict | Already owned, invalid transition, or changed by someone else a moment ago |
| 500 | Unexpected | Generic body. Details go to the server log only. |

## 4. Endpoint details

### GET /api/v1/categories

```json
[ { "id": 1, "name": "Fault" }, { "id": 4, "name": "IT" } ]
```

### POST /api/v1/requests

Request body:

```json
{
  "title": "Broken light in corridor B",
  "description": "The light above room B12 flickers and then goes off.",
  "location": "Block B, first floor",
  "categoryId": 3
}
```

| Field | Rule |
| :-- | :-- |
| `title` | required, 1 to 120 characters |
| `description` | required, 1 to 2000 characters |
| `location` | required, 1 to 200 characters |
| `categoryId` | required, must be an active category |

**201 Created** with a `Location` header and the request. Status starts as `Received`.

```json
{
  "id": "5b0c2f0e-6a52-4b4c-a9f3-0d2a3f3c1d10",
  "reference": "CC-2026-000123",
  "title": "Broken light in corridor B",
  "description": "The light above room B12 flickers and then goes off.",
  "location": "Block B, first floor",
  "categoryId": 3,
  "categoryName": "Maintenance",
  "status": "Received",
  "statusText": "Received",
  "assignedTo": null,
  "assignedAt": null,
  "createdAt": "2026-10-05T08:14:00Z",
  "updatedAt": "2026-10-05T08:14:00Z"
}
```

Errors: 400 (per field), 401, 403.

### GET /api/v1/requests?status=Assigned&page=1&pageSize=20

Returns only the caller's own requests. There is no way to ask for anyone else's (AC-004.2). An empty list is a normal `200` with `"items": []` so the screen can show its empty state (AC-005.1).

### GET /api/v1/requests/{id}

Requester view: the request only. Staff view (`request.view_detail`) adds the timeline:

```json
{
  "request": { "...": "same shape as above" },
  "history": [
    { "id": 1, "action": "Submitted", "fromStatus": null, "toStatus": "Received",
      "actorId": "…", "note": null, "createdAt": "2026-10-05T08:14:00Z" }
  ],
  "notes": [
    { "id": 1, "authorId": "…", "body": "Electrician booked for Friday.", "createdAt": "2026-10-05T09:02:00Z" }
  ]
}
```

Errors: 401, 404. Staff can open a request that is unassigned or assigned to them (Slice 1 rule, see section 6).

### GET /api/v1/requests/queue

| Query | Meaning |
| :-- | :-- |
| `status` | One status. Without it, Closed and Rejected are hidden. |
| `categoryId` | One category |
| `sort` | `created` (oldest first, default), `-created`, `status`, `category`. Anything else is a 400. |
| `page`, `pageSize` | Paging |

Shows requests that are unassigned or assigned to the caller (AC-008.2). An empty result is a normal `200` (AC-009.2).

### POST /api/v1/requests/{id}/assignment

No body. The caller becomes the owner.

* **200** the updated request, status `Assigned`, `assignedTo` = caller.
* **409** `"Someone else has already taken this request."` when another staff member was first, or the request is no longer `Received`. Nothing is changed and no history or feedback is written.
* 403 without `request.assign`, 404 if it does not exist.

### PATCH /api/v1/requests/{id}/status

```json
{ "status": "InProgress" }
```

```json
{ "status": "Rejected", "reason": "This falls outside what the service can help with." }
```

Allowed moves:

| From | To |
| :-- | :-- |
| Received | Assigned (only through the assignment endpoint), Rejected |
| Assigned | InProgress |
| InProgress | Resolved |
| Resolved | Closed |

* Only the owner may move an owned request. Rejecting is done on an unowned `Received` request.
* `Rejected` needs a `reason` (1 to 500 characters). It is shown to the requester (AC-007.2).
* Closing needs `request.close` (AC-014.2).
* **409** for a move not in the table (`"A request cannot move from Received to Closed."`) or if someone else changed the request first.
* Each successful change writes a history row and a feedback item in the same transaction.

### POST /api/v1/requests/{id}/notes

```json
{ "body": "Electrician booked for Friday." }
```

`body` is required, 1 to 2000 characters. Only the owner can add a note. **201** with the saved note. Notes cannot be edited or deleted: there is no endpoint for it, and the database refuses it (AC-013.1).

### GET /api/v1/feedback

```json
[ { "id": 7, "requestId": "…", "kind": "Accepted",
    "message": "Your request CC-2026-000123 has been accepted and assigned to a staff member.",
    "reason": null, "createdAt": "2026-10-05T09:00:00Z", "readAt": null } ]
```

`kind` is one of `Accepted`, `Rejected`, `Updated`, `Completed`. Rejections always carry `reason`.

## 5. What the API does not do yet

Manager and sponsor reporting (`/reports/*`), administration (`/admin/*`), attachments, and real authentication. These are in the RTM against M3 or waiting on a change request, and nothing here pretends otherwise.

## 6. Decisions to confirm with the team

* **Staff visibility.** With no team or category assignment data yet, Slice 1 lets staff see requests that are unassigned or their own. Whether staff are also restricted by team/category (AC-008.1) needs a stakeholder answer, and interacts with CR-004.
* **Validation status code.** The contract uses 400 with a per-field `errors` object. If ADR-006 says 422, change the mapping in `ApiExceptionHandler` and this table together.
* **Rejected status.** See proposed CR-009 in `Data_Model.md`.
