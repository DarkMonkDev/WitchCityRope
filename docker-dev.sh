#!/bin/bash
# Docker Development Helper Script for Witch City Rope

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker Desktop and try again."
        exit 1
    fi
    print_info "Docker is running."
}

# Function to check if .env file exists
check_env_file() {
    if [ ! -f .env ]; then
        print_warning ".env file not found. Creating from .env.example..."
        cp .env.example .env
        print_info "Please edit .env file with your configuration values."
        read -p "Press enter to continue after editing .env file..."
    else
        print_info ".env file found."
    fi
}

# Function to generate development certificates
generate_certs() {
    print_info "Generating development certificates..."
    mkdir -p ~/.aspnet/https
    
    if [ ! -f ~/.aspnet/https/aspnetapp.pfx ]; then
        docker run --rm -v ~/.aspnet/https:/https mcr.microsoft.com/dotnet/sdk:9.0 \
            sh -c "dotnet dev-certs https -ep /https/aspnetapp.pfx -p password && \
                   dotnet dev-certs https --trust"
        print_info "Development certificates generated."
    else
        print_info "Development certificates already exist."
    fi
}

# Function to initialize database
init_database() {
    print_info "Initializing database..."
    docker-compose up -d db-init
    sleep 2
    docker-compose logs db-init
}

# Main menu
show_menu() {
    echo ""
    echo "Witch City Rope Docker Development Helper"
    echo "========================================"
    echo "1) Start development environment"
    echo "2) Start with dev tools (mailcatcher, sqlite viewer)"
    echo "3) Stop all services"
    echo "4) View logs"
    echo "5) Run database migrations"
    echo "6) Run tests"
    echo "7) Clean up (remove all data)"
    echo "8) Rebuild containers"
    echo "9) Access container shell"
    echo "0) Exit"
    echo ""
}

# Start development environment
start_dev() {
    check_docker
    check_env_file
    generate_certs
    init_database
    
    print_info "Starting development environment..."
    docker-compose up -d
    
    print_info "Services are starting up..."
    sleep 5
    
    echo ""
    print_info "Development environment is ready!"
    echo ""
    echo "Access the applications at:"
    echo "  - Web (Blazor): http://localhost:5651"
    echo "  - Web (HTTPS):  https://localhost:5652"
    echo "  - API:          http://localhost:5653"
    echo "  - API (HTTPS):  https://localhost:5654"
    echo ""
}

# Start with dev tools
start_with_tools() {
    check_docker
    check_env_file
    generate_certs
    init_database
    
    print_info "Starting development environment with dev tools..."
    COMPOSE_PROFILES=dev-tools,debug docker-compose up -d
    
    print_info "Services are starting up..."
    sleep 5
    
    echo ""
    print_info "Development environment is ready!"
    echo ""
    echo "Access the applications at:"
    echo "  - Web (Blazor):  http://localhost:5651"
    echo "  - API:           http://localhost:5653"
    echo "  - Mailcatcher:   http://localhost:1080"
    echo ""
}

# View logs
view_logs() {
    echo "Select service to view logs:"
    echo "1) All services"
    echo "2) API"
    echo "3) Web"
    echo "4) Database init"
    read -p "Choice: " log_choice
    
    case $log_choice in
        1) docker-compose logs -f ;;
        2) docker-compose logs -f api ;;
        3) docker-compose logs -f web ;;
        4) docker-compose logs db-init ;;
        *) print_error "Invalid choice" ;;
    esac
}

# Run migrations
run_migrations() {
    print_info "Running database migrations..."
    docker-compose exec api dotnet ef database update \
        --project /src/WitchCityRope.Infrastructure \
        --startup-project /src/WitchCityRope.Api
    print_info "Migrations completed."
}

# Run tests
run_tests() {
    echo "Select test suite:"
    echo "1) API unit tests"
    echo "2) Web unit tests"
    echo "3) Integration tests"
    echo "4) All tests"
    read -p "Choice: " test_choice
    
    case $test_choice in
        1) 
            print_info "Running API unit tests..."
            docker-compose exec api dotnet test /workspace/tests/WitchCityRope.Api.Tests
            ;;
        2) 
            print_info "Running Web unit tests..."
            docker-compose exec web dotnet test /workspace/tests/WitchCityRope.Web.Tests
            ;;
        3) 
            print_info "Running integration tests..."
            docker-compose exec api dotnet test /workspace/tests/WitchCityRope.IntegrationTests
            ;;
        4) 
            print_info "Running all tests..."
            docker-compose exec api dotnet test /workspace/tests
            ;;
        *) print_error "Invalid choice" ;;
    esac
}

# Access shell
access_shell() {
    echo "Select container:"
    echo "1) API"
    echo "2) Web"
    echo "3) SQLite viewer"
    read -p "Choice: " shell_choice
    
    case $shell_choice in
        1) docker-compose exec api /bin/bash ;;
        2) docker-compose exec web /bin/bash ;;
        3) docker-compose exec sqlite-viewer /bin/sh ;;
        *) print_error "Invalid choice" ;;
    esac
}

# Clean up
cleanup() {
    print_warning "This will remove all containers, volumes, and data!"
    read -p "Are you sure? (y/N): " confirm
    
    if [ "$confirm" = "y" ] || [ "$confirm" = "Y" ]; then
        print_info "Stopping all services..."
        docker-compose down -v
        
        print_info "Removing data directories..."
        rm -rf ./data ./logs ./uploads
        
        print_info "Cleanup completed."
    else
        print_info "Cleanup cancelled."
    fi
}

# Rebuild containers
rebuild() {
    print_info "Rebuilding containers..."
    docker-compose build --no-cache
    print_info "Rebuild completed."
}

# Main loop
while true; do
    show_menu
    read -p "Select an option: " choice
    
    case $choice in
        1) start_dev ;;
        2) start_with_tools ;;
        3) 
            print_info "Stopping all services..."
            docker-compose down
            print_info "Services stopped."
            ;;
        4) view_logs ;;
        5) run_migrations ;;
        6) run_tests ;;
        7) cleanup ;;
        8) rebuild ;;
        9) access_shell ;;
        0) 
            print_info "Exiting..."
            exit 0
            ;;
        *) print_error "Invalid option. Please try again." ;;
    esac
done