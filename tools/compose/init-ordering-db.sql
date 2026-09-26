-- summary: PostgreSQL runs every file in
-- docker-entrypoint-initdb.d exactly once — on the FIRST start of an empty
-- data volume. An existing volume never re-runs it.
CREATE DATABASE ordering_db OWNER sphere;