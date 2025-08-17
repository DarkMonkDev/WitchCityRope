#!/bin/bash
# WitchCityRope - Docker Environment Stopper
# Description: Stop containers with optional cleanup

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
print_status() {
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

show_help() {
    echo "WitchCityRope Docker Environment Stopper"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help       Show this help message"
    echo "  -v, --volumes    Remove volumes (⚠️  DELETES DATABASE DATA!)"
    echo "  -f, --force      Force stop (kill containers immediately)"
    echo "  -a, --all        Stop all environments (dev, test, prod)"
    echo "  --confirm        Skip confirmation prompts"
    echo ""
    echo "Examples:"
    echo "  $0               # Graceful stop"
    echo "  $0 --force       # Force stop containers"
    echo "  $0 --volumes     # Stop and remove volumes (with confirmation)"
    echo "  $0 --all         # Stop all environments"
}

confirm_action() {
    local message="$1"
    local default="${2:-N}"
    
    if [ "$skip_confirmation" = true ]; then
        return 0
    fi
    
    echo -e "${YELLOW}[CONFIRM]${NC} $message"
    if [ "$default" = "Y" ]; then
        echo "Continue? (Y/n)"
        read -r response
        [[ -z "$response" || "$response" =~ ^[Yy]$ ]]
    else
        echo "Continue? (y/N)"
        read -r response
        [[ "$response" =~ ^[Yy]$ ]]
    fi
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed or not in PATH"
        exit 1
    fi

    if ! docker info &> /dev/null; then
        print_error "Docker daemon is not running"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed or not in PATH"
        exit 1
    fi
}

get_running_containers() {
    docker-compose ps -q 2>/dev/null || echo ""
}

stop_services() {
    local compose_files="-f docker-compose.yml -f docker-compose.dev.yml"
    
    if [ "$stop_all_environments" = true ]; then
        # Try to stop all possible environments
        print_status "Stopping all environments..."
        
        # Development
        if docker-compose -f docker-compose.yml -f docker-compose.dev.yml ps -q &> /dev/null; then
            print_status "Stopping development environment..."
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml down $stop_options
        fi
        
        # Test
        if docker-compose -f docker-compose.yml -f docker-compose.test.yml ps -q &> /dev/null; then
            print_status "Stopping test environment..."
            docker-compose -f docker-compose.yml -f docker-compose.test.yml down $stop_options
        fi
        
        # Production
        if docker-compose -f docker-compose.yml -f docker-compose.prod.yml ps -q &> /dev/null; then
            print_status "Stopping production environment..."
            docker-compose -f docker-compose.yml -f docker-compose.prod.yml down $stop_options
        fi
        
        # Base
        if docker-compose -f docker-compose.yml ps -q &> /dev/null; then
            print_status "Stopping base environment..."
            docker-compose -f docker-compose.yml down $stop_options
        fi
    else
        # Stop development environment (default)
        local running_containers=$(get_running_containers)
        
        if [ -z "$running_containers" ]; then
            print_warning "No containers are currently running"
            return 0
        fi
        
        print_status "Stopping development environment..."
        
        if [ "$force_stop" = true ]; then
            print_status "Force stopping containers..."
            docker-compose kill
        fi
        
        docker-compose $compose_files down $stop_options
    fi
}

show_final_status() {
    local running_containers=$(get_running_containers)
    
    if [ -z "$running_containers" ]; then
        print_success "All WitchCityRope containers have been stopped"
    else
        print_warning "Some containers are still running:"
        docker-compose ps
        echo ""
        print_status "To force stop remaining containers:"
        echo "  docker-compose kill"
        echo "  docker-compose down -v --rmi all"
    fi
    
    if [ "$remove_volumes" = true ]; then
        print_warning "Database volumes have been removed - all data is lost!"
        echo "To restore from backup:"
        echo "  ./scripts/docker-dev.sh"
        echo "  # Then restore your database backup"
    fi
}

# Parse command line arguments
remove_volumes=false
force_stop=false
stop_all_environments=false
skip_confirmation=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -v|--volumes)
            remove_volumes=true
            shift
            ;;
        -f|--force)
            force_stop=true
            shift
            ;;
        -a|--all)
            stop_all_environments=true
            shift
            ;;
        --confirm)
            skip_confirmation=true
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Build stop options
stop_options=""
if [ "$remove_volumes" = true ]; then
    stop_options="$stop_options -v"
fi

# Main execution
print_status "Stopping WitchCityRope containers..."

# Pre-flight checks
check_docker

# Change to project root
cd "$(dirname "$0")/.."

# Confirmation for destructive operations
if [ "$remove_volumes" = true ]; then
    if ! confirm_action "⚠️  This will PERMANENTLY DELETE all database data! Are you sure?"; then
        print_error "Aborted by user"
        exit 1
    fi
fi

# Stop services
stop_services

# Show final status
show_final_status

print_success "Stop operation completed!"