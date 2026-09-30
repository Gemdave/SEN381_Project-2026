-- Seeds 1,000 requests so the NFR-004 target can be measured on real volume.
-- Run against a development or test database only.

INSERT INTO requests (
    "Id", "Reference", "Title", "Description",
    "CategoryId", "RequesterId", "Status",
    "AssignedToUserId", "AssignedAtUtc", "CreatedAtUtc")
SELECT
    gen_random_uuid(),
    'SEED-' || lpad(n::text, 5, '0'),
    'Seeded request ' || n,
    'Generated for performance measurement, not real data.',
    (SELECT "Id" FROM categories ORDER BY random() LIMIT 1),
    (SELECT "Id" FROM users WHERE "Role" = 'Requester' LIMIT 1),
    'Received',
    NULL,
    NULL,
    now() - (n || ' minutes')::interval
FROM generate_series(1, 1000) AS s(n);
