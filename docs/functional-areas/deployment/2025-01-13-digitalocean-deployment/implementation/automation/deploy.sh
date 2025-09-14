#!/bin/bash
# WitchCityRope Deployment Automation Script
# Automates deployment of specific versions to production or staging
# Usage: ./deploy.sh <environment> <version> [options]

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="/opt/witchcityrope"
DOCKER_REGISTRY="registry.digitalocean.com/witchcityrope"
LOG_FILE="/var/log/witchcityrope/deployment.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to display usage
show_usage() {
    echo "Usage: $0 <environment> <version> [options]"
    echo ""
    echo "Arguments:"
    echo "  environment    production, staging"
    echo "  version        Docker image tag (e.g., v1.2.3, latest, main-abc123)"
    echo ""
    echo "Options:"
    echo "  --dry-run      Show what would be deployed without making changes"
    echo "  --force        Skip confirmation prompts"
    echo "  --rollback     Rollback to previous version"
    echo "  --health-check Skip post-deployment health checks"
    echo "  --backup       Create backup before deployment"
    echo ""
    echo "Examples:"
    echo "  $0 staging latest"
    echo "  $0 production v1.2.3 --backup"
    echo "  $0 production --rollback"
    echo "  $0 staging main-abc123 --dry-run"
    exit 1
}

# Logging function
log_message() {
    local level=$1
    local message=$2
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] [$level] $message" | tee -a "$LOG_FILE"

    case $level in
        "ERROR")
            echo -e "${RED}❌ $message${NC}"
            ;;
        "SUCCESS")
            echo -e "${GREEN}✅ $message${NC}"
            ;;
        "WARNING")
            echo -e "${YELLOW}⚠️  $message${NC}"
            ;;
        "INFO")
            echo -e "${BLUE}ℹ️  $message${NC}"
            ;;
    esac
}

# Function to check prerequisites
check_prerequisites() {
    log_message "INFO" "Checking deployment prerequisites..."

    # Check if running as correct user
    if [ "$USER" = "root" ]; then
        log_message "ERROR" "This script should not be run as root"
        exit 1
    fi

    # Check Docker access
    if ! docker ps > /dev/null 2>&1; then
        log_message "ERROR" "Docker is not accessible"
        exit 1
    fi

    # Check environment directory exists
    if [ ! -d "$PROJECT_ROOT/$ENVIRONMENT" ]; then
        log_message "ERROR" "Environment directory not found: $PROJECT_ROOT/$ENVIRONMENT"
        exit 1
    fi

    # Check environment file exists
    if [ ! -f "$PROJECT_ROOT/$ENVIRONMENT/.env.$ENVIRONMENT" ]; then
        log_message "ERROR" "Environment file not found: $PROJECT_ROOT/$ENVIRONMENT/.env.$ENVIRONMENT"
        exit 1
    fi

    log_message "SUCCESS" "Prerequisites check passed"
}

# Function to create backup before deployment
create_backup() {
    if [ "$CREATE_BACKUP" = true ]; then
        log_message "INFO" "Creating backup before deployment..."

        if [ -x "$PROJECT_ROOT/backup-full.sh" ]; then
            "$PROJECT_ROOT/backup-full.sh" >> "$LOG_FILE" 2>&1
            log_message "SUCCESS" "Backup created successfully"
        else
            log_message "WARNING" "Backup script not found, skipping backup"
        fi
    fi
}

# Function to pull Docker images
pull_images() {
    local api_image="$DOCKER_REGISTRY/witchcityrope-api:$VERSION"
    local web_image="$DOCKER_REGISTRY/witchcityrope-web:$VERSION"

    log_message "INFO" "Pulling Docker images for version $VERSION..."

    if [ "$DRY_RUN" = true ]; then
        log_message "INFO" "DRY RUN: Would pull $api_image"
        log_message "INFO" "DRY RUN: Would pull $web_image"
        return 0
    fi

    # Pull API image
    if docker pull "$api_image"; then
        log_message "SUCCESS" "Successfully pulled API image: $api_image"
    else
        log_message "ERROR" "Failed to pull API image: $api_image"
        return 1
    fi

    # Pull Web image
    if docker pull "$web_image"; then
        log_message "SUCCESS" "Successfully pulled Web image: $web_image"
    else
        log_message "ERROR" "Failed to pull Web image: $web_image"
        return 1
    fi
}

# Function to get current running version
get_current_version() {
    local container_name="witchcity-api-$([ "$ENVIRONMENT" = "production" ] && echo "prod" || echo "staging")"

    if docker ps --format "table {{.Names}}\t{{.Image}}" | grep -q "$container_name"; then
        docker inspect "$container_name" --format='{{.Config.Image}}' | sed 's/.*://'
    else
        echo "none"
    fi
}

# Function to deploy to environment
deploy_environment() {
    local api_image="$DOCKER_REGISTRY/witchcityrope-api:$VERSION"
    local web_image="$DOCKER_REGISTRY/witchcityrope-web:$VERSION"
    local compose_file="docker-compose.$ENVIRONMENT.yml"

    log_message "INFO" "Deploying version $VERSION to $ENVIRONMENT environment..."

    cd "$PROJECT_ROOT/$ENVIRONMENT"

    if [ "$DRY_RUN" = true ]; then
        log_message "INFO" "DRY RUN: Would deploy with images:"
        log_message "INFO" "  API: $api_image"
        log_message "INFO" "  Web: $web_image"
        log_message "INFO" "DRY RUN: Would run: API_IMAGE=$api_image WEB_IMAGE=$web_image docker-compose -f $compose_file up -d"
        return 0
    fi

    # Save current version for potential rollback
    PREVIOUS_VERSION=$(get_current_version)
    echo "$PREVIOUS_VERSION" > ".previous_version"

    # Deploy new version
    API_IMAGE="$api_image" WEB_IMAGE="$web_image" \
    docker-compose -f "$compose_file" up -d

    if [ $? -eq 0 ]; then
        log_message "SUCCESS" "Deployment completed for $ENVIRONMENT"
        echo "$VERSION" > ".current_version"
    else
        log_message "ERROR" "Deployment failed for $ENVIRONMENT"
        return 1
    fi
}

# Function to wait for services to be healthy
wait_for_health() {
    if [ "$SKIP_HEALTH_CHECK" = true ]; then
        log_message "INFO" "Skipping health checks as requested"
        return 0
    fi

    local api_port=$([ "$ENVIRONMENT" = "production" ] && echo "5001" || echo "5002")
    local web_port=$([ "$ENVIRONMENT" = "production" ] && echo "3001" || echo "3002")
    local max_wait=180
    local wait_time=0

    log_message "INFO" "Waiting for services to become healthy (max ${max_wait}s)..."

    if [ "$DRY_RUN" = true ]; then
        log_message "INFO" "DRY RUN: Would wait for health checks on ports $api_port and $web_port"
        return 0
    fi

    while [ $wait_time -lt $max_wait ]; do
        local api_healthy=false
        local web_healthy=false

        # Check API health
        if curl -f -s "http://localhost:$api_port/health" > /dev/null 2>&1; then
            api_healthy=true
        fi

        # Check Web health
        if curl -f -s "http://localhost:$web_port" > /dev/null 2>&1; then
            web_healthy=true
        fi

        if [ "$api_healthy" = true ] && [ "$web_healthy" = true ]; then
            log_message "SUCCESS" "All services are healthy after ${wait_time}s"
            return 0
        fi

        echo -n "."
        sleep 5
        wait_time=$((wait_time + 5))
    done

    echo ""
    log_message "ERROR" "Services did not become healthy within ${max_wait}s"

    # Show container status for debugging
    log_message "INFO" "Container status:"
    docker ps --filter "name=witchcity-" --format "table {{.Names}}\t{{.Status}}" | tee -a "$LOG_FILE"

    return 1
}

# Function to rollback deployment
rollback_deployment() {
    local compose_file="docker-compose.$ENVIRONMENT.yml"
    local previous_version_file="$PROJECT_ROOT/$ENVIRONMENT/.previous_version"

    log_message "INFO" "Rolling back $ENVIRONMENT deployment..."

    if [ ! -f "$previous_version_file" ]; then
        log_message "ERROR" "No previous version information found for rollback"
        exit 1
    fi

    local previous_version=$(cat "$previous_version_file")

    if [ -z "$previous_version" ] || [ "$previous_version" = "none" ]; then
        log_message "ERROR" "No valid previous version found for rollback"
        exit 1
    fi

    log_message "INFO" "Rolling back to version: $previous_version"

    if [ "$DRY_RUN" = true ]; then
        log_message "INFO" "DRY RUN: Would rollback to version $previous_version"
        return 0
    fi

    # Rollback to previous version
    local api_image="$DOCKER_REGISTRY/witchcityrope-api:$previous_version"
    local web_image="$DOCKER_REGISTRY/witchcityrope-web:$previous_version"

    cd "$PROJECT_ROOT/$ENVIRONMENT"

    API_IMAGE="$api_image" WEB_IMAGE="$web_image" \
    docker-compose -f "$compose_file" up -d

    if [ $? -eq 0 ]; then
        log_message "SUCCESS" "Rollback completed to version $previous_version"
        echo "$previous_version" > ".current_version"
    else
        log_message "ERROR" "Rollback failed"
        return 1
    fi
}

# Function to run post-deployment tests
run_deployment_tests() {
    if [ "$SKIP_HEALTH_CHECK" = true ] || [ "$DRY_RUN" = true ]; then
        return 0
    fi

    log_message "INFO" "Running post-deployment tests..."

    local base_url
    if [ "$ENVIRONMENT" = "production" ]; then
        base_url="https://witchcityrope.com"
    else
        base_url="https://staging.witchcityrope.com"
    fi

    # Test API health endpoint
    if curl -f -s "$base_url/api/health" > /dev/null 2>&1; then
        log_message "SUCCESS" "API health check passed"
    else
        log_message "ERROR" "API health check failed"
        return 1
    fi

    # Test web application
    if curl -f -s "$base_url" > /dev/null 2>&1; then
        log_message "SUCCESS" "Web application accessible"
    else
        log_message "ERROR" "Web application not accessible"
        return 1
    fi

    log_message "SUCCESS" "All post-deployment tests passed"
}

# Function to send deployment notification
send_notification() {
    local status=$1
    local webhook_url="${SLACK_WEBHOOK_URL:-}"

    if [ -n "$webhook_url" ] && [ "$DRY_RUN" != true ]; then
        local color=$([ "$status" = "success" ] && echo "good" || echo "danger")
        local emoji=$([ "$status" = "success" ] && echo ":white_check_mark:" || echo ":x:")

        curl -X POST -H 'Content-type: application/json' \
            --data "{
                \"attachments\": [{
                    \"color\": \"$color\",
                    \"title\": \"WitchCityRope Deployment $status\",
                    \"fields\": [
                        {\"title\": \"Environment\", \"value\": \"$ENVIRONMENT\", \"short\": true},
                        {\"title\": \"Version\", \"value\": \"$VERSION\", \"short\": true},
                        {\"title\": \"Timestamp\", \"value\": \"$(date)\", \"short\": true}
                    ]
                }]
            }" \
            "$webhook_url" > /dev/null 2>&1 || true
    fi
}

# Main execution function
main() {
    local deployment_start=$(date +%s)

    log_message "INFO" "Starting deployment process..."
    log_message "INFO" "Environment: $ENVIRONMENT"
    log_message "INFO" "Version: $VERSION"
    log_message "INFO" "Options: DRY_RUN=$DRY_RUN, FORCE=$FORCE, ROLLBACK=$ROLLBACK"

    # Check prerequisites
    check_prerequisites

    # Handle rollback
    if [ "$ROLLBACK" = true ]; then
        rollback_deployment
        wait_for_health
        run_deployment_tests
        send_notification "success"
        log_message "SUCCESS" "Rollback completed successfully"
        return 0
    fi

    # Confirm deployment unless forced
    if [ "$FORCE" != true ] && [ "$DRY_RUN" != true ]; then
        echo ""
        echo -e "${YELLOW}⚠️  Deployment Confirmation${NC}"
        echo "Environment: $ENVIRONMENT"
        echo "Version: $VERSION"
        echo "Current version: $(get_current_version)"
        echo ""
        read -p "Continue with deployment? (yes/no): " confirm

        if [ "$confirm" != "yes" ]; then
            log_message "INFO" "Deployment cancelled by user"
            exit 0
        fi
    fi

    # Create backup if requested
    create_backup

    # Pull new images
    pull_images

    # Deploy to environment
    deploy_environment

    # Wait for services to be healthy
    wait_for_health

    # Run post-deployment tests
    run_deployment_tests

    # Calculate deployment time
    local deployment_end=$(date +%s)
    local duration=$((deployment_end - deployment_start))
    local duration_formatted=$(printf '%02d:%02d:%02d' $((duration/3600)) $((duration%3600/60)) $((duration%60)))

    # Send success notification
    send_notification "success"

    log_message "SUCCESS" "Deployment completed successfully in $duration_formatted"

    # Show final status
    if [ "$DRY_RUN" != true ]; then
        echo ""
        echo -e "${GREEN}✅ Deployment Summary${NC}"
        echo "Environment: $ENVIRONMENT"
        echo "Version: $VERSION"
        echo "Duration: $duration_formatted"
        echo "Status: SUCCESS"
        echo ""

        # Show running containers
        docker ps --filter "name=witchcity-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    fi
}

# Parse command line arguments
if [ $# -eq 0 ]; then
    show_usage
fi

ENVIRONMENT=""
VERSION=""
DRY_RUN=false
FORCE=false
ROLLBACK=false
SKIP_HEALTH_CHECK=false
CREATE_BACKUP=false

# Parse positional arguments
if [ "$1" != "--rollback" ]; then
    ENVIRONMENT=$1
    shift

    if [ $# -gt 0 ] && [ "$1" != "--"* ]; then
        VERSION=$1
        shift
    fi
fi

# Parse options
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --rollback)
            ROLLBACK=true
            if [ -z "$ENVIRONMENT" ]; then
                ENVIRONMENT=$2
                shift
            fi
            shift
            ;;
        --health-check)
            SKIP_HEALTH_CHECK=true
            shift
            ;;
        --backup)
            CREATE_BACKUP=true
            shift
            ;;
        *)
            log_message "ERROR" "Unknown option: $1"
            show_usage
            ;;
    esac
done

# Validate arguments
if [ -z "$ENVIRONMENT" ]; then
    log_message "ERROR" "Environment is required"
    show_usage
fi

if [ "$ENVIRONMENT" != "production" ] && [ "$ENVIRONMENT" != "staging" ]; then
    log_message "ERROR" "Environment must be 'production' or 'staging'"
    exit 1
fi

if [ "$ROLLBACK" != true ] && [ -z "$VERSION" ]; then
    log_message "ERROR" "Version is required unless using --rollback"
    show_usage
fi

# Set default version for rollback
if [ "$ROLLBACK" = true ]; then
    VERSION="rollback"
fi

# Execute main function
main