-- 002_category: starting category list for FR-002.
-- The exact list is still to be confirmed with Management (CR-008).

INSERT INTO category (name) VALUES
    ('Fault'),
    ('Equipment'),
    ('Security'),
    ('IT'),
    ('Maintenance'),
    ('Lost Property'),
    ('Other')
ON CONFLICT (name) DO NOTHING;
