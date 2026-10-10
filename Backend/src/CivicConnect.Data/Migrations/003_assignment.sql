-- 003_assignment: who owns a request, and the history that goes with it.

ALTER TABLE request
    ADD COLUMN assigned_to uuid REFERENCES app_user (id),
    ADD COLUMN assigned_at timestamptz,
    ADD COLUMN version     integer NOT NULL DEFAULT 1,
    -- owner and timestamp always travel together
    ADD CONSTRAINT ck_request_owner_pair
        CHECK ((assigned_to IS NULL) = (assigned_at IS NULL)),
    -- a request nobody owns is Received or Rejected; everything else has an owner
    ADD CONSTRAINT ck_request_owner_vs_status
        CHECK ((status IN ('Received', 'Rejected')) = (assigned_to IS NULL));

CREATE INDEX ix_request_queue ON request (status, category_id, assigned_to);

CREATE TABLE request_status_history (
    id           bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_id   uuid        NOT NULL REFERENCES request (id),
    action       text        NOT NULL,
    from_status  text,
    to_status    text        NOT NULL,
    actor_id     uuid        NOT NULL REFERENCES app_user (id),
    note         text,
    created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_history_request ON request_status_history (request_id, created_at);

CREATE TABLE request_note (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_id  uuid        NOT NULL REFERENCES request (id),
    author_id   uuid        NOT NULL REFERENCES app_user (id),
    body        text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_note_body CHECK (length(btrim(body)) BETWEEN 1 AND 2000)
);
CREATE INDEX ix_note_request ON request_note (request_id, created_at);

-- History and notes are append-only. Corrections are new rows.
CREATE FUNCTION block_history_changes() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION '% is append-only. Add a new row instead of changing an old one.', TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_history_append_only
    BEFORE UPDATE OR DELETE ON request_status_history
    FOR EACH ROW EXECUTE FUNCTION block_history_changes();

CREATE TRIGGER trg_note_append_only
    BEFORE UPDATE OR DELETE ON request_note
    FOR EACH ROW EXECUTE FUNCTION block_history_changes();
