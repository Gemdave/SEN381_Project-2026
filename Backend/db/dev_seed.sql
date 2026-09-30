-- Local development only. Never run this against a shared or production database.
-- Run after the app has applied its migrations:  psql "$DATABASE" -f db/dev_seed.sql

INSERT INTO app_user (email, display_name) VALUES
    ('requester1@civicconnect.test', 'Rita Requester'),
    ('requester2@civicconnect.test', 'Ravi Requester'),
    ('staff1@civicconnect.test',     'Sam Staff'),
    ('staff2@civicconnect.test',     'Sipho Staff')
ON CONFLICT DO NOTHING;

INSERT INTO user_role (user_id, role_id)
SELECT u.id, r.id FROM app_user u JOIN role r
  ON (u.email LIKE 'requester%@civicconnect.test' AND r.name = 'Requester')
  OR (u.email LIKE 'staff%@civicconnect.test'     AND r.name = 'ServiceStaff')
ON CONFLICT DO NOTHING;
