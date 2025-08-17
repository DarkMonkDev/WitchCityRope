#!/bin/bash

# WitchCityRope - Docker Database Migration Script
# This script manages EF Core migrations in Docker container environments

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Print functions
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if containers are running
check_containers() {
    print_info "Checking container status..."
    
    if ! docker-compose ps | grep -q "Up"; then
        print_error "Docker containers are not running. Please start them first:"
        echo "  docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d"
        exit 1
    fi
    
    # Check postgres specifically
    if ! docker-compose ps postgres | grep -q "Up (healthy)"; then
        print_warning "PostgreSQL container is not healthy. Waiting..."
        sleep 5
    fi
    
    print_success "Containers are running"
}

# Function to check database connectivity
check_database() {
    print_info "Checking database connectivity..."
    
    if docker-compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
        print_success "Database is accessible"
    else
        print_error "Cannot connect to database"
        exit 1
    fi
}

# Function to install EF Core tools if needed
install_ef_tools() {
    print_info "Checking EF Core tools..."
    
    if ! docker-compose exec -T api dotnet ef --version > /dev/null 2>&1; then
        print_info "Installing EF Core tools..."
        docker-compose exec -T api dotnet tool install --global dotnet-ef
        # Update PATH in container
        docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools"'
    fi
    
    print_success "EF Core tools are available"
}

# Function to list current migrations
list_migrations() {
    print_info "Listing available migrations..."
    
    if docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations list' > /dev/null 2>&1; then
        docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations list'
    else
        print_warning "No migrations found or EF tools not available"
    fi
}

# Function to create initial migration if none exist
create_initial_migration() {
    print_info "Checking for existing migrations..."
    
    if ! docker-compose exec -T api ls Migrations > /dev/null 2>&1; then
        print_info "Creating initial migration..."
        docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations add InitialCreate'
        print_success "Initial migration created"
    else
        print_info "Migrations directory already exists"
    fi
}

# Function to apply migrations
apply_migrations() {
    print_info "Applying database migrations..."
    
    if docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update'; then
        print_success "Migrations applied successfully"
    else
        print_error "Failed to apply migrations"
        exit 1
    fi
}

# Function to verify tables were created
verify_tables() {
    print_info "Verifying database schema..."
    
    echo "Tables in database:"
    docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev -c "\dt"
    
    # Check for ASP.NET Identity tables in auth schema
    echo "Tables in auth schema:"
    docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev -c "SELECT tablename FROM pg_tables WHERE schemaname = 'auth';"
    
    if docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev -c "SELECT tablename FROM pg_tables WHERE schemaname = 'auth';" | grep -q "Users"; then
        print_success "ASP.NET Identity tables created successfully in auth schema"
    else
        print_warning "ASP.NET Identity tables not found in auth schema"
    fi
}

# Function to show migration status
show_migration_status() {
    print_info "Migration status:"
    
    if docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations list' > /dev/null 2>&1; then
        docker-compose exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations list'
    else
        print_warning "Cannot retrieve migration status"
    fi
}

# Function to test authentication endpoints
test_auth_endpoints() {
    print_info "Testing authentication endpoints..."
    
    # Wait for API to be ready
    sleep 5
    
    # Test health endpoint
    if curl -f http://localhost:5655/health > /dev/null 2>&1; then
        print_success "API health endpoint accessible"
    else
        print_warning "API health endpoint not accessible"
    fi
    
    # Test auth health endpoint
    if curl -f http://localhost:5655/api/auth/health > /dev/null 2>&1; then
        print_success "Auth health endpoint accessible"
    else
        print_warning "Auth health endpoint not accessible"
    fi
}

# Main execution
main() {
    print_info "WitchCityRope Docker Migration Script"
    print_info "====================================="
    
    check_containers
    check_database
    install_ef_tools
    list_migrations
    create_initial_migration
    apply_migrations
    verify_tables
    show_migration_status
    test_auth_endpoints
    
    print_success "Migration process completed!"
    print_info "You can now test authentication endpoints:"
    echo "  curl http://localhost:5655/health"
    echo "  curl http://localhost:5655/api/auth/health"
}

# Handle script arguments
case "${1:-main}" in
    "check")
        check_containers
        check_database
        ;;
    "install")
        install_ef_tools
        ;;
    "list")
        list_migrations
        ;;
    "migrate")
        apply_migrations
        ;;
    "verify")
        verify_tables
        ;;
    "status")
        show_migration_status
        ;;
    "test")
        test_auth_endpoints
        ;;
    "main"|"")
        main
        ;;
    *)
        echo "Usage: $0 [check|install|list|migrate|verify|status|test]"
        echo "  check   - Check container and database status"
        echo "  install - Install EF Core tools"
        echo "  list    - List available migrations"
        echo "  migrate - Apply migrations"
        echo "  verify  - Verify database tables"
        echo "  status  - Show migration status"
        echo "  test    - Test authentication endpoints"
        echo "  (no arg) - Run full migration process"
        exit 1
        ;;
esac