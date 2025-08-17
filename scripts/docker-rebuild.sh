#!/bin/bash
# WitchCityRope - Docker Service Rebuilder
# Description: Rebuild specific services with advanced options

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
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
    echo "WitchCityRope Docker Service Rebuilder"
    echo ""
    echo "Usage: $0 [SERVICE] [OPTIONS]"
    echo ""
    echo "Services:"
    echo "  web, react     React frontend application"
    echo "  api            .NET API backend"
    echo "  postgres, db   PostgreSQL database"
    echo "  all            All services (default)"
    echo ""
    echo "Options:"
    echo "  -h, --help         Show this help message"
    echo "  --no-cache         Build without using cache"
    echo "  --pull             Pull latest base images before building"
    echo "  --restart          Restart service after rebuild"
    echo "  --logs             Show logs after rebuild"
    echo "  --target TARGET    Build specific target (development/production)"
    echo "  --force            Force rebuild even if no changes detected"
    echo "  --parallel         Build multiple services in parallel"
    echo "  --verbose          Show detailed build output"
    echo ""
    echo "Examples:"
    echo "  $0 api                    # Rebuild API service"
    echo "  $0 all --no-cache         # Rebuild all services without cache"
    echo "  $0 web --restart --logs   # Rebuild React app, restart, and show logs"
    echo "  $0 api --target production # Build API production target"
    echo "  $0 --parallel --pull      # Rebuild all services in parallel with latest images"
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

get_service_name() {
    local input="$1"
    
    case $input in
        web|react|frontend)
            echo "web"
            ;;
        api|backend|dotnet)
            echo "api"
            ;;
        postgres|db|database)
            echo "postgres"
            ;;
        all|"")
            echo "all"
            ;;
        *)
            print_error "Unknown service: $input"
            echo "Available services: web, api, postgres, all"
            exit 1
            ;;
    esac
}

get_compose_files() {
    echo "-f docker-compose.yml -f docker-compose.dev.yml"
}

build_docker_command() {
    local service="$1"
    local cmd="docker-compose $(get_compose_files) build"
    
    # Add build options
    if [ "$no_cache" = true ]; then
        cmd="$cmd --no-cache"
    fi
    
    if [ "$pull_images" = true ]; then
        cmd="$cmd --pull"
    fi
    
    if [ "$parallel_build" = true ] && [ "$service" = "all" ]; then
        cmd="$cmd --parallel"
    fi
    
    if [ "$force_rebuild" = true ]; then
        cmd="$cmd --force-rm"
    fi
    
    # Add build args for target
    if [ -n "$build_target" ]; then
        case $service in
            web|api)
                cmd="$cmd --build-arg TARGET=$build_target"
                ;;
        esac
    fi
    
    # Add service if not all
    if [ "$service" != "all" ]; then
        cmd="$cmd $service"
    fi
    
    echo "$cmd"
}

check_service_changes() {
    local service="$1"
    
    # Get the context directory for the service
    local context_dir=""
    case $service in
        web)
            context_dir="./apps/web"
            ;;
        api)
            context_dir="./apps/api"
            ;;
        postgres)
            # Postgres uses base image, rarely needs rebuilding
            return 0
            ;;
        all)
            # Always rebuild all if requested
            return 0
            ;;
    esac
    
    if [ ! -d "$context_dir" ]; then
        print_warning "Context directory not found: $context_dir"
        return 0
    fi
    
    # Check for recent changes (last 24 hours)
    local recent_changes=$(find "$context_dir" -type f -mtime -1 2>/dev/null | wc -l)
    
    if [ "$recent_changes" -gt 0 ]; then
        print_status "Found $recent_changes recent changes in $service"
        return 0
    elif [ "$force_rebuild" = true ]; then
        print_status "Forcing rebuild of $service (--force specified)"
        return 0
    else
        print_warning "No recent changes detected in $service (use --force to rebuild anyway)"
        return 1
    fi
}

show_build_info() {
    local service="$1"
    
    print_status "Build Information:"
    echo "  Service: $service"
    
    if [ "$no_cache" = true ]; then
        echo "  Cache: disabled"
    else
        echo "  Cache: enabled"
    fi
    
    if [ "$pull_images" = true ]; then
        echo "  Base images: will pull latest"
    fi
    
    if [ -n "$build_target" ]; then
        echo "  Target: $build_target"
    fi
    
    if [ "$parallel_build" = true ] && [ "$service" = "all" ]; then
        echo "  Mode: parallel build"
    fi
    
    echo ""
}

stop_service() {
    local service="$1"
    
    print_status "Stopping $service..."
    
    if [ "$service" = "all" ]; then
        docker-compose $(get_compose_files) down
    else
        docker-compose $(get_compose_files) stop "$service"
    fi
}

build_service() {
    local service="$1"
    local build_cmd=$(build_docker_command "$service")
    
    print_status "Building $service..."
    
    if [ "$verbose" = true ]; then
        print_status "Command: $build_cmd"
    fi
    
    # Execute build command
    if [ "$verbose" = true ]; then
        eval "$build_cmd"
    else
        eval "$build_cmd" 2>&1 | while read -r line; do
            # Show progress indicators for non-verbose mode
            if echo "$line" | grep -q -E "(Step [0-9]+|Successfully built|Successfully tagged)"; then
                echo "  $line"
            fi
        done
    fi
    
    if [ $? -eq 0 ]; then
        print_success "$service built successfully"
        return 0
    else
        print_error "$service build failed"
        return 1
    fi
}

start_service() {
    local service="$1"
    
    print_status "Starting $service..."
    
    if [ "$service" = "all" ]; then
        docker-compose $(get_compose_files) up -d
    else
        docker-compose $(get_compose_files) up -d "$service"
    fi
    
    # Wait a moment for service to start
    sleep 3
    
    # Check if service started successfully
    if [ "$service" = "all" ]; then
        local running_services=$(docker-compose ps --services --filter status=running | wc -l)
        local total_services=$(docker-compose ps --services | wc -l)
        
        if [ "$running_services" -eq "$total_services" ]; then
            print_success "All services started successfully"
        else
            print_warning "$running_services/$total_services services started"
        fi
    else
        if docker-compose ps "$service" | grep -q "Up"; then
            print_success "$service started successfully"
        else
            print_warning "$service may not have started correctly"
        fi
    fi
}

show_logs() {
    local service="$1"
    
    print_status "Showing logs for $service (Ctrl+C to exit)..."
    sleep 1
    
    if [ "$service" = "all" ]; then
        docker-compose $(get_compose_files) logs -f
    else
        docker-compose $(get_compose_files) logs -f "$service"
    fi
}

interactive_service_selection() {
    echo "Select a service to rebuild:"
    echo ""
    echo "1) All services"
    echo "2) React app (web)"
    echo "3) API backend (api)"
    echo "4) PostgreSQL database (postgres)"
    echo "0) Exit"
    echo ""
    echo -n "Enter your choice (0-4): "
    read -r choice
    
    case $choice in
        1)
            echo "all"
            ;;
        2)
            echo "web"
            ;;
        3)
            echo "api"
            ;;
        4)
            echo "postgres"
            ;;
        0)
            print_status "Cancelled"
            exit 0
            ;;
        *)
            print_error "Invalid choice: $choice"
            exit 1
            ;;
    esac
}

show_final_status() {
    local service="$1"
    
    echo ""
    print_status "Final Status:"
    
    if [ "$service" = "all" ]; then
        docker-compose ps
    else
        docker-compose ps "$service"
    fi
    
    echo ""
    print_status "Service URLs:"
    echo "  React App:    http://localhost:5173"
    echo "  API Health:   http://localhost:5655/health"
    echo "  Auth Health:  http://localhost:5655/api/auth/health"
    
    echo ""
    print_status "Next steps:"
    echo "  Check logs:   ./scripts/docker-logs.sh $service"
    echo "  Health check: ./scripts/docker-health.sh"
    echo "  Stop:         ./scripts/docker-stop.sh"
}

# Parse command line arguments
service=""
no_cache=false
pull_images=false
restart_after=false
show_logs_after=false
build_target=""
force_rebuild=false
parallel_build=false
verbose=false

# Parse service name first (if provided as first argument)
if [[ $# -gt 0 && ! "$1" =~ ^- ]]; then
    service=$(get_service_name "$1")
    shift
fi

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        --no-cache)
            no_cache=true
            shift
            ;;
        --pull)
            pull_images=true
            shift
            ;;
        --restart)
            restart_after=true
            shift
            ;;
        --logs)
            show_logs_after=true
            restart_after=true  # Need to restart to show logs
            shift
            ;;
        --target)
            if [[ $# -lt 2 ]]; then
                print_error "--target requires a value (development/production)"
                exit 1
            fi
            build_target="$2"
            shift 2
            ;;
        --force)
            force_rebuild=true
            shift
            ;;
        --parallel)
            parallel_build=true
            shift
            ;;
        --verbose)
            verbose=true
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main execution
print_status "WitchCityRope Docker Service Rebuilder"

# Pre-flight checks
check_docker

# Change to project root
cd "$(dirname "$0")/.."

# Interactive service selection if none provided
if [ -z "$service" ]; then
    service=$(interactive_service_selection)
fi

# Show build information
show_build_info "$service"

# Check if rebuild is necessary (unless forced)
if [ "$force_rebuild" = false ]; then
    if ! check_service_changes "$service"; then
        echo "Use --force to rebuild anyway."
        exit 0
    fi
fi

# Stop service before rebuilding
if [ "$restart_after" = true ]; then
    stop_service "$service"
fi

# Build the service
if build_service "$service"; then
    print_success "Rebuild completed successfully"
else
    print_error "Rebuild failed"
    exit 1
fi

# Restart service if requested
if [ "$restart_after" = true ]; then
    start_service "$service"
fi

# Show final status
show_final_status "$service"

# Show logs if requested
if [ "$show_logs_after" = true ]; then
    echo ""
    show_logs "$service"
fi

print_success "Rebuild operation completed!"