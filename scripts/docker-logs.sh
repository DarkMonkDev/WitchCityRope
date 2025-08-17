#!/bin/bash
# WitchCityRope - Docker Logs Viewer
# Description: View and filter logs for specific services with advanced options

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
    echo "WitchCityRope Docker Logs Viewer"
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
    echo "  -f, --follow       Follow log output (live updates)"
    echo "  -t, --timestamps   Show timestamps"
    echo "  -n, --tail N       Show last N lines (default: 100)"
    echo "  --since TIME       Show logs since timestamp (e.g., '2023-01-01T00:00:00Z')"
    echo "  --until TIME       Show logs until timestamp"
    echo "  --filter PATTERN   Filter logs with grep pattern"
    echo "  --errors           Show only error lines"
    echo "  --auth             Show only authentication-related logs"
    echo "  --debug            Show debug/verbose output"
    echo "  --no-color         Disable colored output"
    echo ""
    echo "Examples:"
    echo "  $0                       # Show all service logs"
    echo "  $0 api --follow          # Follow API logs"
    echo "  $0 web --tail 50         # Last 50 lines from React app"
    echo "  $0 all --errors          # Show only errors from all services"
    echo "  $0 api --auth --follow   # Follow authentication logs in API"
    echo "  $0 postgres --since '1 hour ago' # Database logs from last hour"
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
            echo ""
            ;;
        *)
            print_error "Unknown service: $input"
            echo "Available services: web, api, postgres, all"
            exit 1
            ;;
    esac
}

get_running_services() {
    docker-compose ps --services 2>/dev/null || echo ""
}

build_docker_logs_command() {
    local service="$1"
    local cmd="docker-compose logs"
    
    # Add service if specified
    if [ -n "$service" ]; then
        cmd="$cmd $service"
    fi
    
    # Add options
    if [ "$follow_logs" = true ]; then
        cmd="$cmd -f"
    fi
    
    if [ "$show_timestamps" = true ]; then
        cmd="$cmd -t"
    fi
    
    if [ -n "$tail_lines" ]; then
        cmd="$cmd --tail=$tail_lines"
    fi
    
    if [ -n "$since_time" ]; then
        cmd="$cmd --since='$since_time'"
    fi
    
    if [ -n "$until_time" ]; then
        cmd="$cmd --until='$until_time'"
    fi
    
    echo "$cmd"
}

build_filter_command() {
    local filter_cmd=""
    
    if [ "$show_errors_only" = true ]; then
        filter_cmd="grep -i -E '(error|exception|fail|fatal|panic|critical)'"
    elif [ "$show_auth_only" = true ]; then
        filter_cmd="grep -i -E '(auth|login|jwt|token|cookie|session|signin|signout|register)'"
    elif [ -n "$custom_filter" ]; then
        filter_cmd="grep -E '$custom_filter'"
    fi
    
    echo "$filter_cmd"
}

colorize_logs() {
    if [ "$no_color" = true ]; then
        cat
    else
        sed -E "
            s/(ERROR|error|Error)/\\${RED}&\\${NC}/g;
            s/(WARN|warn|Warning|WARNING)/\\${YELLOW}&\\${NC}/g;
            s/(INFO|info|Information)/\\${BLUE}&\\${NC}/g;
            s/(DEBUG|debug|Debug)/\\${CYAN}&\\${NC}/g;
            s/(SUCCESS|success|Success)/\\${GREEN}&\\${NC}/g;
            s/(auth|login|jwt|token|cookie)/\\${YELLOW}&\\${NC}/g;
            s/([0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2})/\\${CYAN}&\\${NC}/g;
        "
    fi
}

show_service_status() {
    print_status "Container Status:"
    local running_services=$(get_running_services)
    
    if [ -z "$running_services" ]; then
        print_warning "No containers are currently running"
        echo "Start the development environment with: ./scripts/docker-dev.sh"
        return 1
    fi
    
    docker-compose ps
    echo ""
}

show_log_info() {
    local service="$1"
    
    print_status "Log Information:"
    if [ -n "$service" ]; then
        echo "  Service: $service"
    else
        echo "  Service: all services"
    fi
    
    if [ "$follow_logs" = true ]; then
        echo "  Mode: following (live updates)"
    else
        echo "  Mode: static"
    fi
    
    if [ -n "$tail_lines" ]; then
        echo "  Lines: last $tail_lines"
    fi
    
    if [ -n "$since_time" ]; then
        echo "  Since: $since_time"
    fi
    
    if [ -n "$until_time" ]; then
        echo "  Until: $until_time"
    fi
    
    if [ "$show_errors_only" = true ]; then
        echo "  Filter: errors only"
    elif [ "$show_auth_only" = true ]; then
        echo "  Filter: authentication logs"
    elif [ -n "$custom_filter" ]; then
        echo "  Filter: $custom_filter"
    fi
    
    echo ""
}

interactive_service_selection() {
    local running_services=$(get_running_services)
    
    if [ -z "$running_services" ]; then
        print_error "No containers are running"
        exit 1
    fi
    
    echo "Select a service to view logs:"
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
            service=""
            ;;
        2)
            service="web"
            ;;
        3)
            service="api"
            ;;
        4)
            service="postgres"
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

# Parse command line arguments
service=""
follow_logs=false
show_timestamps=false
tail_lines="100"
since_time=""
until_time=""
custom_filter=""
show_errors_only=false
show_auth_only=false
debug_mode=false
no_color=false

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
        -f|--follow)
            follow_logs=true
            shift
            ;;
        -t|--timestamps)
            show_timestamps=true
            shift
            ;;
        -n|--tail)
            if [[ $# -lt 2 ]]; then
                print_error "--tail requires a number"
                exit 1
            fi
            tail_lines="$2"
            shift 2
            ;;
        --since)
            if [[ $# -lt 2 ]]; then
                print_error "--since requires a timestamp"
                exit 1
            fi
            since_time="$2"
            shift 2
            ;;
        --until)
            if [[ $# -lt 2 ]]; then
                print_error "--until requires a timestamp"
                exit 1
            fi
            until_time="$2"
            shift 2
            ;;
        --filter)
            if [[ $# -lt 2 ]]; then
                print_error "--filter requires a pattern"
                exit 1
            fi
            custom_filter="$2"
            shift 2
            ;;
        --errors)
            show_errors_only=true
            shift
            ;;
        --auth)
            show_auth_only=true
            shift
            ;;
        --debug)
            debug_mode=true
            shift
            ;;
        --no-color)
            no_color=true
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
print_status "WitchCityRope Docker Logs Viewer"

# Pre-flight checks
check_docker

# Change to project root
cd "$(dirname "$0")/.."

# Show container status
if ! show_service_status; then
    exit 1
fi

# Interactive service selection if none provided
if [ -z "$service" ] && [ "$#" -eq 0 ]; then
    interactive_service_selection
fi

# Build and execute the command
docker_cmd=$(build_docker_logs_command "$service")
filter_cmd=$(build_filter_command)

# Show what we're about to do
show_log_info "$service"

if [ "$debug_mode" = true ]; then
    print_status "Command: $docker_cmd"
    if [ -n "$filter_cmd" ]; then
        print_status "Filter: $filter_cmd"
    fi
    echo ""
fi

# Execute the command
if [ "$follow_logs" = true ]; then
    print_status "Following logs... (Press Ctrl+C to exit)"
fi

if [ -n "$filter_cmd" ]; then
    eval "$docker_cmd 2>&1 | $filter_cmd | colorize_logs"
else
    eval "$docker_cmd 2>&1 | colorize_logs"
fi