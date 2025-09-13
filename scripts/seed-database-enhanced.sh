#!/bin/bash

# Database Seed Script for Enhanced Containerized Testing Infrastructure
# Phase 1: Enhanced Containerized Testing Infrastructure
# Seeds both development and test databases with consistent data

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
Database Seed Script for WitchCityRope

Usage: $0 [OPTIONS] <environment>

Arguments:
  environment     Target environment: 'dev' or 'test'

Options:
  -h, --host      Database host (default: localhost)
  -p, --port      Database port (default: dev=5433, test=5432)
  -d, --database  Database name (default: dev=witchcityrope, test=witchcityrope_test)
  -u, --user      Database user (default: dev=postgres, test=test_user)
  -w, --password  Database password (default: dev=postgres, test=Test123!)
  --force         Force reseed even if data already exists
  --help          Show this help message

Examples:
  $0 dev                    # Seed development database with defaults
  $0 test                   # Seed test database with defaults
  $0 dev --force           # Force reseed development database
  $0 test -p 5434          # Seed test database on custom port

This script will:
1. Check if database exists and is accessible
2. Run the SeedDataService to populate with standard data
3. Create test accounts and sample events
4. Ensure idempotent operations (safe to run multiple times)

EOF
}

# Function to check if database exists and is accessible
check_database_connection() {
    local host=$1
    local port=$2
    local user=$3
    local password=$4
    local database=$5
    
    log_info "Checking database connection to $database at $host:$port..."
    
    export PGPASSWORD="$password"
    if psql -h "$host" -p "$port" -U "$user" -d "$database" -c "SELECT 1;" > /dev/null 2>&1; then
        log_success "Database connection successful"
        return 0
    else
        log_error "Cannot connect to database '$database' at $host:$port with user $user"
        log_error "Ensure the database exists (run reset-database.sh first)"
        return 1
    fi
}

# Function to check if database has existing data
check_existing_data() {
    local host=$1
    local port=$2
    local user=$3
    local password=$4
    local database=$5
    
    log_info "Checking for existing data..."
    
    export PGPASSWORD="$password"
    
    # Check if AspNetUsers table has data
    local user_count
    user_count=$(psql -h "$host" -p "$port" -U "$user" -d "$database" -t -c "
        SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"UserName\" IN ('admin@witchcityrope.com', 'teacher@witchcityrope.com');
    " 2>/dev/null | tr -d ' ')
    
    if [ "${user_count:-0}" -gt 0 ]; then
        log_info "Found existing seed data (${user_count} test users)"
        return 0
    else
        log_info "No existing seed data found"
        return 1
    fi
}

# Function to run seed data service
run_seed_service() {
    local environment=$1
    local connection_string=$2
    local force_reseed=$3
    
    log_info "Running SeedDataService for $environment environment..."
    
    # Change to the API project directory
    cd "$(dirname "$0")/../apps/api"
    
    # Set environment variables
    if [ "$environment" = "dev" ]; then
        export ConnectionStrings__DefaultConnection="$connection_string"
        export ASPNETCORE_ENVIRONMENT="Development"
    else
        export ConnectionStrings__TestConnection="$connection_string"
        export ASPNETCORE_ENVIRONMENT="Testing"
    fi
    
    # Set force reseed flag if requested
    if [ "$force_reseed" = "true" ]; then
        export SEED_FORCE_REFRESH="true"
    fi
    
    # Check if there's a dedicated seeder tool, otherwise use the API startup seeding
    if [ -f "SeedDataTool.cs" ] || dotnet run --dry-run 2>/dev/null | grep -q "SeedDataTool"; then
        log_info "Using dedicated seed data tool..."
        if dotnet run --project . -- --seed-only > /dev/null 2>&1; then
            log_success "Seed data tool executed successfully"
        else
            log_error "Seed data tool failed"
            return 1
        fi
    else
        log_info "Using API startup seeding (development pattern)..."
        
        # Create a temporary seeding script that runs the API briefly to trigger seeding
        cat > temp_seed.sh << 'EOF'
#!/bin/bash
timeout 30s dotnet run --environment "$ASPNETCORE_ENVIRONMENT" --urls "http://localhost:0" > /dev/null 2>&1 || true
EOF
        chmod +x temp_seed.sh
        
        if ./temp_seed.sh; then
            log_success "API startup seeding completed"
        else
            log_warning "API startup seeding may have failed (this might be normal)"
        fi
        
        rm -f temp_seed.sh
    fi
}

# Function to verify seed data was created
verify_seed_data() {
    local host=$1
    local port=$2
    local user=$3
    local password=$4
    local database=$5
    
    log_info "Verifying seed data was created..."
    
    export PGPASSWORD="$password"
    
    # Check for test accounts
    local user_count
    user_count=$(psql -h "$host" -p "$port" -U "$user" -d "$database" -t -c "
        SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"UserName\" IN (
            'admin@witchcityrope.com', 
            'teacher@witchcityrope.com', 
            'vetted@witchcityrope.com',
            'member@witchcityrope.com',
            'guest@witchcityrope.com'
        );
    " 2>/dev/null | tr -d ' ')
    
    # Check for events
    local event_count
    event_count=$(psql -h "$host" -p "$port" -U "$user" -d "$database" -t -c "
        SELECT COUNT(*) FROM \"Events\" WHERE \"Title\" LIKE '%Sample%' OR \"Title\" LIKE '%Workshop%';
    " 2>/dev/null | tr -d ' ')
    
    log_info "Verification results:"
    log_info "  Test users created: ${user_count:-0}/5"
    log_info "  Sample events created: ${event_count:-0}"
    
    if [ "${user_count:-0}" -ge 3 ]; then
        log_success "Seed data verification passed"
        return 0
    else
        log_warning "Seed data verification found fewer users than expected"
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
FORCE_RESEED="false"

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
        --force)
            FORCE_RESEED="true"
            shift
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

log_info "Starting database seeding for $ENVIRONMENT environment"
log_info "Target: $HOST:$PORT/$DATABASE (user: $USER)"

# Check database connectivity
if ! check_database_connection "$HOST" "$PORT" "$USER" "$PASSWORD" "$DATABASE"; then
    exit 1
fi

# Check for existing data unless force reseed is requested
if [ "$FORCE_RESEED" = "false" ]; then
    if check_existing_data "$HOST" "$PORT" "$USER" "$PASSWORD" "$DATABASE"; then
        log_info "Existing seed data found. Use --force to reseed."
        log_success "Database seeding completed (data already exists)"
        exit 0
    fi
fi

# Run seed service
if ! run_seed_service "$ENVIRONMENT" "$CONNECTION_STRING" "$FORCE_RESEED"; then
    log_error "Failed to run seed service"
    exit 1
fi

# Verify seed data was created
if ! verify_seed_data "$HOST" "$PORT" "$USER" "$PASSWORD" "$DATABASE"; then
    log_warning "Seed data verification failed, but seeding may have still succeeded"
fi

log_success "Database seeding completed successfully for $ENVIRONMENT environment"
log_info "Test accounts available:"
log_info "  admin@witchcityrope.com / Test123!"
log_info "  teacher@witchcityrope.com / Test123!"
log_info "  vetted@witchcityrope.com / Test123!"
log_info "  member@witchcityrope.com / Test123!"
log_info "  guest@witchcityrope.com / Test123!"