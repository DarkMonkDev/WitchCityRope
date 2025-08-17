#!/bin/bash
# WitchCityRope - Docker Environment Cleaner
# Description: Comprehensive cleanup of containers, volumes, and images

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
    echo "WitchCityRope Docker Environment Cleaner"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help          Show this help message"
    echo "  -a, --all           Remove everything (containers, volumes, images, networks)"
    echo "  -c, --containers    Remove containers only"
    echo "  -v, --volumes       Remove volumes (⚠️  DELETES DATABASE DATA!)"
    echo "  -i, --images        Remove project images"
    echo "  -n, --networks      Remove project networks"
    echo "  -s, --system        Clean Docker system (unused containers, networks, images)"
    echo "  --force             Skip all confirmation prompts"
    echo "  --dry-run           Show what would be removed without actually removing"
    echo ""
    echo "Examples:"
    echo "  $0                  # Interactive cleanup"
    echo "  $0 --all            # Remove everything (with confirmation)"
    echo "  $0 --containers     # Remove containers only"
    echo "  $0 --volumes --force # Remove volumes without confirmation"
    echo "  $0 --dry-run        # Show what would be removed"
}

confirm_action() {
    local message="$1"
    local default="${2:-N}"
    
    if [ "$force_cleanup" = true ]; then
        return 0
    fi
    
    if [ "$dry_run" = true ]; then
        echo -e "${BLUE}[DRY-RUN]${NC} Would execute: $message"
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

get_project_resources() {
    local resource_type="$1"
    
    case $resource_type in
        containers)
            docker-compose -f docker-compose.yml ps -a -q 2>/dev/null || echo ""
            ;;
        images)
            docker images --filter "reference=witchcityrope/*" --filter "reference=*witchcityrope*" -q 2>/dev/null || echo ""
            ;;
        volumes)
            docker volume ls --filter "name=witchcityrope" -q 2>/dev/null || echo ""
            ;;
        networks)
            docker network ls --filter "name=witchcityrope" -q 2>/dev/null || echo ""
            ;;
    esac
}

show_current_resources() {
    print_status "Current WitchCityRope Docker resources:"
    
    echo ""
    echo "📦 Containers:"
    local containers=$(get_project_resources containers)
    if [ -n "$containers" ]; then
        docker-compose ps -a 2>/dev/null || echo "  (Error reading containers)"
    else
        echo "  No containers found"
    fi
    
    echo ""
    echo "🖼️  Images:"
    local images=$(get_project_resources images)
    if [ -n "$images" ]; then
        docker images --filter "reference=witchcityrope/*" --filter "reference=*witchcityrope*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}" 2>/dev/null || echo "  (Error reading images)"
    else
        echo "  No project images found"
    fi
    
    echo ""
    echo "💾 Volumes:"
    local volumes=$(get_project_resources volumes)
    if [ -n "$volumes" ]; then
        docker volume ls --filter "name=witchcityrope" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || echo "  (Error reading volumes)"
    else
        echo "  No project volumes found"
    fi
    
    echo ""
    echo "🌐 Networks:"
    local networks=$(get_project_resources networks)
    if [ -n "$networks" ]; then
        docker network ls --filter "name=witchcityrope" --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}" 2>/dev/null || echo "  (Error reading networks)"
    else
        echo "  No project networks found"
    fi
}

stop_containers() {
    print_status "Stopping containers..."
    
    if [ "$dry_run" = true ]; then
        echo -e "${BLUE}[DRY-RUN]${NC} Would stop all containers"
        return 0
    fi
    
    # Try all environment configurations
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml down 2>/dev/null || true
    docker-compose -f docker-compose.yml -f docker-compose.test.yml down 2>/dev/null || true
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml down 2>/dev/null || true
    docker-compose -f docker-compose.yml down 2>/dev/null || true
    
    print_success "Containers stopped"
}

remove_containers() {
    local containers=$(get_project_resources containers)
    
    if [ -z "$containers" ]; then
        print_status "No containers to remove"
        return 0
    fi
    
    if confirm_action "Remove project containers"; then
        if [ "$dry_run" = true ]; then
            echo -e "${BLUE}[DRY-RUN]${NC} Would remove containers: $containers"
            return 0
        fi
        
        print_status "Removing containers..."
        echo "$containers" | xargs -r docker rm -f
        print_success "Containers removed"
    fi
}

remove_volumes() {
    local volumes=$(get_project_resources volumes)
    
    if [ -z "$volumes" ]; then
        print_status "No volumes to remove"
        return 0
    fi
    
    if confirm_action "⚠️  Remove volumes (THIS WILL DELETE ALL DATABASE DATA!)"; then
        if [ "$dry_run" = true ]; then
            echo -e "${BLUE}[DRY-RUN]${NC} Would remove volumes: $volumes"
            return 0
        fi
        
        print_status "Removing volumes..."
        echo "$volumes" | xargs -r docker volume rm
        print_success "Volumes removed"
        print_warning "All database data has been permanently deleted!"
    fi
}

remove_images() {
    local images=$(get_project_resources images)
    
    if [ -z "$images" ]; then
        print_status "No project images to remove"
        return 0
    fi
    
    if confirm_action "Remove project images"; then
        if [ "$dry_run" = true ]; then
            echo -e "${BLUE}[DRY-RUN]${NC} Would remove images: $images"
            return 0
        fi
        
        print_status "Removing images..."
        echo "$images" | xargs -r docker rmi -f
        print_success "Images removed"
    fi
}

remove_networks() {
    local networks=$(get_project_resources networks)
    
    if [ -z "$networks" ]; then
        print_status "No project networks to remove"
        return 0
    fi
    
    if confirm_action "Remove project networks"; then
        if [ "$dry_run" = true ]; then
            echo -e "${BLUE}[DRY-RUN]${NC} Would remove networks: $networks"
            return 0
        fi
        
        print_status "Removing networks..."
        echo "$networks" | xargs -r docker network rm
        print_success "Networks removed"
    fi
}

system_cleanup() {
    if confirm_action "Clean Docker system (remove unused containers, networks, images)"; then
        if [ "$dry_run" = true ]; then
            echo -e "${BLUE}[DRY-RUN]${NC} Would run: docker system prune -f"
            return 0
        fi
        
        print_status "Cleaning Docker system..."
        docker system prune -f
        print_success "System cleanup completed"
    fi
}

interactive_cleanup() {
    echo ""
    print_status "Interactive cleanup mode"
    echo "What would you like to clean up?"
    echo ""
    echo "1) Containers only"
    echo "2) Volumes only (⚠️  DELETES DATABASE DATA!)"
    echo "3) Images only"
    echo "4) Networks only"
    echo "5) Everything (containers + volumes + images + networks)"
    echo "6) System cleanup (Docker-wide unused resources)"
    echo "7) Show current resources and exit"
    echo "0) Exit without doing anything"
    echo ""
    echo -n "Enter your choice (0-7): "
    read -r choice
    
    case $choice in
        1)
            remove_containers=true
            ;;
        2)
            remove_volumes=true
            ;;
        3)
            remove_images=true
            ;;
        4)
            remove_networks=true
            ;;
        5)
            remove_containers=true
            remove_volumes=true
            remove_images=true
            remove_networks=true
            ;;
        6)
            system_cleanup_only=true
            ;;
        7)
            show_current_resources
            exit 0
            ;;
        0)
            print_status "Cleanup cancelled"
            exit 0
            ;;
        *)
            print_error "Invalid choice: $choice"
            exit 1
            ;;
    esac
}

# Parse command line arguments
remove_containers=false
remove_volumes=false
remove_images=false
remove_networks=false
force_cleanup=false
dry_run=false
system_cleanup_only=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -a|--all)
            remove_containers=true
            remove_volumes=true
            remove_images=true
            remove_networks=true
            shift
            ;;
        -c|--containers)
            remove_containers=true
            shift
            ;;
        -v|--volumes)
            remove_volumes=true
            shift
            ;;
        -i|--images)
            remove_images=true
            shift
            ;;
        -n|--networks)
            remove_networks=true
            shift
            ;;
        -s|--system)
            system_cleanup_only=true
            shift
            ;;
        --force)
            force_cleanup=true
            shift
            ;;
        --dry-run)
            dry_run=true
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
if [ "$dry_run" = true ]; then
    print_status "🧪 DRY RUN MODE - No changes will be made"
fi

print_status "WitchCityRope Docker Cleanup Tool"

# Pre-flight checks
check_docker

# Change to project root
cd "$(dirname "$0")/.."

# Show current resources
show_current_resources

# If no specific options were provided, run interactive mode
if [ "$remove_containers" = false ] && [ "$remove_volumes" = false ] && [ "$remove_images" = false ] && [ "$remove_networks" = false ] && [ "$system_cleanup_only" = false ]; then
    interactive_cleanup
fi

# Execute cleanup operations
echo ""
print_status "Starting cleanup operations..."

# Stop containers first
if [ "$remove_containers" = true ] || [ "$remove_volumes" = true ] || [ "$remove_images" = true ]; then
    stop_containers
fi

# Remove in the correct order
if [ "$remove_containers" = true ]; then
    remove_containers
fi

if [ "$remove_volumes" = true ]; then
    remove_volumes
fi

if [ "$remove_images" = true ]; then
    remove_images
fi

if [ "$remove_networks" = true ]; then
    remove_networks
fi

if [ "$system_cleanup_only" = true ]; then
    system_cleanup
fi

# Show final status
echo ""
print_status "Final status:"
show_current_resources

if [ "$dry_run" = true ]; then
    print_success "Dry run completed - no changes were made"
else
    print_success "Cleanup completed!"
fi