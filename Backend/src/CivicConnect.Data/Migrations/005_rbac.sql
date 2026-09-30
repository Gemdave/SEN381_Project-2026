-- 005_rbac: roles and permissions. The starting matrix is a draft;
-- Gerald owns the final version through ADR-005 / CR-003.

CREATE TABLE role (
    id    smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  text     NOT NULL UNIQUE
);

CREATE TABLE permission (
    id    smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code  text     NOT NULL UNIQUE
);

CREATE TABLE role_permission (
    role_id       smallint NOT NULL REFERENCES role (id),
    permission_id smallint NOT NULL REFERENCES permission (id),
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE user_role (
    user_id  uuid     NOT NULL REFERENCES app_user (id),
    role_id  smallint NOT NULL REFERENCES role (id),
    PRIMARY KEY (user_id, role_id)
);

INSERT INTO role (name) VALUES
    ('Requester'), ('ServiceStaff'), ('Manager'), ('Administrator'), ('Sponsor');

INSERT INTO permission (code) VALUES
    ('request.submit'), ('request.view_own'),
    ('request.queue'), ('request.view_detail'), ('request.assign'),
    ('request.update_status'), ('request.close'), ('request.add_note');

-- Requester
INSERT INTO role_permission (role_id, permission_id)
SELECT r.id, p.id FROM role r JOIN permission p
  ON p.code IN ('request.submit', 'request.view_own')
WHERE r.name = 'Requester';

-- Service staff
INSERT INTO role_permission (role_id, permission_id)
SELECT r.id, p.id FROM role r JOIN permission p
  ON p.code IN ('request.queue', 'request.view_detail', 'request.assign',
                'request.update_status', 'request.close', 'request.add_note')
WHERE r.name = 'ServiceStaff';

-- Manager, Administrator and Sponsor get their permissions in M3.
