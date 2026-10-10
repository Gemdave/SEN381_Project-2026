-- 004_feedback: in-app messages for the requester (FR-007).
-- Only read_at is ever updated.

CREATE TABLE feedback_item (
    id            bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_id    uuid        NOT NULL REFERENCES request (id),
    recipient_id  uuid        NOT NULL REFERENCES app_user (id),
    kind          text        NOT NULL,
    message       text        NOT NULL,
    reason        text,
    created_at    timestamptz NOT NULL DEFAULT now(),
    read_at       timestamptz,
    CONSTRAINT ck_feedback_kind   CHECK (kind IN ('Accepted', 'Rejected', 'Updated', 'Completed')),
    CONSTRAINT ck_feedback_reason CHECK (kind <> 'Rejected' OR length(btrim(coalesce(reason, ''))) > 0)
);
CREATE INDEX ix_feedback_recipient ON feedback_item (recipient_id, created_at DESC);
