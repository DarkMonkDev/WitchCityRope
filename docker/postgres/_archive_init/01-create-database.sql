-- PostgreSQL Database Initialization Script
-- WitchCityRope Docker Authentication System
-- 
-- This script runs once when the PostgreSQL container is first created
-- Creates database, schemas, users, and enables required extensions
-- 
-- References:
-- - /docs/functional-areas/docker-authentication/design/database-container-design.md
-- - /docs/standards-processes/development-standards/entity-framework-patterns.md

-- ============================================================================
-- 1. DATABASE AND SCHEMA CREATION
-- ============================================================================

-- Create the main application database (if not exists via POSTGRES_DB env var)
-- The database will be created automatically by the postgres:16-alpine image
-- using the POSTGRES_DB environment variable

-- Create schemas for organized table management
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS public;

-- Set schema comments for documentation
COMMENT ON SCHEMA auth IS 'ASP.NET Core Identity authentication tables';
COMMENT ON SCHEMA public IS 'Application-specific business logic tables';

-- ============================================================================
-- 2. POSTGRESQL EXTENSIONS
-- ============================================================================

-- Enable UUID generation for Guid primary keys (required for ASP.NET Identity)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
COMMENT ON EXTENSION "uuid-ossp" IS 'UUID generation functions for Guid primary keys';

-- Enable case-insensitive text type (useful for email addresses)
CREATE EXTENSION IF NOT EXISTS "citext";
COMMENT ON EXTENSION "citext" IS 'Case-insensitive text type for email handling';

-- Enable performance monitoring (development environment)
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
COMMENT ON EXTENSION "pg_stat_statements" IS 'Query performance monitoring for development';

-- ============================================================================
-- 3. APPLICATION USER CREATION
-- ============================================================================

-- Create dedicated application user with minimal required privileges
-- Following security best practice: separate application user from superuser
DO $$
BEGIN
    -- Check if user already exists
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'witchcityrope_app') THEN
        -- Create application user
        CREATE USER witchcityrope_app WITH PASSWORD 'WitchCity2024!';
        
        -- Log user creation
        RAISE NOTICE 'Created application user: witchcityrope_app';
    ELSE
        RAISE NOTICE 'Application user witchcityrope_app already exists';
    END IF;
END
$$;

-- ============================================================================
-- 4. DATABASE PERMISSIONS
-- ============================================================================

-- Grant database connection permissions
GRANT CONNECT ON DATABASE witchcityrope TO witchcityrope_app;

-- Grant schema usage permissions
GRANT USAGE ON SCHEMA auth TO witchcityrope_app;
GRANT USAGE ON SCHEMA public TO witchcityrope_app;

-- Grant schema creation permissions (required for EF Core migrations)
GRANT CREATE ON SCHEMA auth TO witchcityrope_app;
GRANT CREATE ON SCHEMA public TO witchcityrope_app;

-- ============================================================================
-- 5. TABLE PERMISSIONS (DEFAULT PRIVILEGES)
-- ============================================================================

-- Set default privileges for future tables created by postgres user
-- This ensures EF Core migrations create tables with proper permissions

-- Auth schema default privileges (Identity tables)
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA auth 
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO witchcityrope_app;

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA auth 
    GRANT USAGE, SELECT ON SEQUENCES TO witchcityrope_app;

-- Public schema default privileges (Application tables)
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public 
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO witchcityrope_app;

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public 
    GRANT USAGE, SELECT ON SEQUENCES TO witchcityrope_app;

-- ============================================================================
-- 6. PERFORMANCE OPTIMIZATION SETTINGS
-- ============================================================================

-- Configure PostgreSQL settings for optimal performance in container environment
-- These settings are calibrated for 2GB memory allocation (docker-compose)

-- Memory settings (25% of container memory for shared_buffers)
ALTER SYSTEM SET shared_buffers = '512MB';
ALTER SYSTEM SET effective_cache_size = '1536MB';
ALTER SYSTEM SET maintenance_work_mem = '128MB';
ALTER SYSTEM SET work_mem = '16MB';

-- Connection settings (matching docker container limits)
ALTER SYSTEM SET max_connections = '200';
ALTER SYSTEM SET max_prepared_transactions = '0';

-- Write-ahead logging optimization
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET checkpoint_completion_target = '0.9';
ALTER SYSTEM SET checkpoint_timeout = '10min';

-- SSD optimization (Docker volumes typically use SSD)
ALTER SYSTEM SET random_page_cost = '1.1';
ALTER SYSTEM SET effective_io_concurrency = '200';

-- Query optimization
ALTER SYSTEM SET default_statistics_target = '100';

-- ============================================================================
-- 7. LOGGING CONFIGURATION
-- ============================================================================

-- Configure logging for development environment
-- Production environment will override these settings

-- Enable connection logging for security audit
ALTER SYSTEM SET log_connections = 'on';
ALTER SYSTEM SET log_disconnections = 'on';

-- Log slow queries (development: all queries, production: >1 second)
ALTER SYSTEM SET log_min_duration_statement = '0';  -- Log all queries in dev
ALTER SYSTEM SET log_statement = 'all';             -- Log all statements in dev

-- Enhanced logging format
ALTER SYSTEM SET log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h ';

-- ============================================================================
-- 8. TIMEZONE CONFIGURATION
-- ============================================================================

-- Set timezone to UTC for consistency (critical for ASP.NET Core Identity)
-- All TIMESTAMPTZ columns will store and return UTC values
ALTER SYSTEM SET timezone = 'UTC';

-- ============================================================================
-- 9. RELOAD CONFIGURATION
-- ============================================================================

-- Apply all configuration changes
SELECT pg_reload_conf();

-- ============================================================================
-- 10. VALIDATION AND REPORTING
-- ============================================================================

-- Display configuration summary
DO $$
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'PostgreSQL Database Initialization Complete';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Database: %', current_database();
    RAISE NOTICE 'Schemas: auth, public';
    RAISE NOTICE 'Extensions: uuid-ossp, citext, pg_stat_statements';
    RAISE NOTICE 'Application User: witchcityrope_app';
    RAISE NOTICE 'Timezone: UTC';
    RAISE NOTICE 'Performance: Optimized for 2GB container memory';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Ready for EF Core migrations and ASP.NET Core Identity tables';
    RAISE NOTICE '============================================================================';
END
$$;

-- Log successful initialization
\echo 'Database initialization script completed successfully'