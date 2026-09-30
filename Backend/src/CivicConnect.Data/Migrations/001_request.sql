-- 001_request: the request itself, plus the two tables it has to point at.

CREATE TABLE app_user (
    id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    email         text        NOT NULL,
    display_name  text        NOT NULL,
    is_active     boolean     NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX uq_app_user_email ON app_user (lower(email));

CREATE TABLE category (
    id         smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name       text     NOT NULL,
    is_active  boolean  NOT NULL DEFAULT true,
    CONSTRAINT uq_category_name UNIQUE (name)
);

CREATE SEQUENCE request_reference_seq;

CREATE TABLE request (
    id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    reference     text        NOT NULL DEFAULT (
                      'CC-' || to_char(now(), 'YYYY') || '-' ||
                      lpad(nextval('request_reference_seq')::text, 6, '0')),
    title         text        NOT NULL,
    description   text        NOT NULL,
    location      text        NOT NULL,
    category_id   smallint    NOT NULL REFERENCES category (id),
    requester_id  uuid        NOT NULL REFERENCES app_user (id),
    status        text        NOT NULL DEFAULT 'Received',
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_request_reference UNIQUE (reference),
    CONSTRAINT ck_request_title       CHECK (length(btrim(title))       BETWEEN 1 AND 120),
    CONSTRAINT ck_request_description CHECK (length(btrim(description)) BETWEEN 1 AND 2000),
    CONSTRAINT ck_request_location    CHECK (length(btrim(location))    BETWEEN 1 AND 200),
    CONSTRAINT ck_request_status      CHECK (status IN
        ('Received', 'Assigned', 'InProgress', 'Resolved', 'Closed', 'Rejected'))
);

CREATE INDEX ix_request_requester ON request (requester_id, status, created_at DESC);
CREATE INDEX ix_request_created   ON request (created_at);
