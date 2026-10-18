# CivicConnect Data Model and Persistence Baseline (M2)

**Owner:** K. Marota
**Supports:** ADR-002 (integrity), ADR-003 (PostgreSQL 17.x), ADR-007 (Neon), ASR-01, ASR-02, ASR-03, ASR-04
**Status:** Proposed for the M2 baseline. SQL lives in `src/CivicConnect.Data/Migrations/`.

---

## 1. Why a relational database

The data is highly connected: a request has one requester, one category, at most one owner, and many history rows, notes and feedback items. The rules we care about most (one owner, no lost updates, history that cannot be edited) are exactly what transactions, foreign keys, `CHECK` constraints and conditional updates are for. Reporting is just grouping and counting the same tables, which SQL does well. The volume is small (NFR-004 targets 1,000 requests). Nothing here needs a document store or a cache, and ASR-05 pushes us toward one database we can run for free.

| Access pattern | Who | Handled by |
| :-- | :-- | :-- |
| Create a request | Requester | insert into `request` + history row |
| "My requests", optionally by status | Requester | `ix_request_requester` |
| Staff queue by status/category/owner | Staff | `ix_request_queue` |
| Open one request with its history | Staff | primary key + `ix_history_request` |
| Accept a request | Staff | conditional UPDATE on `request` |
| Feedback for me | Requester | `ix_feedback_recipient` |
| Counts by status/category | Management (M3) | grouping on `request`, same indexes |

## 2. Entity relationship diagram

```mermaid
erDiagram
    APP_USER ||--o{ REQUEST : submits
    APP_USER |o--o{ REQUEST : owns
    CATEGORY ||--o{ REQUEST : classifies
    REQUEST ||--o{ REQUEST_STATUS_HISTORY : records
    REQUEST ||--o{ REQUEST_NOTE : has
    REQUEST ||--o{ FEEDBACK_ITEM : produces
    APP_USER ||--o{ FEEDBACK_ITEM : receives
    APP_USER ||--o{ REQUEST_STATUS_HISTORY : "acts in"
    APP_USER ||--o{ REQUEST_NOTE : writes
    APP_USER ||--o{ USER_ROLE : has
    ROLE ||--o{ USER_ROLE : "given to"
    ROLE ||--o{ ROLE_PERMISSION : includes
    PERMISSION ||--o{ ROLE_PERMISSION : "part of"

    APP_USER {
        uuid id PK
        text email UK
        text display_name
        boolean is_active
        timestamptz created_at
    }
    CATEGORY {
        smallint id PK
        text name UK
        boolean is_active
    }
    REQUEST {
        uuid id PK
        text reference UK
        text title
        text description
        text location
        smallint category_id FK
        uuid requester_id FK
        text status
        uuid assigned_to FK
        timestamptz assigned_at
        int version
        timestamptz created_at
        timestamptz updated_at
    }
    REQUEST_STATUS_HISTORY {
        bigint id PK
        uuid request_id FK
        text action
        text from_status
        text to_status
        uuid actor_id FK
        text note
        timestamptz created_at
    }
    REQUEST_NOTE {
        bigint id PK
        uuid request_id FK
        uuid author_id FK
        text body
        timestamptz created_at
    }
    FEEDBACK_ITEM {
        bigint id PK
        uuid request_id FK
        uuid recipient_id FK
        text kind
        text message
        text reason
        timestamptz created_at
        timestamptz read_at
    }
    ROLE {
        smallint id PK
        text name UK
    }
    PERMISSION {
        smallint id PK
        text code UK
    }
    ROLE_PERMISSION {
        smallint role_id PK
        smallint permission_id PK
    }
    USER_ROLE {
        uuid user_id PK
        smallint role_id PK
    }
```

## 3. Entities, ownership and lifecycle

"Owner" here means which part of the backend is allowed to write the table.

| Table | What it holds | Written by | Lifecycle |
| :-- | :-- | :-- | :-- |
| `app_user` | Person, active flag. No password column yet (ADR-005). | User management (M3), dev seed | Deactivated, never deleted, so old history still points somewhere |
| `category` | Controlled list for FR-002 | Category maintenance (M3), seed | Retired with `is_active = false`, never deleted, so old requests keep their category |
| `request` | The request and its current state | `RequestService`, `AssignmentService`, `StatusTransitionService` | Received, then Assigned, In Progress, Resolved, Closed (or Rejected from Received). Never deleted |
| `request_status_history` | One row per submission, assignment or status change | Same services, in the same transaction | Append-only, kept for good |
| `request_note` | Staff notes and resolution information | `NoteService` | Append-only |
| `feedback_item` | In-app messages for the requester | Assignment and transition services | Created with the change; only `read_at` is ever updated |
| `role`, `permission`, `role_permission`, `user_role` | RBAC data (ADR-005) | Seeded now, admin screens in M3 | Changed by administrators |

## 4. Constraints that protect business correctness

| Rule | Constraint | Requirement |
| :-- | :-- | :-- |
| Status is one of a fixed set | `ck_request_status` | FR-012, ASR-01 |
| Required fields are present and not blank | `NOT NULL` + `ck_request_title / description / location` | AC-001.2 |
| Every request has a real, active-at-the-time category | `category_id NOT NULL` + foreign key | FR-002 |
| Reference is unique and generated by the database | `uq_request_reference` + sequence default | AC-001.1 |
| Owner and timestamp go together | `ck_request_owner_pair` | FR-011 |
| A request that is Received or Rejected has no owner; every other status has one | `ck_request_owner_vs_status` | FR-011, NFR-005 |
| Claiming a request only works if it is still unowned | conditional `UPDATE ... WHERE assigned_to IS NULL AND status = 'Received'` | NFR-005 |
| Two status changes cannot both win from the same starting status | `UPDATE ... WHERE status = <what we read>` | NFR-005 |
| History and notes cannot be edited or deleted | `trg_*_append_only` triggers | NFR-007, ASR-03 |
| Feedback for a rejection must carry a reason | `ck_feedback_reason` | AC-007.2 |

The conditional `UPDATE` is the recommendation from Assignment 2 (R2-2). The `version` column is bumped on every change so a later feature that needs "save only if unchanged" has something to use.

## 5. Indexes

| Index | Columns | Serves |
| :-- | :-- | :-- |
| `uq_request_reference` | `reference` | AC-001.1 unique reference |
| `ix_request_requester` | `requester_id, status, created_at DESC` | FR-004, FR-005 |
| `ix_request_queue` | `status, category_id, assigned_to` | FR-008, FR-009 |
| `ix_request_created` | `created_at` | sorting, NFR-004 |
| `ix_history_request` | `request_id, created_at` | FR-010 |
| `ix_note_request` | `request_id, created_at` | FR-010 |
| `ix_feedback_recipient` | `recipient_id, created_at DESC` | FR-007 |

We are deliberately not adding a cache (A2 R2-4). If the 3-second target is missed at 1,000 seeded requests, indexes and query shape come first.

## 6. Migrations

Plain SQL files, applied once and in order by a small runner that records them in `schema_migration`. The first three keep the names already used in the RTM.

| File | Contents |
| :-- | :-- |
| `001_request.sql` | `app_user`, `category`, `request` with checks and indexes |
| `002_category.sql` | Seed of the starting category list (content still to be confirmed, CR-008) |
| `003_assignment.sql` | Owner columns, `request_status_history`, `request_note`, append-only triggers, queue index |
| `004_feedback.sql` | `feedback_item` |
| `005_rbac.sql` | `role`, `permission`, `role_permission`, `user_role` and a draft starting matrix |

The `app_user` table is created in 001 because `request.requester_id` has to point at something from day one.

## 7. Size, availability and backup

At the volumes in the PED (thousands of requests, roughly five to ten history rows each) the data is tiny. The realistic concerns are not scale.

* **Single point of failure.** One Neon instance is the SPOF for the whole product. We accept this on purpose: a replica or failover costs money (ASR-05) and the Sponsor has not asked for high availability. It is recorded as a risk, not hidden.
* **Scaling if it ever mattered.** The order would be: check indexes, add connection pooling, move up a plan, and only then consider read replicas for the reporting queries. Nothing in the current schema blocks that path.
* **Backup and recovery.** We rely on the managed provider's restore feature on the free plan. The exact restore window and limits must be read from Neon's own documentation and written into the ADR-007 evidence before baseline sign-off (RSK-015). As a fallback we can run a scheduled `pg_dump`; that is recorded as a forward consideration, not built.
* **Sensitive data.** Requests can contain personal information. Nothing sensitive is committed to the repo; the connection string comes from configuration only (RSK-005).

## 8. Pending schema changes (not applied)

| Change | Trigger | Draft |
| :-- | :-- | :-- |
| Target response time so "overdue" can be defined | CR-005 approved | `ALTER TABLE request ADD COLUMN target_response_at timestamptz;` then compare with `now()` in reporting |
| Credentials or identity link columns on `app_user` | ADR-005 / CR-003 | Depends on the mechanism Gerald settles on |
| Rejected status | Proposed **CR-009** (see below) | Already allowed by `ck_request_status` so no migration is needed |
| Admin action audit table | M3 (FR-022) | `audit_log` mirroring the history table pattern |

**Why CR-009.** FR-007 says a requester is told when a request is *rejected*, with a reason (AC-007.2). FR-012 lists the lifecycle as Received, Assigned, In Progress, Resolved, Closed, and says Received to Closed is invalid (AC-012.2). Rejection therefore has no place in the baselined lifecycle. The backend treats it as a separate terminal status reachable only from Received, with a mandatory reason. That is a small change to the M1 lifecycle, so it should go through change control before the baseline is signed.

## 9. As built at M3 (corrections)

The schema built in `app/` uses these names. Section 8 should also be read with the Rejected note below.

| Above | As built |
| :-- | :-- |
| `app_user`, `category`, `request` | `users`, `categories`, `requests` |
| `request_status_history` | `request_history` |
| `request_note` table | No separate table. A note is a `request_history` row with action `NoteAdded`. |
| `feedback_item` with `read_at` | `requester_feedback`, no `read_at` yet |
| `ck_request_status` | `ck_requests_status`. It does **not** allow `Rejected`, so adding Rejected needs a migration. |
| `ck_request_owner_vs_status` | `ck_requests_owner_matches_status` (only `Received` has no owner) |
| `ck_request_title / description / location`, `ck_request_owner_pair` | Not built. There is no location column. |
| Append-only triggers in `003_assignment.sql` | Trigger `request_history_insert_only`, created by the EF migration `AddHistoryTrigger` |
| Migrations in `src/CivicConnect.Data/Migrations/` as SQL | EF Core migrations in `app/src/CivicConnect.Infrastructure/Migrations/` |
| Foreign keys | Built: requests to categories and users, history and messages to requests and users, all `ON DELETE RESTRICT` |

**Open question for D-005 (microservices):** the foreign keys from requests to users and categories assume one database. If those tables move to other services, the keys have to go, and the checks that depend on them need another home.

## 10. As built at v1.0.0 (final)

Section 9 described the M3 schema. The final schema comes from six EF Core migrations (`InitialCreate` to `AddSignIn`) and has eight tables. Changes since section 9:

| Table | Final columns and rules | Requirement |
| :-- | :-- | :-- |
| `requests` | Adds `Location`, a unique `Reference` from `request_reference_seq`, and `TargetResponseAtUtc` (due time fixed at submission). `ck_requests_status` now allows `Rejected`; `ck_requests_owner_matches_status` lets only `Received` and `Rejected` have no owner. Indexes on status with category and owner, and on status with due time | FR-001, FR-016 (CR-005), CR-009 |
| `categories` | `IsRetired` and `TargetResponseHours` (1 to 2160, `ck_categories_target_hours`) | FR-021, CR-005 |
| `users` | `IsActive`, a unique `Email`, and the sign-in columns `PasswordHash` (PBKDF2), `MustChangePassword`, `FailedSignIns`, `LockedUntil` and `SecurityStamp` | FR-019, CR-003 (ADR-008) |
| `staff_category_scopes` (new) | One row per staff member and category they may work on (`UserId`, `CategoryId`) | FR-008, ADR-005 |
| `request_attachments` (new) | File bytes stored in PostgreSQL (`Content`), with name, type, size (1 byte to 5 MB, `ck_request_attachments_size`), uploader and time; the code allows 5 per request | FR-006 (CR-016) |
| `audit_entries` (new) | Actor, action, subject, detail and time for user, role, scope and category changes; insert-only through the trigger `audit_entries_insert_only` | FR-022, NFR-007 |
| `request_history`, `requester_feedback` | Unchanged since section 9; `requester_feedback` still has no `read_at` | FR-013, FR-007 |

All foreign keys stay `ON DELETE RESTRICT`. Attachments live in the database, so the hosted service keeps no files of its own. The open question above did not arise, because the release is one deployable (CR-014).
