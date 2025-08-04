#!/bin/bash

# Development helper script for WitchCityRope

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_color() {
    color=$1
    message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check if docker is running
check_docker() {
    if ! docker info >/dev/null 2>&1; then
        print_color $RED "Docker is not running. Please start Docker first."
        exit 1
    fi
}

# Main menu
show_menu() {
    echo ""
    print_color $GREEN "WitchCityRope Development Tools"
    echo "================================"
    echo "1) Start development environment (with hot reload)"
    echo "2) Stop all containers"
    echo "3) Restart containers"
    echo "4) View logs (all services)"
    echo "5) View web logs only"
    echo "6) View API logs only"
    echo "7) Rebuild and restart (when hot reload fails)"
    echo "8) Clean rebuild (remove volumes)"
    echo "9) Run database migrations"
    echo "10) Access database shell"
    echo "q) Quit"
    echo ""
}

# Start development environment
start_dev() {
    print_color $GREEN "Starting development environment with hot reload..."
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
    print_color $GREEN "Services started!"
    echo ""
    echo "Access the application at:"
    echo "  Web: http://localhost:5651"
    echo "  API: http://localhost:5653"
    echo "  Database: localhost:5433"
}

# Stop all containers
stop_all() {
    print_color $YELLOW "Stopping all containers..."
    docker-compose down
    print_color $GREEN "All containers stopped."
}

# Restart containers
restart_containers() {
    print_color $YELLOW "Restarting containers..."
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart
    print_color $GREEN "Containers restarted."
}

# View logs
view_logs() {
    print_color $GREEN "Showing logs (Ctrl+C to exit)..."
    docker-compose logs -f
}

# View web logs
view_web_logs() {
    print_color $GREEN "Showing web logs (Ctrl+C to exit)..."
    docker-compose logs -f web
}

# View API logs
view_api_logs() {
    print_color $GREEN "Showing API logs (Ctrl+C to exit)..."
    docker-compose logs -f api
}

# Rebuild and restart
rebuild_restart() {
    print_color $YELLOW "Rebuilding and restarting containers..."
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
    print_color $GREEN "Containers rebuilt and started!"
}

# Clean rebuild
clean_rebuild() {
    print_color $RED "WARNING: This will remove all data volumes!"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_color $YELLOW "Performing clean rebuild..."
        docker-compose down -v
        docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
        print_color $GREEN "Clean rebuild complete!"
    else
        print_color $YELLOW "Clean rebuild cancelled."
    fi
}

# Run migrations
run_migrations() {
    print_color $GREEN "Running database migrations..."
    docker-compose exec web dotnet ef database update
    print_color $GREEN "Migrations complete!"
}

# Access database
access_db() {
    print_color $GREEN "Accessing PostgreSQL shell..."
    docker-compose exec postgres psql -U postgres -d witchcityrope_db
}

# Main loop
check_docker

while true; do
    show_menu
    read -p "Select an option: " choice
    
    case $choice in
        1) start_dev ;;
        2) stop_all ;;
        3) restart_containers ;;
        4) view_logs ;;
        5) view_web_logs ;;
        6) view_api_logs ;;
        7) rebuild_restart ;;
        8) clean_rebuild ;;
        9) run_migrations ;;
        10) access_db ;;
        q|Q) 
            print_color $GREEN "Goodbye!"
            exit 0 
            ;;
        *)
            print_color $RED "Invalid option. Please try again."
            ;;
    esac
    
    if [[ $choice =~ ^[4-6]$ ]] || [ "$choice" = "10" ]; then
        # Don't show menu immediately after logs or database access
        continue
    else
        # Wait a moment before showing menu again
        sleep 1
    fi
done