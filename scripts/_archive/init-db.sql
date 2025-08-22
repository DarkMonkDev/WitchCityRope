-- Initial database setup for WitchCityRope
-- This script runs when the PostgreSQL container is first created

-- Create the main database if it doesn't exist
-- (The database is already created by the POSTGRES_DB environment variable)

-- Create any initial schemas or extensions if needed
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- You can add additional initialization SQL here
-- For example, creating tables, inserting seed data, etc.

SELECT 'WitchCityRope database initialized successfully!' AS message;