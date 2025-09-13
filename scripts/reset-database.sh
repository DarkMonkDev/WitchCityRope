#!/bin/bash

# Database Reset Script for Enhanced Containerized Testing Infrastructure
# Phase 1: Enhanced Containerized Testing Infrastructure
# Supports both development and test database reset operations

set -e  # Exit on any error

# Default configuration
DEFAULT_DEV_HOST="localhost"
DEFAULT_DEV_PORT="5433"
DEFAULT_DEV_DATABASE="witchcityrope"
DEFAULT_DEV_USER="postgres"
DEFAULT_DEV_PASSWORD="postgres"

DEFAULT_TEST_HOST="localhost"
DEFAULT_TEST_PORT="5432"
DEFAULT_TEST_DATABASE="witchcityrope_test"
DEFAULT_TEST_USER="test_user"
DEFAULT_TEST_PASSWORD="Test123!"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to display usage
show_usage() {
    cat << EOF
Database Reset Script for WitchCityRope

Usage: $0 [OPTIONS] <environment>

Arguments:
  environment     Target environment: 'dev' or 'test'

Options:
  -h, --host      Database host (default: localhost)
  -p, --port      Database port (default: dev=5433, test=5432)
  -d, --database  Database name (default: dev=witchcityrope, test=witchcityrope_test)
  -u, --user      Database user (default: dev=postgres, test=test_user)
  -w, --password  Database password (default: dev=postgres, test=Test123!)
  --help          Show this help message

Examples:
  $0 dev                    # Reset development database with defaults
  $0 test                   # Reset test database with defaults
  $0 dev -p 5434           # Reset dev database on custom port
  $0 test -h testhost      # Reset test database on custom host

This script will:
1. Drop the target database if it exists
2. Create a new empty database
3. Apply EF Core migrations
4. Ready for seeding (use seed-database.sh)

EOF
}

# Function to check if PostgreSQL is available
check_postgres_connection() {
    local host=$1
    local port=$2
    local user=$3
    local password=$4
    
    log_info "Checking PostgreSQL connection to $host:$port..."
    
    export PGPASSWORD="$password"
    if psql -h "$host" -p "$port" -U "$user" -d postgres -c "SELECT 1;" > /dev/null 2>&1; then
        log_success "PostgreSQL connection successful"
        return 0
    else
        log_error "Cannot connect to PostgreSQL at $host:$port with user $user"
        return 1
    fi
}

# Function to drop database if exists
drop_database() {
    local host=$1
    local port=$2
    local user=$3
    local password=$4
    local database=$5
    
    log_info "Dropping database '$database' if it exists..."
    
    export PGPASSWORD="$password"
    
    # Terminate any active connections to the database
    psql -h "$host" -p "$port" -U "$user" -d postgres -c "
        SELECT pg_terminate_backend(pg_stat_activity.pid)
        FROM pg_stat_activity
        WHERE pg_stat_activity.datname = '$database'
          AND pid <> pg_backend_pid();" > /dev/null 2>&1 || true
    
    # Drop the database
    if psql -h "$host" -p "$port" -U "$user" -d postgres -c "DROP DATABASE IF EXISTS \"$database\";" > /dev/null 2>&1; then
        log_success "Database '$database' dropped successfully"
    else
        log_warning "Could not drop database '$database' (may not exist)"
    fi
}

# Function to create database
create_database() {
    local host=$1
    local port=$2
    local user=$3
    local password=$4
    local database=$5
    
    log_info "Creating database '$database'..."
    
    export PGPASSWORD="$password"
    
    if psql -h "$host" -p "$port" -U "$user" -d postgres -c "CREATE DATABASE \"$database\";" > /dev/null 2>&1; then
        log_success "Database '$database' created successfully"
    else
        log_error "Failed to create database '$database'"
        return 1
    fi
}

# Function to apply migrations
apply_migrations() {
    local environment=$1
    local connection_string=$2
    
    log_info "Applying EF Core migrations for $environment environment..."
    
    # Change to the API project directory
    cd "$(dirname "$0")/../apps/api"
    
    # Set the connection string environment variable
    if [ "$environment" = "dev" ]; then
        export ConnectionStrings__DefaultConnection="$connection_string"
        export ASPNETCORE_ENVIRONMENT="Development"
    else
        export ConnectionStrings__TestConnection="$connection_string"
        export ASPNETCORE_ENVIRONMENT="Testing"
    fi
    
    # Apply migrations
    if dotnet ef database update > /dev/null 2>&1; then
        log_success "EF Core migrations applied successfully"
    else
        log_error "Failed to apply EF Core migrations"
        return 1
    fi
}

# Parse command line arguments
ENVIRONMENT=""
HOST=""
PORT=""
DATABASE=""
USER=""
PASSWORD=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--host)
            HOST="$2"
            shift 2
            ;;
        -p|--port)
            PORT="$2"
            shift 2
            ;;
        -d|--database)
            DATABASE="$2"
            shift 2
            ;;
        -u|--user)
            USER="$2"
            shift 2
            ;;
        -w|--password)
            PASSWORD="$2"
            shift 2
            ;;
        --help)
            show_usage
            exit 0
            ;;
        dev|test)
            if [ -z "$ENVIRONMENT" ]; then
                ENVIRONMENT="$1"
            else
                log_error "Multiple environments specified"
                exit 1
            fi
            shift
            ;;
        *)
            log_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Validate environment argument
if [ -z "$ENVIRONMENT" ]; then
    log_error "Environment argument required (dev or test)"
    show_usage
    exit 1
fi

if [ "$ENVIRONMENT" != "dev" ] && [ "$ENVIRONMENT" != "test" ]; then
    log_error "Environment must be 'dev' or 'test'"
    exit 1
fi

# Set defaults based on environment
if [ "$ENVIRONMENT" = "dev" ]; then
    HOST=${HOST:-$DEFAULT_DEV_HOST}
    PORT=${PORT:-$DEFAULT_DEV_PORT}
    DATABASE=${DATABASE:-$DEFAULT_DEV_DATABASE}
    USER=${USER:-$DEFAULT_DEV_USER}
    PASSWORD=${PASSWORD:-$DEFAULT_DEV_PASSWORD}
else
    HOST=${HOST:-$DEFAULT_TEST_HOST}
    PORT=${PORT:-$DEFAULT_TEST_PORT}
    DATABASE=${DATABASE:-$DEFAULT_TEST_DATABASE}
    USER=${USER:-$DEFAULT_TEST_USER}
    PASSWORD=${PASSWORD:-$DEFAULT_TEST_PASSWORD}
fi

# Build connection string
CONNECTION_STRING="Host=$HOST;Port=$PORT;Database=$DATABASE;Username=$USER;Password=$PASSWORD"

log_info "Starting database reset for $ENVIRONMENT environment"
log_info "Target: $HOST:$PORT/$DATABASE (user: $USER)"

# Check PostgreSQL availability
if ! check_postgres_connection "$HOST" "$PORT" "$USER" "$PASSWORD"; then
    exit 1
fi

# Drop existing database
drop_database "$HOST" "$PORT" "$USER" "$PASSWORD" "$DATABASE"

# Create new database
if ! create_database "$HOST" "$PORT" "$USER" "$PASSWORD" "$DATABASE"; then
    exit 1
fi

# Apply migrations
if ! apply_migrations "$ENVIRONMENT" "$CONNECTION_STRING"; then
    exit 1
fi

log_success "Database reset completed successfully for $ENVIRONMENT environment"
log_info "Database is ready for seeding. Run: ./scripts/seed-database.sh $ENVIRONMENT"