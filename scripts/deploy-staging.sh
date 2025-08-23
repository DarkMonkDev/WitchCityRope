#!/bin/bash
# WitchCityRope Staging Deployment Script
# This script automates the deployment process to staging environment

set -e  # Exit on error

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="docker-compose.staging.yml"
ENV_FILE=".env.staging"
LOG_FILE="logs/staging-deployment-$(date +%Y%m%d_%H%M%S).log"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log() {
    echo -e "$1" | tee -a "$LOG_FILE"
}

log_info() {
    log "${GREEN}[INFO]${NC} $1"
}

log_warning() {
    log "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    log "${RED}[ERROR]${NC} $1"
}

log_section() {
    log "${BLUE}==== $1 ====${NC}"
}

# Initialize logging
mkdir -p "$(dirname "$LOG_FILE")"
log_section "WitchCityRope Staging Deployment Started at $(date)"

# Pre-deployment checks
pre_deployment_checks() {
    log_section "Pre-deployment Checks"
    
    # Check if running from project root
    if [ ! -f "$PROJECT_ROOT/WitchCityRope.sln" ]; then
        log_error "Must run from project root directory"
        exit 1
    fi
    
    # Check for required files
    if [ ! -f "$PROJECT_ROOT/$ENV_FILE" ]; then
        log_error "Environment file $ENV_FILE not found!"
        log_info "Creating from example..."
        if [ -f "$PROJECT_ROOT/.env.staging.example" ]; then
            cp "$PROJECT_ROOT/.env.staging.example" "$PROJECT_ROOT/$ENV_FILE"
            log_warning "Please edit $ENV_FILE with your staging configuration"
            exit 1
        else
            log_error "No example environment file found"
            exit 1
        fi
    fi
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    # Check Docker daemon
    if ! docker info &> /dev/null; then
        log_error "Docker daemon is not running"
        exit 1
    fi
    
    log_info "All pre-deployment checks passed"
}

# Pull latest code
update_code() {
    log_section "Updating Code"
    
    # Check for uncommitted changes
    if ! git diff-index --quiet HEAD --; then
        log_warning "Uncommitted changes detected"
        read -p "Continue with deployment? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "Deployment cancelled"
            exit 1
        fi
    fi
    
    # Pull latest changes
    log_info "Pulling latest code from staging branch..."
    git fetch origin
    git checkout staging
    git pull origin staging
    
    # Log current commit
    COMMIT_HASH=$(git rev-parse HEAD)
    log_info "Current commit: $COMMIT_HASH"
}

# Backup database
backup_database() {
    log_section "Database Backup"
    
    DB_PATH="$PROJECT_ROOT/data/staging/witchcityrope_staging.db"
    BACKUP_DIR="$PROJECT_ROOT/backups"
    
    if [ -f "$DB_PATH" ]; then
        mkdir -p "$BACKUP_DIR"
        BACKUP_FILE="$BACKUP_DIR/staging_$(date +%Y%m%d_%H%M%S).db"
        log_info "Backing up database to $BACKUP_FILE"
        cp "$DB_PATH" "$BACKUP_FILE"
        
        # Keep only last 10 backups
        log_info "Cleaning old backups..."
        cd "$BACKUP_DIR"
        ls -t staging_*.db 2>/dev/null | tail -n +11 | xargs -r rm
        cd - > /dev/null
        
        log_info "Database backup completed"
    else
        log_warning "No existing database found, skipping backup"
    fi
}

# Build Docker images
build_images() {
    log_section "Building Docker Images"
    
    log_info "Building API image..."
    docker-compose -f "$COMPOSE_FILE" build api
    
    log_info "Building Web image..."
    docker-compose -f "$COMPOSE_FILE" build web
    
    log_info "All images built successfully"
}

# Deploy services
deploy_services() {
    log_section "Deploying Services"
    
    # Stop existing services
    log_info "Stopping existing services..."
    docker-compose -f "$COMPOSE_FILE" down
    
    # Remove old containers and volumes (except data volumes)
    log_info "Cleaning up old containers..."
    docker-compose -f "$COMPOSE_FILE" rm -f
    
    # Start services
    log_info "Starting services..."
    docker-compose -f "$COMPOSE_FILE" up -d
    
    # Wait for services to be ready
    log_info "Waiting for services to start..."
    sleep 10
}

# Run database migrations
run_migrations() {
    log_section "Database Migrations"
    
    log_info "Running Entity Framework migrations..."
    docker-compose -f "$COMPOSE_FILE" exec -T api dotnet ef database update
    
    if [ $? -eq 0 ]; then
        log_info "Migrations completed successfully"
    else
        log_error "Migration failed!"
        return 1
    fi
}

# Initialize staging data
init_staging_data() {
    log_section "Initializing Staging Data"
    
    # Check if database is empty
    USER_COUNT=$(docker-compose -f "$COMPOSE_FILE" exec -T api sqlite3 /app/data/witchcityrope_staging.db "SELECT COUNT(*) FROM Users;" 2>/dev/null || echo "0")
    
    if [ "$USER_COUNT" -eq "0" ]; then
        log_info "Empty database detected, initializing staging data..."
        docker-compose -f "$COMPOSE_FILE" exec -T api /app/scripts/init-staging-db.sh
    else
        log_info "Database already contains data (Users: $USER_COUNT)"
    fi
}

# Health checks
perform_health_checks() {
    log_section "Health Checks"
    
    # Check API health
    log_info "Checking API health..."
    API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5653/health || echo "000")
    if [ "$API_HEALTH" = "200" ]; then
        log_info "API health check passed"
    else
        log_error "API health check failed (HTTP $API_HEALTH)"
        return 1
    fi
    
    # Check Web health
    log_info "Checking Web application health..."
    WEB_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5651 || echo "000")
    if [ "$WEB_HEALTH" = "200" ]; then
        log_info "Web application health check passed"
    else
        log_error "Web application health check failed (HTTP $WEB_HEALTH)"
        return 1
    fi
    
    # Check Redis
    log_info "Checking Redis connection..."
    docker-compose -f "$COMPOSE_FILE" exec -T redis redis-cli ping > /dev/null
    if [ $? -eq 0 ]; then
        log_info "Redis is responding"
    else
        log_warning "Redis health check failed"
    fi
    
    log_info "All health checks completed"
}

# Post-deployment tasks
post_deployment() {
    log_section "Post-deployment Tasks"
    
    # Clear caches
    log_info "Clearing application caches..."
    docker-compose -f "$COMPOSE_FILE" exec -T redis redis-cli FLUSHDB > /dev/null
    
    # Warm up application
    log_info "Warming up application..."
    curl -s http://localhost:5651 > /dev/null
    curl -s http://localhost:5653/api/events > /dev/null
    
    # Display service information
    log_section "Deployment Summary"
    log_info "Deployment completed successfully!"
    log_info ""
    log_info "Service URLs:"
    log_info "  Web Application: http://localhost:5651"
    log_info "  API: http://localhost:5653"
    log_info "  Seq Logs: http://localhost:5341"
    log_info ""
    log_info "Container Status:"
    docker-compose -f "$COMPOSE_FILE" ps
    
    # Save deployment info
    cat > "$PROJECT_ROOT/logs/last-staging-deployment.json" << EOF
{
    "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "commit": "$COMMIT_HASH",
    "version": "$(grep Version src/WitchCityRope.Api/WitchCityRope.Api.csproj | grep -oP '(?<=<Version>)[^<]+')",
    "status": "success"
}
EOF
}

# Error handler
handle_error() {
    log_error "Deployment failed!"
    log_info "Rolling back..."
    
    # Stop any partially started services
    docker-compose -f "$COMPOSE_FILE" down
    
    # Restore database backup if exists
    if [ -n "$BACKUP_FILE" ] && [ -f "$BACKUP_FILE" ]; then
        log_info "Restoring database backup..."
        cp "$BACKUP_FILE" "$DB_PATH"
    fi
    
    log_error "Rollback completed. Please check the logs at: $LOG_FILE"
    exit 1
}

# Main deployment flow
main() {
    cd "$PROJECT_ROOT"
    
    # Set error handler
    trap handle_error ERR
    
    # Execute deployment steps
    pre_deployment_checks
    update_code
    backup_database
    build_images
    deploy_services
    run_migrations
    init_staging_data
    perform_health_checks
    post_deployment
    
    log_section "Deployment Completed Successfully at $(date)"
}

# Run main function
main "$@"