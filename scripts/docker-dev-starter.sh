#!/bin/bash
# WitchCityRope - Docker Development Environment Starter
# Description: Start development environment with hot reload and debugging

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
    echo "WitchCityRope Docker Development Environment"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help     Show this help message"
    echo "  -b, --build    Force rebuild images before starting"
    echo "  -l, --logs     Show logs after starting"
    echo "  -s, --status   Show container status after starting"
    echo "  --clean        Clean build (no cache)"
    echo ""
    echo "Examples:"
    echo "  $0              # Start development environment"
    echo "  $0 --build      # Rebuild and start"
    echo "  $0 --logs       # Start and follow logs"
    echo "  $0 --clean      # Clean rebuild and start"
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

check_ports() {
    local ports=(5173 5655 5433)
    local occupied_ports=()

    for port in "${ports[@]}"; do
        if netstat -tlnp 2>/dev/null | grep ":$port " &> /dev/null; then
            occupied_ports+=($port)
        fi
    done

    if [ ${#occupied_ports[@]} -gt 0 ]; then
        print_warning "The following ports are in use: ${occupied_ports[*]}"
        echo "This may cause conflicts with Docker containers."
        echo "Would you like to continue anyway? (y/N)"
        read -r response
        if [[ ! "$response" =~ ^[Yy]$ ]]; then
            print_error "Aborted by user"
            exit 1
        fi
    fi
}

build_images() {
    local build_args=""
    if [ "$clean_build" = true ]; then
        build_args="--no-cache"
        print_status "Building images with no cache..."
    else
        print_status "Building images..."
    fi

    if docker-compose -f docker-compose.yml -f docker-compose.dev.yml build $build_args; then
        print_success "Images built successfully"
    else
        print_error "Failed to build images"
        exit 1
    fi
}

start_services() {
    print_status "Starting development environment..."
    
    if docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d; then
        print_success "Development environment started"
    else
        print_error "Failed to start development environment"
        exit 1
    fi
}

wait_for_services() {
    print_status "Waiting for services to be ready..."
    
    local max_attempts=30
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        attempt=$((attempt + 1))
        
        # Check if all containers are running
        local running_containers=$(docker-compose ps -q | wc -l)
        local healthy_containers=0
        
        # Check PostgreSQL
        if docker-compose exec -T postgres pg_isready -U postgres -d witchcityrope_dev &> /dev/null; then
            healthy_containers=$((healthy_containers + 1))
        fi
        
        # Check API
        if curl -f http://localhost:5655/health &> /dev/null; then
            healthy_containers=$((healthy_containers + 1))
        fi
        
        # Check React app
        if curl -f http://localhost:5173 &> /dev/null; then
            healthy_containers=$((healthy_containers + 1))
        fi
        
        if [ $healthy_containers -eq 3 ]; then
            print_success "All services are ready!"
            return 0
        fi
        
        echo -n "."
        sleep 2
    done
    
    print_warning "Services may not be fully ready yet. Check logs for details."
}

show_status() {
    print_status "Container Status:"
    docker-compose ps
    
    echo ""
    print_status "Service URLs:"
    echo "  React App:    http://localhost:5173"
    echo "  API Health:   http://localhost:5655/health"
    echo "  Auth Health:  http://localhost:5655/api/auth/health"
    echo "  Database:     localhost:5433 (postgres/WitchCity2024!)"
    
    echo ""
    print_status "Useful Commands:"
    echo "  View logs:    docker-compose logs -f"
    echo "  Stop:         docker-compose down"
    echo "  Restart API:  docker-compose restart api"
    echo "  Shell into:   docker-compose exec <service> bash"
}

show_logs() {
    print_status "Following logs for all services (Ctrl+C to exit)..."
    docker-compose logs -f
}

# Parse command line arguments
force_build=false
show_logs_after=false
show_status_after=true
clean_build=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -b|--build)
            force_build=true
            shift
            ;;
        -l|--logs)
            show_logs_after=true
            shift
            ;;
        -s|--status)
            show_status_after=true
            shift
            ;;
        --clean)
            clean_build=true
            force_build=true
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
print_status "Starting WitchCityRope development environment..."

# Pre-flight checks
check_docker
check_ports

# Change to project root
cd "$(dirname "$0")/.."

# Build if requested
if [ "$force_build" = true ]; then
    build_images
fi

# Start services
start_services

# Wait for services to be ready
wait_for_services

# Show status
if [ "$show_status_after" = true ]; then
    show_status
fi

# Show logs if requested
if [ "$show_logs_after" = true ]; then
    echo ""
    show_logs
fi

print_success "Development environment is ready!"